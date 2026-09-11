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


def _normalize_utc(value, field):
    if value is None:
        return None
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f'{field}_INVALID')
    raw = value.strip()
    try:
        parsed = datetime.fromisoformat(raw[:-1] + '+00:00' if raw.endswith('Z') else raw)
    except ValueError as exc:
        raise ValueError(f'{field}_INVALID') from exc
    if parsed.tzinfo is None:
        raise ValueError(f'{field}_TIMEZONE_REQUIRED')
    return parsed.astimezone(timezone.utc).isoformat().replace('+00:00', 'Z')


def build_telemetry_clocks(receipt, observed_at, scheduler_trigger_utc=None, material_action_utc=None):
    """Keep scheduler, material work and telemetry freshness as separate clocks.

    Scheduler runtime belongs to the external controller/Tasks runtime, so this
    repo-local verifier never invents it from a Git commit or sync timestamp.
    Material activity may be supplied explicitly from verified runtime evidence;
    otherwise the last certified parent receipt is the conservative fallback.
    None of these clocks certifies ACTIVE_REAL or LISTO_REAL by itself.
    """
    observed = _normalize_utc(observed_at, 'LAST_TELEMETRY_SYNC')
    scheduler = _normalize_utc(scheduler_trigger_utc, 'LAST_SCHEDULER_TRIGGER') if scheduler_trigger_utc else None

    if material_action_utc:
        material = _normalize_utc(material_action_utc, 'LAST_MATERIAL_ACTION')
        material_source = 'VERIFIED_RUNTIME_EXPLICIT'
    else:
        certified = receipt.get('certifiedAtUtc') if isinstance(receipt, dict) else None
        material = _normalize_utc(certified, 'LAST_MATERIAL_ACTION') if certified else None
        material_source = 'LAST_CLOSED_RECEIPT_CERTIFICATION' if material else 'UNAVAILABLE'

    return {
        'lastSchedulerTriggerUtc': scheduler,
        'lastMaterialActionUtc': material,
        'lastTelemetrySyncUtc': observed,
        'provenance': {
            'lastSchedulerTriggerUtc': 'EXTERNAL_CONTROLLER_RUNTIME_EXPLICIT' if scheduler else 'UNAVAILABLE_REPO_LOCAL',
            'lastMaterialActionUtc': material_source,
            'lastTelemetrySyncUtc': 'STATE_SYNC_LOCAL_OBSERVATION',
        },
        'invariants': {
            'telemetrySyncIsNotSchedulerTrigger': True,
            'telemetrySyncIsNotMaterialAction': True,
            'schedulerTriggerDoesNotProveMaterialAction': True,
            'materialActionDoesNotProveActiveReal': True,
            'materialActionDoesNotProveListoReal': True,
        },
    }


def observation(root, head, observed_at, scheduler_trigger_utc=None, material_action_utc=None):
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
    clocks = build_telemetry_clocks(
        receipt,
        observed_at,
        scheduler_trigger_utc=scheduler_trigger_utc,
        material_action_utc=material_action_utc,
    )
    return dict(currentParent=current, lastClosedParent=closed, sourceHead=head,
                observedAt=observed_at, nextParent=later[0]['id'] if later else None,
                receipt=receipt_path, functionalHead=receipt['functionalHead'],
                causalGates=receipt.get('causalGates', receipt.get('gates')),
                telemetryClocks=clocks, syncStatus='SYNC_PENDING')


def verify(snapshot, readback, live_head):
    errors = []
    if live_head != snapshot['sourceHead']:
        errors.append('HEAD_CHANGED_REBUILD')
    config = readback.get('config', {})
    for key, field in [('CURRENT_PARENT', 'currentParent'), ('LAST_CLOSED_PARENT', 'lastClosedParent'),
                       ('CURRENT_HEAD', 'sourceHead'), ('NEXT_PARENT', 'nextParent')]:
        if config.get(key) != (snapshot[field] or ''):
            errors.append('CONFIG_MISMATCH:' + key)

    # Tri-clock fields are opt-in for existing Sheets. If a controller supplies
    # them in readback they are verified independently; absence does not break
    # older sheets and, crucially, one clock never substitutes for another.
    clocks = snapshot.get('telemetryClocks', {})
    for key, field in [('LAST_SCHEDULER_TRIGGER', 'lastSchedulerTriggerUtc'),
                       ('LAST_MATERIAL_ACTION', 'lastMaterialActionUtc'),
                       ('LAST_TELEMETRY_SYNC', 'lastTelemetrySyncUtc')]:
        if key in config and config.get(key) != (clocks.get(field) or ''):
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
    parser.add_argument('--scheduler-trigger-utc',
                        help='Verified external Tasks/controller scheduler trigger; never inferred locally')
    parser.add_argument('--material-action-utc',
                        help='Verified latest useful material action; receipt certification is fallback')
    args = parser.parse_args()
    head = subprocess.check_output(['git', '-C', args.root, 'rev-parse', 'HEAD'], text=True).strip()
    observed_at = datetime.now(timezone.utc).isoformat()
    snap = observation(
        args.root,
        head,
        observed_at,
        scheduler_trigger_utc=args.scheduler_trigger_utc,
        material_action_utc=args.material_action_utc,
    )
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
