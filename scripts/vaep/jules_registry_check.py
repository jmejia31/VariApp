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
assert data["schemaVersion"] == "3.0"
assert data["executionModel"] == "TASKS_FIRST_JULES_ON_DEMAND"
assert data["runtimeMode"] == "OPTIONAL_ACCELERATORS_J1_J6"
assert data["cutoverEnabled"] is True
assert data["directExecutionDefault"] is True
assert data["julesRequiredForProgress"] is False
assert data["minimumUtilizationTarget"] == 0
assert data["automaticRefillEnabled"] is False
assert data["automaticQueueFloorEnabled"] is False
assert data["offloadRequiresExplicitApproval"] is True
assert data["offloadApprovalMarker"] == "JULES_OFFLOAD_APPROVED"
assert data["offloadRequiresCriticalPathGain"] is True
assert data["offloadRequiresNonOverlappingScope"] is True
assert data["offloadMayBlockCurrentParent"] is False
assert data["legacyRuntimeEnabled"] is False
assert data["activeLegacyWorkers"] == []
assert data["legacyAliases"] == ALIASES
assert data["historicalAliasMode"] == "READ_ONLY_NORMALIZATION"
assert data["availableWorkers"] == EXPECTED
assert sorted(data["workers"]) == EXPECTED

for wid in EXPECTED:
    worker = data["workers"][wid]
    assert worker["enabled"] is True
    assert worker["operatingMode"] == "AVAILABLE_ON_DEMAND"
    assert worker["maxAttempts"] == 2
    assert worker["reworkMax"] == 1
    assert worker["secretName"] == f"JULES_{wid}_API_KEY"
    assert worker["workflow"] == f".github/workflows/vaep-jules-{wid.lower()}.yml"
    for obsolete_key in (
        "queueDepthTarget",
        "programmedBacklogTarget",
        "programmedBacklogRefillFloor",
        "desiredSecretName",
        "credentialMigrationPending",
    ):
        assert obsolete_key not in worker, f"obsolete Jules runtime field: {wid}.{obsolete_key}"

for raw_path in TRANSITIONAL_PATHS:
    assert not Path(raw_path).exists(), f"transitional Jules path still present: {raw_path}"

print("JULES_REGISTRY_TASKS_FIRST_OK")
