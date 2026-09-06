#!/usr/bin/env bash
set -euo pipefail

# A terminal hook may be running from an older manifest checkout while the
# control-plane has already advanced. Never let stale autorefill logic create a
# semantically duplicated task under a new numeric id. Compare the local
# controller inputs with the current Desarrollo versions; if any differ, leave
# refill to a fresh run/watchdog instead of publishing from stale logic.
if git remote get-url origin >/dev/null 2>&1; then
  git fetch --quiet origin Desarrollo || true
  if git rev-parse --verify origin/Desarrollo >/dev/null 2>&1; then
    stale=0
    for path in \
      .github/scripts/vaep-jules-autorefill-core.sh \
      .github/scripts/vaep-jules-catalog-floor.sh \
      vaep/control/jules-autorefill-catalog.json; do
      if ! git diff --quiet HEAD origin/Desarrollo -- "$path"; then
        stale=1
        echo "AUTOREFILL_WAIT=STALE_CONTROL_PLANE path=$path action=FRESH_WATCHDOG_REFILL"
      fi
    done
    if (( stale != 0 )); then
      exit 0
    fi
  fi
fi

bash .github/scripts/vaep-jules-catalog-floor.sh
exec bash .github/scripts/vaep-jules-autorefill-core.sh
