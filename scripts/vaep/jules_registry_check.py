#!/usr/bin/env python3
import json
from pathlib import Path

REGISTRY = Path("vaep/control/jules-workers.json")
EXPECTED = [f"J{i}" for i in range(1, 7)]
ALIASES = {
    "JULES_A": "J1",
    "JULES_B": "J2",
    "JULES_C": "J3",
    "JULES_D": "J4",
}
TRANSITIONAL_PATHS = [
    "docs/VAEP_J1_J6_PREPARATION.md",
    "scripts/vaep/j1_j6_cutover.py",
    "scripts/vaep/j1_j6_registry_check.py",
    ".github/workflows/vaep-j1-j6-cutover-apply.yml",
    ".github/workflows/vaep-j1-j6-cutover-dry-run.yml",
    ".github/workflows/vaep-j1-j6-readiness-gate.yml",
    ".github/workflows/vaep-j3-j4-legacy-readiness.yml",
    ".github/workflows/vaep-j5-j6-canary.yml",
    ".github/workflows/vaep-jules-secondary.yml",
    ".github/workflows/vaep-jules-secondary-b.yml",
    ".github/workflows/vaep-jules-secondary-c.yml",
    ".github/workflows/vaep-jules-secondary-d.yml",
    ".github/workflows/vaep-jules-a-recovery.yml",
    ".github/workflows/vaep-jules-b-recovery.yml",
    ".github/workflows/vaep-jules-c-recovery.yml",
    ".github/workflows/vaep-jules-d-recovery.yml",
]

data = json.loads(REGISTRY.read_text(encoding="utf-8"))
assert data["schemaVersion"] == "2.0"
assert data["phase"] == "F3_FINAL_CERTIFIED"
assert data["runtimeMode"] == "CANONICAL_J1_J6"
assert data["cutoverEnabled"] is True
assert data["legacyRuntimeEnabled"] is False
assert data["activeWorkers"] == EXPECTED
assert data["activeLegacyWorkers"] == []
assert data["legacyAliases"] == ALIASES
assert data["historicalAliasMode"] == "READ_ONLY_NORMALIZATION"
assert sorted(data["workers"]) == EXPECTED

for wid in EXPECTED:
    worker = data["workers"][wid]
    assert worker["enabled"] is True
    assert worker["queueDepthTarget"] == 2
    assert worker["programmedBacklogTarget"] == 12
    assert worker["programmedBacklogRefillFloor"] == 4
    assert worker["maxAttempts"] == 2
    assert worker["reworkMax"] == 1
    assert worker["secretName"] == f"JULES_{wid}_API_KEY"
    assert worker["workflow"] == f".github/workflows/vaep-jules-{wid.lower()}.yml"
    assert "desiredSecretName" not in worker
    assert "credentialMigrationPending" not in worker

for raw_path in TRANSITIONAL_PATHS:
    assert not Path(raw_path).exists(), f"F3 transitional path still present: {raw_path}"

print("JULES_REGISTRY_F3_FINAL_OK")
