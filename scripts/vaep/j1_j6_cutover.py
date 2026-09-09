#!/usr/bin/env python3
"""Fail-closed J1-J6 cutover transformer.

This script is intentionally deterministic and may only run from the dedicated
cutover workflow after the read-only readiness/canary gates have been reviewed.
It never touches main, Production, secrets, deployments or application code.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path('.')


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')


def write(path: str, text: str) -> None:
    (ROOT / path).write_text(text, encoding='utf-8')


def must_replace(text: str, old: str, new: str, *, count: int | None = None, label: str = '') -> str:
    found = text.count(old)
    if found == 0:
        raise SystemExit(f'CUTOVER_ASSERT_MISSING {label or old[:80]!r}')
    if count is not None and found != count:
        raise SystemExit(f'CUTOVER_ASSERT_COUNT {label or old[:80]!r} expected={count} actual={found}')
    return text.replace(old, new)


def replace_if_present(text: str, old: str, new: str) -> str:
    return text.replace(old, new)


def operational_workflow(worker: str, dispatch_path: str, secret: str) -> str:
    return f'''name: VAEP {worker} Trusted Worker

on:
  push:
    branches:
      - Desarrollo
    paths:
      - '{dispatch_path}/*.json'
  workflow_dispatch:
    inputs:
      manifest_commit:
        description: 'Exact immutable manifest commit created by VAEP autorefill'
        required: false
        type: string

permissions:
  actions: write
  contents: write
  issues: write

concurrency:
  group: vaep-{worker.lower()}-lane
  cancel-in-progress: false

jobs:
  dispatch-jules:
    runs-on: ubuntu-latest
    timeout-minutes: 25
    env:
      JULES_API_BASE: https://jules.googleapis.com/v1alpha
      EXPECTED_OWNER: jmejia31
      EXPECTED_REPO: VariApp
      EXPECTED_BRANCH: Desarrollo
      DISPATCH_PATH: {dispatch_path}
      SESSION_TITLE_PREFIX: "VAEP-{worker} "
      ARTIFACT_PREFIX: vaep-{worker.lower()}
      ISSUE_PREFIX: "[VAEP-{worker}]"
      WORKER_LABEL: {worker}
      WORKER_ID: {worker}
      JULES_API_KEY: ${{{{ secrets.{secret} }}}}
      GH_TOKEN: ${{{{ github.token }}}}
      VAEP_DISPATCH_SHA: ${{{{ inputs.manifest_commit || github.sha }}}}
      VAEP_LANE_RUN_ID: ${{{{ github.run_id }}}}
    steps:
      - name: Checkout dispatch commit
        uses: actions/checkout@v4
        with:
          ref: ${{{{ inputs.manifest_commit || github.sha }}}}
          fetch-depth: 2
      - name: Validate immutable dispatch transport
        shell: bash
        run: bash .github/scripts/vaep-jules-master.sh --transport-preflight
      - name: Reserve NEXT {worker} lane
        shell: bash
        continue-on-error: true
        run: |
          set +e
          bash .github/scripts/vaep-jules-autorefill.sh
          rc=$?
          set -e
          case "$rc" in
            0|78|79|80|81|82) exit 0 ;;
            *) exit "$rc" ;;
          esac
      - name: Execute VAEP/Jules MASTER
        shell: bash
        run: bash .github/scripts/vaep-jules-master.sh
      - name: Upload {worker} result artifact
        if: always() && env.RESULT_DIR != ''
        uses: actions/upload-artifact@v4
        with:
          name: ${{{{ env.ARTIFACT_NAME }}}}
          path: ${{{{ env.RESULT_DIR }}}}
          if-no-files-found: error
          retention-days: 14
      - name: Auto-refill {worker} lane
        if: always()
        shell: bash
        continue-on-error: true
        run: |
          set +e
          bash .github/scripts/vaep-jules-autorefill.sh --post-terminal
          rc=$?
          set -e
          case "$rc" in
            0|78|79|80|81|82) exit 0 ;;
            *) exit "$rc" ;;
          esac
'''


def disabled_legacy(name: str) -> str:
    return f'''name: VAEP Legacy {name} Disabled

on:
  workflow_dispatch:

permissions:
  contents: read

jobs:
  disabled:
    runs-on: ubuntu-latest
    steps:
      - run: |
          echo "LEGACY_WORKER_DISABLED_AFTER_J1_J6_CUTOVER={name}"
          exit 1
'''


def transform_master() -> None:
    p = 'docs/VAEP_AUTHORITY.md'
    t = read(p)
    t = must_replace(t, 'JULES_TASKS_TARGET_ROLLING_24H_TOTAL=400', 'JULES_TASKS_TARGET_ROLLING_24H_TOTAL=600', count=1)
    t = must_replace(t, 'JULES_PROGRAMMED_BACKLOG_TARGET_TOTAL=48', 'JULES_PROGRAMMED_BACKLOG_TARGET_TOTAL=72', count=1)
    t = must_replace(t, 'LANE_REFILL_DEADLINE_SECONDS=90', 'LANE_REFILL_DEADLINE_SECONDS=30', count=1)
    t = must_replace(t,
        '1. ChatGPT/VAEP, Jules A/B/C/D y todas las automatizaciones activas deben leer **este mismo archivo** antes de decidir reglas operativas.',
        '1. ChatGPT/VAEP, J1/J2/J3/J4/J5/J6 y todas las automatizaciones activas deben leer **este mismo archivo** antes de decidir reglas operativas.',
        count=1)
    t = must_replace(t,
        '- Jules A/B/C/D: implementers cloud; máximo un write-scope autoritativo por Jules; entregan patch/artifact y no publican funcionalmente.',
        '- J1/J2/J3/J4: implementers cloud CODE. J5: QA/security/regression con fallback CODE. J6: integration/recovery con fallback CODE/QA. Los seis conservan máximo un write-scope autoritativo por ejecución; entregan patch/artifact y no publican funcionalmente.',
        count=1)
    marker = '- Codex: fuera del flujo salvo orden explícita futura del usuario.\n'
    contract = '''- Codex: fuera del flujo salvo orden explícita futura del usuario.\n\n### Workers Jules canónicos\n\n```text\nJULES_ACTIVE_WORKERS=J1,J2,J3,J4,J5,J6\nJULES_ACTIVE_WORKER_COUNT=6\nJULES_WORKER_REGISTRY=vaep/control/jules-workers.json\nJ1_ROLE=CODE_CORE\nJ2_ROLE=CODE_BACKEND_DATA\nJ3_ROLE=CODE_FRONTEND\nJ4_ROLE=CODE_INFRA_INTEGRATIONS\nJ5_ROLE=QA_SECURITY_REGRESSION__FALLBACK_CODE\nJ6_ROLE=INTEGRATION_RECOVERY__FALLBACK_CODE_QA\nJULES_QUEUE_DEPTH_PER_WORKER=2\nJULES_MAX_LIVE_RUNS_TOTAL=12\n```\n\nLos IDs operativos nuevos son exclusivamente `J1..J6`. `JULES_A..JULES_D` pueden aparecer únicamente como evidencia histórica o para terminar una sesión ya iniciada antes del cutover; no son IDs válidos para nuevos dispatches. J3 y J4 usan exclusivamente sus secretos nominales `JULES_J3_API_KEY` y `JULES_J4_API_KEY`; no existe fallback ni referencia temporal de credenciales legacy para estos workers.\n'''
    t = must_replace(t, marker, contract, count=1)
    t = t.replace('Jules A/B/C/D', 'J1/J2/J3/J4/J5/J6')
    t = t.replace('A/B/C/D', 'J1/J2/J3/J4/J5/J6')
    t = t.replace('entre A/B/C/D', 'entre J1/J2/J3/J4/J5/J6')
    t = t.replace('por cada worker A/B/C/D', 'por cada worker J1/J2/J3/J4/J5/J6')
    t = t.replace('400 tareas Jules', '600 tareas Jules')
    t = t.replace('48 agregadas', '72 agregadas')
    t = t.replace('48 tareas', '72 tareas')
    t = t.replace('`LANE_REFILL_DEADLINE_SECONDS=90`', '`LANE_REFILL_DEADLINE_SECONDS=30`')
    old_paths = '''```text\nA: vaep/jules/dispatch/*.json\nB: vaep/jules-b/dispatch/*.json\nC: vaep/jules-c/dispatch/*.json\nD: vaep/jules-d/dispatch/*.json\n```'''
    new_paths = '''```text\nJ1: vaep/jules/dispatch/*.json\nJ2: vaep/jules-b/dispatch/*.json\nJ3: vaep/jules-c/dispatch/*.json\nJ4: vaep/jules-d/dispatch/*.json\nJ5: vaep/j5/dispatch/*.json\nJ6: vaep/j6/dispatch/*.json\n```'''
    t = replace_if_present(t, old_paths, new_paths)
    write(p, t)


def transform_agents() -> None:
    p = 'AGENTS.md'
    t = read(p)
    t = t.replace('Jules A/B/C/D', 'J1/J2/J3/J4/J5/J6')
    t = t.replace('JULES_A|JULES_B|JULES_C|JULES_D', 'J1|J2|J3|J4|J5|J6')
    t = t.replace('JULES_A, JULES_B, JULES_C, JULES_D', 'J1, J2, J3, J4, J5, J6')
    if 'J1/J2/J3/J4/J5/J6' not in t:
        raise SystemExit('CUTOVER_ASSERT_AGENTS_NO_J1_J6')
    write(p, t)


def transform_registry() -> None:
    p = ROOT / 'vaep/control/jules-workers.json'
    data = json.loads(p.read_text(encoding='utf-8'))
    if data.get('cutoverEnabled') is not False:
        raise SystemExit('CUTOVER_ASSERT_REGISTRY_NOT_F1')
    data['phase'] = 'F2_ACTIVE_CUTOVER'
    data['activeLegacyWorkers'] = []
    data['activeWorkers'] = [f'J{i}' for i in range(1, 7)]
    data['preparedWorkers'] = [f'J{i}' for i in range(1, 7)]
    data['cutoverEnabled'] = True
    workflow_map = {
        'J1': '.github/workflows/vaep-jules-j1.yml',
        'J2': '.github/workflows/vaep-jules-j2.yml',
        'J3': '.github/workflows/vaep-jules-j3.yml',
        'J4': '.github/workflows/vaep-jules-j4.yml',
        'J5': '.github/workflows/vaep-jules-j5.yml',
        'J6': '.github/workflows/vaep-jules-j6.yml',
    }
    dispatch_map = {
        'J1': 'vaep/jules/dispatch', 'J2': 'vaep/jules-b/dispatch',
        'J3': 'vaep/jules-c/dispatch', 'J4': 'vaep/jules-d/dispatch',
        'J5': 'vaep/j5/dispatch', 'J6': 'vaep/j6/dispatch',
    }
    for wid, worker in data['workers'].items():
        worker['enabled'] = True
        worker['workflow'] = workflow_map[wid]
        worker['dispatchPath'] = dispatch_map[wid]
        worker['secretName'] = f'JULES_{wid}_API_KEY'
        worker['credentialMigrationPending'] = False
        worker['desiredSecretName'] = f'JULES_{wid}_API_KEY'
    p.write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def transform_registry_checker() -> None:
    p = 'scripts/vaep/j1_j6_registry_check.py'
    write(p, '''#!/usr/bin/env python3\nimport json\nfrom pathlib import Path\n\np = Path("vaep/control/jules-workers.json")\nd = json.loads(p.read_text(encoding="utf-8"))\nexpected = [f"J{i}" for i in range(1, 7)]\nassert d["phase"] == "F2_ACTIVE_CUTOVER"\nassert d["cutoverEnabled"] is True\nassert d["activeWorkers"] == expected\nassert d["activeLegacyWorkers"] == []\nassert sorted(d["workers"]) == expected\nfor wid in expected:\n    w = d["workers"][wid]\n    assert w["enabled"] is True\n    assert w["queueDepthTarget"] == 2\n    assert w["programmedBacklogTarget"] == 12\n    assert w["programmedBacklogRefillFloor"] == 4\n    assert w["maxAttempts"] == 2\n    assert w["reworkMax"] == 1\n    assert w["secretName"] == f"JULES_{wid}_API_KEY"\n    assert w["desiredSecretName"] == f"JULES_{wid}_API_KEY"\n    assert w["credentialMigrationPending"] is False\nprint("J1_J6_REGISTRY_ACTIVE_OK")\n''')


def transform_schema_and_preflight() -> None:
    sp = ROOT / 'vaep/schemas/jules-dispatch.schema.json'
    schema = json.loads(sp.read_text(encoding='utf-8'))
    schema['properties']['worker']['enum'] = ['J1','J2','J3','J4','J5','J6','CODEX','CHATGPT_VAEP']
    sp.write_text(json.dumps(schema, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    p = 'scripts/vaep/dispatch-preflight.mjs'
    t = read(p)
    t = must_replace(t, "['JULES_A', 'JULES_B', 'JULES_C', 'JULES_D', 'CODEX', 'CHATGPT_VAEP']", "['J1', 'J2', 'J3', 'J4', 'J5', 'J6', 'CODEX', 'CHATGPT_VAEP']", count=1)
    write(p, t)


def transform_primary_workflows() -> None:
    workers = {
        'J1': ('vaep/jules/dispatch', 'JULES_J1_API_KEY'),
        'J2': ('vaep/jules-b/dispatch', 'JULES_J2_API_KEY'),
        'J3': ('vaep/jules-c/dispatch', 'JULES_J3_API_KEY'),
        'J4': ('vaep/jules-d/dispatch', 'JULES_J4_API_KEY'),
        'J5': ('vaep/j5/dispatch', 'JULES_J5_API_KEY'),
        'J6': ('vaep/j6/dispatch', 'JULES_J6_API_KEY'),
    }
    for wid, (path, secret) in workers.items():
        write(f'.github/workflows/vaep-jules-{wid.lower()}.yml', operational_workflow(wid, path, secret))
    legacy = {
        '.github/workflows/vaep-jules-secondary.yml': 'Jules A',
        '.github/workflows/vaep-jules-secondary-b.yml': 'Jules B',
        '.github/workflows/vaep-jules-secondary-c.yml': 'Jules C',
        '.github/workflows/vaep-jules-secondary-d.yml': 'Jules D',
    }
    for path, name in legacy.items():
        write(path, disabled_legacy(name))


def transform_autorefill_core() -> None:
    p = '.github/scripts/vaep-jules-autorefill-core.sh'
    t = read(p)
    old = '''case "$WORKER_ID" in\n  JULES_A) DISPATCH_PATH="vaep/jules/dispatch" ;;\n  JULES_B) DISPATCH_PATH="vaep/jules-b/dispatch" ;;\n  JULES_C) DISPATCH_PATH="vaep/jules-c/dispatch" ;;\n  JULES_D) DISPATCH_PATH="vaep/jules-d/dispatch" ;;\n  *) echo "AUTOREFILL_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;\nesac'''
    new = '''case "$WORKER_ID" in\n  J1) DISPATCH_PATH="vaep/jules/dispatch" ;;\n  J2) DISPATCH_PATH="vaep/jules-b/dispatch" ;;\n  J3) DISPATCH_PATH="vaep/jules-c/dispatch" ;;\n  J4) DISPATCH_PATH="vaep/jules-d/dispatch" ;;\n  J5) DISPATCH_PATH="vaep/j5/dispatch" ;;\n  J6) DISPATCH_PATH="vaep/j6/dispatch" ;;\n  *) echo "AUTOREFILL_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;\nesac'''
    t = must_replace(t, old, new, count=1)
    old_key = '''  case "$WORKER_ID" in\n    JULES_A) printf '%s\\n' "${JULES_A_API_KEY:-}" ;;\n    JULES_B) printf '%s\\n' "${JULES_B_API_KEY:-}" ;;\n    JULES_C) printf '%s\\n' "${JULES_C_API_KEY:-}" ;;\n    JULES_D) printf '%s\\n' "${JULES_D_API_KEY:-}" ;;\n  esac'''
    new_key = '''  case "$WORKER_ID" in\n    J1) printf '%s\\n' "${J1_API_KEY:-}" ;;\n    J2) printf '%s\\n' "${J2_API_KEY:-}" ;;\n    J3) printf '%s\\n' "${J3_API_KEY:-}" ;;\n    J4) printf '%s\\n' "${J4_API_KEY:-}" ;;\n    J5) printf '%s\\n' "${J5_API_KEY:-}" ;;\n    J6) printf '%s\\n' "${J6_API_KEY:-}" ;;\n  esac'''
    t = must_replace(t, old_key, new_key, count=1)
    old_name = '''  case "$WORKER_ID" in\n    JULES_A) printf '%s\\n' "VAEP Jules A Trusted Secondary Worker" ;;\n    JULES_B) printf '%s\\n' "VAEP Jules B Trusted Secondary Worker" ;;\n    JULES_C) printf '%s\\n' "VAEP Jules C Trusted Secondary Worker" ;;\n    JULES_D) printf '%s\\n' "VAEP Jules D Trusted Secondary Worker" ;;\n  esac'''
    new_name = '''  case "$WORKER_ID" in\n    J1) printf '%s\\n' "VAEP J1 Trusted Worker" ;;\n    J2) printf '%s\\n' "VAEP J2 Trusted Worker" ;;\n    J3) printf '%s\\n' "VAEP J3 Trusted Worker" ;;\n    J4) printf '%s\\n' "VAEP J4 Trusted Worker" ;;\n    J5) printf '%s\\n' "VAEP J5 Trusted Worker" ;;\n    J6) printf '%s\\n' "VAEP J6 Trusted Worker" ;;\n  esac'''
    t = must_replace(t, old_name, new_name, count=1)
    old_file = '''  case "$WORKER_ID" in\n    JULES_A) printf '%s\\n' "vaep-jules-secondary.yml" ;;\n    JULES_B) printf '%s\\n' "vaep-jules-secondary-b.yml" ;;\n    JULES_C) printf '%s\\n' "vaep-jules-secondary-c.yml" ;;\n    JULES_D) printf '%s\\n' "vaep-jules-secondary-d.yml" ;;\n  esac'''
    new_file = '''  case "$WORKER_ID" in\n    J1) printf '%s\\n' "vaep-jules-j1.yml" ;;\n    J2) printf '%s\\n' "vaep-jules-j2.yml" ;;\n    J3) printf '%s\\n' "vaep-jules-j3.yml" ;;\n    J4) printf '%s\\n' "vaep-jules-j4.yml" ;;\n    J5) printf '%s\\n' "vaep-jules-j5.yml" ;;\n    J6) printf '%s\\n' "vaep-jules-j6.yml" ;;\n  esac'''
    t = must_replace(t, old_file, new_file, count=1)
    t = t.replace('vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|', 'vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|vaep/j5/dispatch/*.json|vaep/j6/dispatch/*.json|')
    write(p, t)


def transform_catalog_floor() -> None:
    p = '.github/scripts/vaep-jules-catalog-floor.sh'
    t = read(p)
    old = '''case "$WORKER_ID" in\n  JULES_A) DISPATCH_PATH="vaep/jules/dispatch" ;;\n  JULES_B) DISPATCH_PATH="vaep/jules-b/dispatch" ;;\n  JULES_C) DISPATCH_PATH="vaep/jules-c/dispatch" ;;\n  JULES_D) DISPATCH_PATH="vaep/jules-d/dispatch" ;;\n  *) echo "CATALOG_FLOOR_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;\nesac'''
    new = '''case "$WORKER_ID" in\n  J1) DISPATCH_PATH="vaep/jules/dispatch" ;;\n  J2) DISPATCH_PATH="vaep/jules-b/dispatch" ;;\n  J3) DISPATCH_PATH="vaep/jules-c/dispatch" ;;\n  J4) DISPATCH_PATH="vaep/jules-d/dispatch" ;;\n  J5) DISPATCH_PATH="vaep/j5/dispatch" ;;\n  J6) DISPATCH_PATH="vaep/j6/dispatch" ;;\n  *) echo "CATALOG_FLOOR_ERROR=unknown_worker worker=$WORKER_ID" >&2; exit 2 ;;\nesac'''
    t = must_replace(t, old, new, count=1)
    write(p, t)


def transform_worker_script() -> None:
    p = '.github/scripts/vaep-jules-worker.sh'
    t = read(p)
    t = t.replace('${WORKER_ID:-JULES_A}', '${WORKER_ID:-UNKNOWN}')
    t = must_replace(t,
        '[[ ${#changed_files[@]} -ge 1 && ${#changed_files[@]} -le 4 ]] || fail "MASTER atomic dispatch batch must contain 1..4 files." 22',
        '[[ ${#changed_files[@]} -ge 1 && ${#changed_files[@]} -le 6 ]] || fail "MASTER atomic dispatch batch must contain 1..6 files." 22',
        count=1)
    t = must_replace(t,
        '[[ "$changed" =~ ^vaep/jules(-b|-c|-d)?/dispatch/[^/]+\\.json$ ]] || fail "MASTER atomic dispatch batch contains a non-dispatch file: $changed" 22',
        '[[ "$changed" =~ ^(vaep/jules/dispatch|vaep/jules-b/dispatch|vaep/jules-c/dispatch|vaep/jules-d/dispatch|vaep/j5/dispatch|vaep/j6/dispatch)/[^/]+\\.json$ ]] || fail "MASTER atomic dispatch batch contains a non-dispatch file: $changed" 22',
        count=1)
    t = t.replace('all four lanes', 'all six lanes')
    write(p, t)


def transform_autorefill_wrapper() -> None:
    p = '.github/scripts/vaep-jules-autorefill.sh'
    t = read(p)
    guard = '''\n# J1-J6 cutover guard: a legacy lane may finish an already-created session,\n# but it must never publish/refill new legacy work after cutover.\nif [[ -f vaep/control/jules-workers.json ]] && jq -e '.cutoverEnabled == true' vaep/control/jules-workers.json >/dev/null 2>&1; then\n  case "${WORKER_ID:-}" in\n    JULES_A|JULES_B|JULES_C|JULES_D)\n      echo "AUTOREFILL_LEGACY_NOOP worker=${WORKER_ID} reason=J1_J6_CUTOVER_ACTIVE"\n      exit 0\n      ;;\n  esac\nfi\n'''
    anchor = 'readonly BRANCH="Desarrollo"\n'
    if guard.strip() not in t:
        t = must_replace(t, anchor, anchor + guard, count=1)
    write(p, t)


def transform_checkpoint_script() -> None:
    p = '.github/scripts/vaep-checkpoint.sh'
    t = read(p)
    t = must_replace(t, 'readonly WORKERS=(JULES_A JULES_B JULES_C JULES_D)', 'readonly WORKERS=(J1 J2 J3 J4 J5 J6)', count=1)
    t = t.replace('[--worker JULES_A|JULES_B|JULES_C|JULES_D]', '[--worker J1|J2|J3|J4|J5|J6]')
    old = '''  case "${1:-}" in\n    JULES_A) printf '%s\\n' "${JULES_A_API_KEY:-${JULES_API_KEY:-}}" ;;\n    JULES_B) printf '%s\\n' "${JULES_B_API_KEY:-${JULES_API_KEY:-}}" ;;\n    JULES_C) printf '%s\\n' "${JULES_C_API_KEY:-${JULES_API_KEY:-}}" ;;\n    JULES_D) printf '%s\\n' "${JULES_D_API_KEY:-${JULES_API_KEY:-}}" ;;\n    *) printf '\\n' ;;\n  esac'''
    new = '''  case "${1:-}" in\n    J1) printf '%s\\n' "${J1_API_KEY:-}" ;;\n    J2) printf '%s\\n' "${J2_API_KEY:-}" ;;\n    J3) printf '%s\\n' "${J3_API_KEY:-}" ;;\n    J4) printf '%s\\n' "${J4_API_KEY:-}" ;;\n    J5) printf '%s\\n' "${J5_API_KEY:-}" ;;\n    J6) printf '%s\\n' "${J6_API_KEY:-}" ;;\n    JULES_A) printf '%s\\n' "${JULES_A_API_KEY:-}" ;;\n    JULES_B) printf '%s\\n' "${JULES_B_API_KEY:-}" ;;\n    JULES_C) printf '%s\\n' "${JULES_C_API_KEY:-}" ;;\n    JULES_D) printf '%s\\n' "${JULES_D_API_KEY:-}" ;;\n    *) printf '\\n' ;;\n  esac'''
    t = must_replace(t, old, new, count=1)
    t = t.replace('capture("- Worker: `(?<worker>JULES_[ABCD])`")', 'capture("- Worker: `(?<worker>J[1-6]|JULES_[ABCD])`")')
    t = t.replace('test("^VAEP Jules [ABCD] Trusted Secondary Worker$")', 'test("^VAEP J[1-6] Trusted Worker$")')
    write(p, t)


def transform_checkpoint_workflow() -> None:
    p = '.github/workflows/vaep-checkpoints.yml'
    t = read(p)
    t = must_replace(t, 'worker: [JULES_A, JULES_B, JULES_C, JULES_D]', 'worker: [J1, J2, J3, J4, J5, J6]', count=1)
    old = '''      JULES_A_API_KEY: ${{ secrets.JULES_API_KEY }}\n      JULES_B_API_KEY: ${{ secrets.JULES_B_API_KEY }}\n      JULES_C_API_KEY: ${{ secrets.JULES_C_API_KEY }}\n      JULES_D_API_KEY: ${{ secrets.JULES_D_API_KEY }}'''
    new = '''      J1_API_KEY: ${{ secrets.JULES_J1_API_KEY }}\n      J2_API_KEY: ${{ secrets.JULES_J2_API_KEY }}\n      J3_API_KEY: ${{ secrets.JULES_J3_API_KEY }}\n      J4_API_KEY: ${{ secrets.JULES_J4_API_KEY }}\n      J5_API_KEY: ${{ secrets.JULES_J5_API_KEY }}\n      J6_API_KEY: ${{ secrets.JULES_J6_API_KEY }}\n      JULES_A_API_KEY: ${{ secrets.JULES_API_KEY }}\n      JULES_B_API_KEY: ${{ secrets.JULES_B_API_KEY }}\n      JULES_C_API_KEY: ${{ secrets.JULES_C_API_KEY }}\n      JULES_D_API_KEY: ${{ secrets.JULES_D_API_KEY }}'''
    t = must_replace(t, old, new, count=1)
    write(p, t)


def transform_parent_close() -> None:
    p = '.github/scripts/vaep-parent-close.sh'
    t = read(p)
    t = t.replace('vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|', 'vaep/jules/dispatch/*.json|vaep/jules-b/dispatch/*.json|vaep/jules-c/dispatch/*.json|vaep/jules-d/dispatch/*.json|vaep/j5/dispatch/*.json|vaep/j6/dispatch/*.json|')
    write(p, t)


def transform_metrics() -> None:
    p = 'scripts/vaep/jules_integration_metrics.py'
    t = read(p)
    t = must_replace(t,
        'WORKERS = {"JULES_A", "JULES_B", "JULES_C", "JULES_D"}',
        'ACTIVE_WORKERS = {"J1", "J2", "J3", "J4", "J5", "J6"}\nLEGACY_ALIASES = {"JULES_A":"J1", "JULES_B":"J2", "JULES_C":"J3", "JULES_D":"J4"}\nWORKERS = ACTIVE_WORKERS | set(LEGACY_ALIASES)',
        count=1)
    t = t.replace('errors.append("Worker must be JULES_A/B/C/D")', 'errors.append("Worker must be J1..J6 (legacy A-D accepted only for historical receipts)")')
    t = t.replace('re.match(r"^vaep/jules(-b|-c|-d)?/dispatch/", path)', 're.match(r"^(?:vaep/jules(?:-b|-c|-d)?/dispatch|vaep/j[56]/dispatch)/", path)')
    t = t.replace('re.match(r"^vaep/jules(-b|-c|-d)?/dispatch/[^/]+\\.json$", manifest_path)', 're.match(r"^(?:vaep/jules(?:-b|-c|-d)?/dispatch|vaep/j[56]/dispatch)/[^/]+\\.json$", manifest_path)')
    old = '    by_worker = Counter(item["trailers"]["Worker"] for item in valid)'
    new = '    by_worker = Counter(LEGACY_ALIASES.get(item["trailers"]["Worker"], item["trailers"]["Worker"]) for item in valid)'
    t = must_replace(t, old, new, count=1)
    t = t.replace('for worker in sorted(WORKERS)}', 'for worker in sorted(ACTIVE_WORKERS)}')
    t = t.replace('for worker in sorted(WORKERS)},', 'for worker in sorted(ACTIVE_WORKERS)},')
    write(p, t)


def transform_terminal_handoff() -> None:
    p = 'scripts/vaep/terminal_handoff.py'
    t = read(p)
    old = 'choices=["JULES_A", "JULES_B", "JULES_C", "JULES_D"]'
    new = 'choices=["J1", "J2", "J3", "J4", "J5", "J6", "JULES_A", "JULES_B", "JULES_C", "JULES_D"]'
    t = must_replace(t, old, new, count=1)
    write(p, t)


def transform_stop_workflow() -> None:
    p = '.github/workflows/vaep-jules-stop.yml'
    t = read(p)
    old_env = '''      JULES_A_API_KEY: ${{ secrets.JULES_API_KEY }}\n      JULES_B_API_KEY: ${{ secrets.JULES_B_API_KEY }}\n      JULES_C_API_KEY: ${{ secrets.JULES_C_API_KEY }}\n      JULES_D_API_KEY: ${{ secrets.JULES_D_API_KEY }}'''
    new_env = '''      J1_API_KEY: ${{ secrets.JULES_J1_API_KEY }}\n      J2_API_KEY: ${{ secrets.JULES_J2_API_KEY }}\n      J3_API_KEY: ${{ secrets.JULES_J3_API_KEY }}\n      J4_API_KEY: ${{ secrets.JULES_J4_API_KEY }}\n      J5_API_KEY: ${{ secrets.JULES_J5_API_KEY }}\n      J6_API_KEY: ${{ secrets.JULES_J6_API_KEY }}\n      JULES_A_API_KEY: ${{ secrets.JULES_API_KEY }}\n      JULES_B_API_KEY: ${{ secrets.JULES_B_API_KEY }}\n      JULES_C_API_KEY: ${{ secrets.JULES_C_API_KEY }}\n      JULES_D_API_KEY: ${{ secrets.JULES_D_API_KEY }}'''
    t = must_replace(t, old_env, new_env, count=1)
    t = t.replace('((.workerId // "JULES_A") | type == "string" and test("^JULES_[ABCD]$"))', '((.workerId // "") | type == "string" and test("^(J[1-6]|JULES_[ABCD])$"))')
    t = t.replace("printf 'worker_id=%s\\n' \"$(jq -r '.workerId // \\\"JULES_A\\\"' \"$manifest\")\"", "printf 'worker_id=%s\\n' \"$(jq -r '.workerId // \\\"\\\"' \"$manifest\")\"")
    old_cases = '''            JULES_A) selected_key="$JULES_A_API_KEY"; title_prefix="VAEP " ;;\n            JULES_B) selected_key="$JULES_B_API_KEY"; title_prefix="VAEP-B " ;;\n            JULES_C) selected_key="$JULES_C_API_KEY"; title_prefix="VAEP-C " ;;\n            JULES_D) selected_key="$JULES_D_API_KEY"; title_prefix="VAEP-D " ;;'''
    new_cases = '''            J1) selected_key="$J1_API_KEY"; title_prefix="VAEP-J1 " ;;\n            J2) selected_key="$J2_API_KEY"; title_prefix="VAEP-J2 " ;;\n            J3) selected_key="$J3_API_KEY"; title_prefix="VAEP-J3 " ;;\n            J4) selected_key="$J4_API_KEY"; title_prefix="VAEP-J4 " ;;\n            J5) selected_key="$J5_API_KEY"; title_prefix="VAEP-J5 " ;;\n            J6) selected_key="$J6_API_KEY"; title_prefix="VAEP-J6 " ;;\n            JULES_A) selected_key="$JULES_A_API_KEY"; title_prefix="VAEP " ;;\n            JULES_B) selected_key="$JULES_B_API_KEY"; title_prefix="VAEP-B " ;;\n            JULES_C) selected_key="$JULES_C_API_KEY"; title_prefix="VAEP-C " ;;\n            JULES_D) selected_key="$JULES_D_API_KEY"; title_prefix="VAEP-D " ;;'''
    t = must_replace(t, old_cases, new_cases, count=1)
    write(p, t)


def transform_catalogs() -> None:
    aliases = {'JULES_A':'J1','JULES_B':'J2','JULES_C':'J3','JULES_D':'J4'}
    p = ROOT / 'vaep/control/jules-autorefill-catalog.json'
    data = json.loads(p.read_text(encoding='utf-8'))
    lanes = data.get('lanes', {})
    new_lanes = {}
    for key, value in lanes.items():
        new_lanes[aliases.get(key, key)] = value
    for wid in [f'J{i}' for i in range(1,7)]:
        new_lanes.setdefault(wid, [])
    def walk(v):
        if isinstance(v, dict):
            for k, x in list(v.items()):
                if k == 'workerId' and x in aliases:
                    v[k] = aliases[x]
                else:
                    walk(x)
        elif isinstance(v, list):
            for x in v: walk(x)
    walk(new_lanes)
    data['lanes'] = new_lanes
    data.setdefault('policy', {})['programmedBacklogTargetTotal'] = 72
    data['policy']['materialParallelismTargetPerParent'] = 6
    data['policy']['activeWorkerCount'] = 6
    data['policy']['activeWorkers'] = [f'J{i}' for i in range(1,7)]
    p.write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')

    fp = ROOT / 'vaep/control/jules-completed-semantic-facets.json'
    if fp.exists():
        d = json.loads(fp.read_text(encoding='utf-8'))
        old_lanes = d.get('lanes', {})
        mapped = {aliases.get(k,k):v for k,v in old_lanes.items()}
        for wid in [f'J{i}' for i in range(1,7)]: mapped.setdefault(wid, [])
        d['lanes'] = mapped
        fp.write_text(json.dumps(d, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def transform_engine_ci() -> None:
    p = '.github/workflows/vaep-engine-ci.yml'
    t = read(p)
    t = t.replace('for wf in .github/workflows/vaep-jules-secondary{,-b,-c,-d}.yml; do', 'for wf in .github/workflows/vaep-jules-j{1,2,3,4,5,6}.yml; do')
    t = t.replace('grep -q "VAEP_LANE_RUN_ID" .github/workflows/vaep-jules-secondary.yml', 'grep -q "VAEP_LANE_RUN_ID" .github/workflows/vaep-jules-j1.yml')
    old = '''          grep -q "JULES_A_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "JULES_B_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "JULES_C_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "JULES_D_API_KEY:" .github/workflows/vaep-checkpoints.yml'''
    new = '''          grep -q "J1_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "J2_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "J3_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "J4_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "J5_API_KEY:" .github/workflows/vaep-checkpoints.yml\n          grep -q "J6_API_KEY:" .github/workflows/vaep-checkpoints.yml'''
    t = must_replace(t, old, new, count=1)
    insert_after = "      - name: Validate VAEP/Jules MASTER\n        run: bash .github/scripts/vaep-jules-master.sh --static-self-test\n"
    extra = insert_after + "      - name: Validate J1-J6 active registry\n        run: python3 scripts/vaep/j1_j6_registry_check.py\n"
    t = must_replace(t, insert_after, extra, count=1)
    write(p, t)


def final_assertions() -> None:
    registry = json.loads(read('vaep/control/jules-workers.json'))
    assert registry['cutoverEnabled'] is True
    assert registry['activeWorkers'] == [f'J{i}' for i in range(1,7)]
    for i in range(1, 7):
        worker = registry['workers'][f'J{i}']
        assert worker['secretName'] == f'JULES_J{i}_API_KEY'
        assert worker['desiredSecretName'] == f'JULES_J{i}_API_KEY'
        assert worker['credentialMigrationPending'] is False
    master = read('docs/VAEP_AUTHORITY.md')
    for needle in ['JULES_TASKS_TARGET_ROLLING_24H_TOTAL=600','JULES_PROGRAMMED_BACKLOG_TARGET_TOTAL=72','LANE_REFILL_DEADLINE_SECONDS=30','JULES_ACTIVE_WORKERS=J1,J2,J3,J4,J5,J6']:
        assert needle in master, needle
    assert 'J3 y J4 usan exclusivamente sus secretos nominales' in master
    schema = json.loads(read('vaep/schemas/jules-dispatch.schema.json'))
    assert schema['properties']['worker']['enum'][:6] == [f'J{i}' for i in range(1,7)]
    for i in range(1,7):
        wf = read(f'.github/workflows/vaep-jules-j{i}.yml')
        assert f'WORKER_ID: J{i}' in wf
        assert 'Execute VAEP/Jules MASTER' in wf
        assert f'secrets.JULES_J{i}_API_KEY' in wf
    assert 'secrets.JULES_C_API_KEY' not in read('.github/workflows/vaep-jules-j3.yml')
    assert 'secrets.JULES_D_API_KEY' not in read('.github/workflows/vaep-jules-j4.yml')
    checkpoints = read('.github/workflows/vaep-checkpoints.yml')
    assert 'J3_API_KEY: ${{ secrets.JULES_J3_API_KEY }}' in checkpoints
    assert 'J4_API_KEY: ${{ secrets.JULES_J4_API_KEY }}' in checkpoints
    stop = read('.github/workflows/vaep-jules-stop.yml')
    assert 'J3_API_KEY: ${{ secrets.JULES_J3_API_KEY }}' in stop
    assert 'J4_API_KEY: ${{ secrets.JULES_J4_API_KEY }}' in stop
    cat = json.loads(read('vaep/control/jules-autorefill-catalog.json'))
    assert all(f'J{i}' in cat['lanes'] for i in range(1,7))
    assert not any(k.startswith('JULES_') for k in cat['lanes'])
    print('J1_J6_CUTOVER_TRANSFORM=PASS')


def main() -> None:
    transform_master()
    transform_agents()
    transform_registry()
    transform_registry_checker()
    transform_schema_and_preflight()
    transform_primary_workflows()
    transform_autorefill_core()
    transform_catalog_floor()
    transform_worker_script()
    transform_autorefill_wrapper()
    transform_checkpoint_script()
    transform_checkpoint_workflow()
    transform_parent_close()
    transform_metrics()
    transform_terminal_handoff()
    transform_stop_workflow()
    transform_catalogs()
    transform_engine_ci()
    final_assertions()


if __name__ == '__main__':
    main()
