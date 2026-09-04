#!/usr/bin/env bash
set -euo pipefail

# Single operational entrypoint. Rules come only from docs/VAEP_AUTHORITY.md.
readonly MASTER_FILE="docs/VAEP_AUTHORITY.md"
readonly WORKER_TEMPLATE=".github/scripts/vaep-jules-worker-template.sh"
readonly GUARD_TEMPLATE=".github/scripts/vaep-jules-throughput-template.sh"

test -f "$MASTER_FILE"
test -f "$WORKER_TEMPLATE"
test -f "$GUARD_TEMPLATE"

: "${RUNNER_TEMP:=${TMPDIR:-/tmp}}"
tmp="$RUNNER_TEMP/vaep-jules-master-${GITHUB_RUN_ID:-local}-$$"
mkdir -p "$tmp"
trap 'rm -rf "$tmp"' EXIT
worker="$tmp/worker.sh"
guard="$tmp/guard.sh"
cp "$WORKER_TEMPLATE" "$worker"
cp "$GUARD_TEMPLATE" "$guard"

python3 - "$worker" "$guard" <<'PY'
from pathlib import Path
import sys

worker=Path(sys.argv[1])
guard=Path(sys.argv[2])

w=worker.read_text(encoding="utf-8")
for old in ("v3.25","v3.26","v3.27","v3.28","v3.29","V3.25_CURRENT","VAEP_V4_6_KEYED_MUTEX_HARD_EXECUTION"):
    w=w.replace(old,"MASTER")
lines=w.splitlines()
replacement=r'''routine_prompt="VAEP/Jules MASTER deterministic clarification recovery. Read docs/VAEP_AUTHORITY.md as the only operational rule source. Continue autonomously inside the SAME assigned exclusive scope; do not wait for a human when missing input can be derived from the dispatch, repository, tests, direct dependencies or visible session evidence. ORIGINAL_ASSIGNED_MICROTASK: $user_prompt FILE_SCOPE_HINT=$file_scope. If routine targets were not enumerated, derive a bounded reproducible set from the assigned scope/direct dependencies. If no valid target exists after a reproducible search, report NO_TARGETS_FOUND with exact searches/paths, produce allowed evidence, complete both self-reviews and finish. Ask for human input only for a true business decision, explicit authorization, secret/credential, destructive action, or external resource that cannot be safely inferred. Never invent endpoints, permissions, URLs, facts or requirements. TASK_ATTEMPT=$task_attempt; maximum attempts=2; R2 final; R3+ prohibited. Do not expand scope or promote N+1. COMPLETED never equals LISTO. Preserve a reviewable ChangeSet/gitPatch with exact baseCommitId, causal evidence, limitations and tests not executed."'''
count=0
for i,line in enumerate(lines):
    if line.startswith("routine_prompt="):
        lines[i]=replacement
        count+=1
if count != 1:
    raise SystemExit(f"MASTER expected one routine_prompt, found {count}")
worker.write_text("\n".join(lines)+"\n",encoding="utf-8")

g=guard.read_text(encoding="utf-8")
for old in ("v3.25","v3.26","v3.27","v3.28","v3.29","V3.25_CURRENT"):
    g=g.replace(old,"MASTER")
for line in g.splitlines():
    if line.startswith('readonly INNER_WORKER='):
        old=line
        break
else:
    raise SystemExit("MASTER INNER_WORKER invariant failed")
g=g.replace(old,f'readonly INNER_WORKER="{worker}"',1)
g=g.replace('readonly DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT=true',
            'readonly DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT=false',1)
g=g.replace('[[ "$DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT" == true ]]',
            '[[ "$DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT" == false ]]',1)
g=g.replace('"deleteOrphanedRemoteSessionOnTimeout":true',
            '"deleteOrphanedRemoteSessionOnTimeout":false',1)
guard.write_text(g,encoding="utf-8")
PY

chmod +x "$worker" "$guard"

if [[ "${1:-}" == "--static-self-test" ]]; then
  bash "$guard" --static-self-test
  exit 0
fi

exec bash "$guard"
