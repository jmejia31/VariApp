#!/usr/bin/env bash
set -euo pipefail
bash .github/scripts/vaep-jules-catalog-floor.sh
exec bash .github/scripts/vaep-jules-autorefill-core.sh
