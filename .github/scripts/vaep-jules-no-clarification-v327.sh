#!/usr/bin/env bash
set -euo pipefail
readonly VAEP_JULES_CLARIFICATION_GUARD="v3.28"
readonly BASE_WORKER=".github/scripts/vaep-jules-worker-v320.sh"
readonly BASE_GUARD=".github/scripts/vaep-jules-throughput-guard-v326.sh"
: "${RUNNER_TEMP:=${TMPDIR:-/tmp}}"
tmp="$RUNNER_TEMP/vaep-jules-v328-${GITHUB_RUN_ID:-local}-$$"
mkdir -p "$tmp"
trap 'rm -rf "$tmp"' EXIT
patched_worker="$tmp/vaep-jules-worker-v328.sh"
patched_guard="$tmp/vaep-jules-throughput-guard-v328.sh"
cp "$BASE_WORKER" "$patched_worker"
cp "$BASE_GUARD" "$patched_guard"
python3 - "$patched_worker" <<'PY'
from pathlib import Path
import sys
path=Path(sys.argv[1]); lines=path.read_text(encoding='utf-8').splitlines()
replacement=r'''routine_prompt="VAEP Jules v3.28 deterministic clarification recovery. Continue autonomously inside the SAME assigned exclusive scope; do not wait for a human when the missing input can be derived from the dispatch, repository, docs, tests, direct dependencies or already visible session evidence. ORIGINAL_ASSIGNED_MICROTASK: $user_prompt FILE_SCOPE_HINT=$file_scope. If you asked for URLs, queries, claims, examples, files, test targets or similar routine inputs that were not enumerated, derive a bounded reproducible target set yourself from the assigned scope/direct dependencies: prefer 5-10 concrete targets when available; for URL/grounding work scan in-scope repository content for explicit http/https URLs and factual claims and verify those; for code/QA work select the exact files/contracts/tests named or directly implied by the task. If fewer than 5 valid targets exist, use all valid targets and state the count. If no valid target exists after a reproducible search, do NOT remain in AWAITING_USER_FEEDBACK: report NO_TARGETS_FOUND with the exact searches/paths inspected, produce the allowed evidence-only artifact/patch when the task permits it, emit SELF_REVIEW_PASS_1 and SELF_REVIEW_PASS_2, and COMPLETE. Ask for human input only for a true business decision, explicit authorization, secret/credential, destructive action, or an external resource that cannot be safely inferred. Never invent endpoints, permissions, URLs, facts or requirements. TASK_ATTEMPT=$task_attempt; maximum attempts=2; R2 final; R3+ prohibited. Do not expand scope or promote N+1. COMPLETED never equals LISTO. Preserve a reviewable ChangeSet/gitPatch with exact baseCommitId, causal evidence, limitations and tests not executed."'''
count=0
for i,line in enumerate(lines):
    if line.startswith('routine_prompt='): lines[i]=replacement; count+=1
if count!=1: raise SystemExit(f'v3.28 expected one routine_prompt, found {count}')
path.write_text('\n'.join(lines)+'\n',encoding='utf-8')
PY
python3 - "$patched_guard" "$patched_worker" <<'PY'
from pathlib import Path
import sys
guard=Path(sys.argv[1]); worker=sys.argv[2]; text=guard.read_text(encoding='utf-8')
old='readonly INNER_WORKER=".github/scripts/vaep-jules-worker-v320.sh"'
if text.count(old)!=1: raise SystemExit('v3.28 INNER_WORKER invariant failed')
text=text.replace(old,f'readonly INNER_WORKER="{worker}"')
old_delete='readonly DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT=true'
if text.count(old_delete)!=1: raise SystemExit('v3.28 delete-session invariant failed')
text=text.replace(old_delete,'readonly DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT=false')
text=text.replace('[[ "$DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT" == true ]]','[[ "$DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT" == false ]]',1)
text=text.replace('"deleteOrphanedRemoteSessionOnTimeout":true','"deleteOrphanedRemoteSessionOnTimeout":false',1)
guard.write_text(text,encoding='utf-8')
PY
chmod +x "$patched_worker" "$patched_guard"
if [[ "${1:-}" == "--static-self-test" ]]; then
  grep -q 'NO_TARGETS_FOUND' "$patched_worker"
  grep -q 'do not wait for a human' "$patched_worker"
  grep -q 'DELETE_ORPHANED_REMOTE_SESSION_ON_TIMEOUT=false' "$patched_guard"
  bash "$patched_guard" --static-self-test >/dev/null
  printf '{"status":"ok","clarificationGuard":"%s","deterministicRepoFallback":true,"deleteActiveSessionOnMonitorTimeout":false}\n' "$VAEP_JULES_CLARIFICATION_GUARD"
  exit 0
fi
exec bash "$patched_guard"
