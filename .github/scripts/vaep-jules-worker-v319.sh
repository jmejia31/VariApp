#!/usr/bin/env bash
set -euo pipefail

: "${RUNNER_TEMP:?RUNNER_TEMP is required}"

legacy_worker=".github/scripts/vaep-jules-worker-v313.sh"
runtime_worker="$RUNNER_TEMP/vaep-jules-worker-v319-runtime.sh"

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
    'work="$RUNNER_TEMP/vaep-jules-v319"',
)

old_validation = '''mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
[[ ${#changed_files[@]} -eq 1 && "${changed_files[0]}" == "$manifest" ]] || fail "Dispatch commit must change exactly the single new manifest." 22'''
new_validation = '''mapfile -t changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA")
[[ ${#changed_files[@]} -ge 1 ]] || fail "Dispatch commit has no changed files." 22
for changed in "${changed_files[@]}"; do
  [[ "$changed" =~ ^vaep/jules(-[b-d])?/dispatch/[A-Za-z0-9_.-]+\\.json$ ]] || fail "v3.19 atomic dispatch contains non-dispatch file: $changed" 22
done
mapfile -t worker_changed_files < <(git diff-tree --no-commit-id --name-only -r "$GITHUB_SHA" -- "$DISPATCH_PATH/*.json")
[[ ${#worker_changed_files[@]} -eq 1 && "${worker_changed_files[0]}" == "$manifest" ]] || fail "Expected exactly one v3.19 manifest for ${WORKER_ID:-JULES_A}." 22'''
if old_validation not in text:
    raise SystemExit("v3.19 adapter: dispatch validation block not found")
text = text.replace(old_validation, new_validation)

identity_needle = '    "PROJECT_ID=VARIAPP" \\\n'
identity_replacement = (
    identity_needle
    + '    "VAEP_JULES_PROTOCOL=v3.19" \\\n'
    + '    "VAEP_AUTHORITY_FILE=docs/VAEP_AUTHORITY.md" \\\n'
)
if identity_needle not in text:
    raise SystemExit("v3.19 adapter: identity prompt anchor not found")
text = text.replace(identity_needle, identity_replacement, 1)

text = text.replace(
    'Before changing anything, read AGENTS.md and docs/VAEP_JULES.md. Confirm repository, branch and assigned scope.',
    'Before changing anything, read docs/VAEP_AUTHORITY.md, AGENTS.md and docs/VAEP_JULES.md. Confirm repository, branch, VAEP Jules protocol v3.19 and assigned scope.',
)

stale_prompt = 'VAEP v3.13 automated follow-up.'
current_prompt = (
    'VAEP Jules integration v3.19 automated follow-up. '
    'Global ChatGPT/VAEP control-plane remains governed by CONFIG.RUNNER_PROTOCOL_VERSION.'
)
if stale_prompt not in text:
    raise SystemExit("v3.19 adapter: stale v3.13 follow-up anchor not found")
text = text.replace(stale_prompt, current_prompt)

runtime_path.write_text(text, encoding="utf-8")
PY

chmod +x "$runtime_worker"
exec bash "$runtime_worker"
