#!/usr/bin/env python3
"""Build a receipt-backed observation; check a controller's Sheets readback.

No provider calls, credentials, certification, ownership or dispatch side effects.
The controller must still verify causal runs, revalidate HEAD and apply one batch.
"""
import argparse
import json
from pathlib import Path
import subprocess
from datetime import datetime, timezone

from parent_transition import valid_closure, SHA40


def _exact_functional_head_gates(receipt):
    """Require every recorded causal gate to certify the exact functional head.

    A successful workflow on a different SHA is not closure evidence. Post-gate
    control-plane commits may exist, but they cannot substitute for testing the
    functional head itself.
    """
    functional = receipt.get('functionalHead')
    gates = receipt.get('gates', receipt.get('causalGates', []))
    return (
        isinstance(functional, str)
        and SHA40.fullmatch(functional) is not None
        and isinstance(gates, list) and bool(gates)
        and all(
            isinstance(g, dict)
            and g.get('conclusion') == 'success'
            and (g.get('workflowRunId') or g.get('runId'))
            and g.get('headSha') == functional
            for g in gates
        )
    )


def observation(root, head, observed_at):
    root = Path(root)
    catalog = json.loads((root / 'vaep/control/jules-autorefill-catalog.json').read_text())
    current, closed = catalog['currentParent'], catalog['lastClosedParent']
    roadmap = catalog['roadmap']
    if roadmap.get('authority') != 'docs/VAEP_AUTHORITY.md':
        raise ValueError('INVALID_ROADMAP_AUTHORITY')
    nodes = {n['id']: n for n in roadmap['nodes']}
    if len(nodes) != len(roadmap['nodes']):
        raise ValueError('DUPLICATE_ROADMAP_NODE')
    receipt_path = catalog['closureReceipts'][closed]
    resolved = (root / receipt_path).resolve()
    if not resolved.is_relative_to((root / 'vaep/evidence/fragments').resolve()):
        raise ValueError('RECEIPT_OUTSIDE_EVIDENCE')
    receipt = json.loads(resolved.read_text())
    acceptance, guardrails = receipt.get('acceptance', {}), receipt.get('guardrails', {})
    recorded_contract = (
        receipt.get('authority') == 'docs/VAEP_AUTHORITY.md'
        and receipt.get('parent') == closed and receipt.get('status') == 'LISTO_REAL'
        and SHA40.fullmatch(receipt.get('functionalHead', '')) is not None
        and type(acceptance.get('p0')) is int and acceptance['p0'] == 0
        and type(acceptance.get('p1')) is int and acceptance['p1'] == 0
        and bool(receipt.get('reviewReceipt'))
        and _exact_functional_head_gates(receipt)
        and all(guardrails.get(k) is False for k in ['mainTouched', 'productionTouched', 'pr2Merged'])
    )
    if not (valid_closure(receipt, closed) or recorded_contract):
        raise ValueError('INVALID_CLOSURE_RECEIPT')
    if not _exact_functional_head_gates(receipt):
        raise ValueError('CAUSAL_GATE_NOT_ON_EXACT_FUNCTIONAL_HEAD')
    if current not in nodes:
        raise ValueError('CURRENT_PARENT_NOT_IN_ROADMAP')
    later = sorted((n for n in nodes.values() if current in n.get('dependencies', [])),
                   key=lambda n: n['order'])
    return dict(currentParent=current, lastClosedParent=closed, sourceHead=head,
                observedAt=observed_at, nextParent=later[0]['id'] if later else None,
                receipt=receipt_path, functionalHead=receipt['functionalHead'],
                causalGates=receipt.get('causalGates', receipt.get('gates')), syncStatus='SYNC_PENDING')


def verify(snapshot, readback, live_head):
    errors = []
    if live_head != snapshot['sourceHead']:
        errors.append('HEAD_CHANGED_REBUILD')
    config = readback.get('config', {})
    for key, field in [('CURRENT_PARENT', 'currentParent'), ('LAST_CLOSED_PARENT', 'lastClosedParent'),
                       ('CURRENT_HEAD', 'sourceHead'), ('NEXT_PARENT', 'nextParent')]:
        if config.get(key) != (snapshot[field] or ''):
            errors.append('CONFIG_MISMATCH:' + key)
    states = readback.get('cola', {})
    if states.get(snapshot['lastClosedParent']) != 'LISTO':
        errors.append('CLOSED_PARENT_REGRESSION')
    current = snapshot['currentParent']
    expected = {'LISTO'} if current == snapshot['lastClosedParent'] else {'EN_PROGRESO', 'VALIDANDO', 'BLOQUEADO'}
    if states.get(current) not in expected:
        errors.append('CURRENT_PARENT_STATE_MISMATCH')
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', default='.')
    parser.add_argument('--readback', help='JSON: config key/value map and cola ID/state map')
    parser.add_argument('--live-head', help='Fresh remote HEAD, mandatory with --readback')
    args = parser.parse_args()
    head = subprocess.check_output(['git', '-C', args.root, 'rev-parse', 'HEAD'], text=True).strip()
    snap = observation(args.root, head, datetime.now(timezone.utc).isoformat())
    if args.readback:
        if not args.live_head:
            parser.error('--readback requires --live-head')
        errors = verify(snap, json.loads(Path(args.readback).read_text()), args.live_head)
        print(json.dumps(dict(status='DRIFT' if errors else 'VERIFIED_READBACK', errors=errors)))
        return 1 if errors else 0
    print(json.dumps(snap, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
