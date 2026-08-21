#!/usr/bin/env bash
set -euo pipefail

: "${RUNNER_TEMP:?RUNNER_TEMP is required}"
: "${DISPATCH_PATH:?DISPATCH_PATH is required}"
: "${GITHUB_SHA:?GITHUB_SHA is required}"

# v3.20 hard gate: one worker-specific manifest per workflow invocation and
# at most two CONTENT attempts for the same logical task. taskAttempt is
# canonical for new manifests; R2 is accepted as the second/last attempt.
mapfile -t attempt_manifests < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
[[ ${#attempt_manifests[@]} -eq 1 ]] || {
  echo "v3.20 expected exactly one manifest under $DISPATCH_PATH for this worker; found ${#attempt_manifests[@]}" >&2
  exit 72
}

attempt_manifest="${attempt_manifests[0]}"
read -r dispatch_id task_attempt < <(python3 - "$attempt_manifest" <<'PY'
import json
import re
import sys
from pathlib import Path

manifest = Path(sys.argv[1])
data = json.loads(manifest.read_text(encoding="utf-8"))
dispatch_id = str(data.get("dispatchId") or manifest.stem)
explicit = data.get("taskAttempt")

m = re.search(r"(?:^|-)R(\d+)(?:-|$)", dispatch_id, flags=re.IGNORECASE)
label_attempt = int(m.group(1)) if m else None

if explicit is None:
    if label_attempt is not None:
        attempt = label_attempt
    else:
        attempt = 1
else:
    try:
        attempt = int(explicit)
    except (TypeError, ValueError):
        raise SystemExit("v3.20 taskAttempt must be integer 1 or 2")

if label_attempt is not None and label_attempt >= 3:
    raise SystemExit(f"v3.20 rejects Jules R3+: {dispatch_id}")
if attempt not in (1, 2):
    raise SystemExit(f"v3.20 rejects taskAttempt={attempt}; allowed=1,2")
if label_attempt == 2 and attempt != 2:
    raise SystemExit("v3.20 R2 dispatch must declare/infer taskAttempt=2")

print(dispatch_id, attempt)
PY
) || {
  echo "v3.20 retry-cap validation failed for $attempt_manifest" >&2
  exit 73
}

export VAEP_TASK_ATTEMPT="$task_attempt"
export VAEP_DISPATCH_ID="$dispatch_id"
export JULES_MAX_ATTEMPTS_PER_TASK=2
export JULES_REWORK_MAX=1

legacy_worker=".github/scripts/vaep-jules-worker-v313.sh"
runtime_worker="$RUNNER_TEMP/vaep-jules-worker-v320-runtime.sh"

[[ -f "$legacy_worker" ]] || {
  echo "Legacy common worker not found: $legacy_worker" >&2
  exit 70
}

python3 - "$legacy_worker" "$runtime_worker" <<'PY'
from pathlib import Path
import sys

source_path = Path(sys.argv[1])
runtime_path = Path(sys.argv[2])
text = source_path.read_text(encoding="utf-8")

text = text.replace(
    'work="$RUNNER_TEMP/vaep-jules-v313"',
    'work="$RUNNER_TEMP/vaep-jules-v320"',
)

old_validation = '''mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
[[ ${#changed_files[@]} -eq 1 && "${changed_files[0]}" == "$manifest" ]] || fail "Dispatch commit must change exactly the single new manifest." 22'''
new_validation = '''mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
[[ ${#changed_files[@]} -ge 1 ]] || fail "Dispatch commit has no changed files." 22
for changed in "${changed_files[@]}"; do
  [[ "$changed" =~ ^vaep/jules(-[b-d])?/dispatch/[A-Za-z0-9_.-]+\\.json$ ]] || fail "v3.20 atomic dispatch contains non-dispatch file: $changed" 22
done
mapfile -t worker_changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
[[ ${#worker_changed_files[@]} -eq 1 && "${worker_changed_files[0]}" == "$manifest" ]] || fail "Expected exactly one v3.20 manifest for ${WORKER_ID:-JULES_A}." 22'''
if old_validation not in text:
    raise SystemExit("v3.20 adapter: dispatch validation block not found")
text = text.replace(old_validation, new_validation)

identity_needle = '    "PROJECT_ID=VARIAPP" \\\n'
identity_replacement = (
    identity_needle
    + '    "VAEP_JULES_PROTOCOL=v3.20" \\\n'
    + '    "VAEP_AUTHORITY_FILE=docs/VAEP_AUTHORITY.md" \\\n'
    + '    "VAEP_RETRY_POLICY_FILE=docs/VAEP_V320_RETRY_CAP.md" \\\n'
    + '    "TASK_ATTEMPT=${VAEP_TASK_ATTEMPT}" \\\n'
    + '    "JULES_MAX_ATTEMPTS_PER_TASK=2" \\\n'
    + '    "JULES_REWORK_MAX=1" \\\n'
)
if identity_needle not in text:
    raise SystemExit("v3.20 adapter: identity prompt anchor not found")
text = text.replace(identity_needle, identity_replacement, 1)

text = text.replace(
    'Before changing anything, read AGENTS.md and docs/VAEP_JULES.md. Confirm repository, branch and assigned scope.',
    'Before changing anything, read docs/VAEP_AUTHORITY.md, docs/VAEP_V320_RETRY_CAP.md, AGENTS.md and docs/VAEP_JULES.md. Confirm repository, branch, VAEP Jules protocol v3.20, TASK_ATTEMPT and assigned scope. HARD RULE: this logical task allows only ATTEMPT=1 plus one final correction ATTEMPT=2/R2. Never request, propose or perform Jules R3+. If ATTEMPT=2 still has a blocking defect, report it precisely for ChatGPT/VAEP/Vibe QA takeover and finish your evidence; do not start a third round.',
)

stale_prompt = 'VAEP v3.13 automated follow-up.'
current_prompt = (
    'VAEP Jules integration v3.20 automated follow-up. '
    'Global ChatGPT/VAEP control-plane remains governed by CONFIG.RUNNER_PROTOCOL_VERSION. '
    'Retry cap is hard: maximum two Jules content attempts per logical task; R2 is final; R3+ prohibited; after failed R2 ownership transfers to ChatGPT/VAEP/Vibe QA and this Jules moves to next safe work.'
)
if stale_prompt not in text:
    raise SystemExit("v3.20 adapter: stale v3.13 follow-up anchor not found")
text = text.replace(stale_prompt, current_prompt)

runtime_path.write_text(text, encoding="utf-8")
PY

chmod +x "$runtime_worker"
exec bash "$runtime_worker"
