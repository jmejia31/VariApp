#!/usr/bin/env python3
import json
from pathlib import Path

p = Path("vaep/control/jules-workers.json")
d = json.loads(p.read_text(encoding="utf-8"))
expected = [f"J{i}" for i in range(1, 7)]
assert d["phase"] == "F2_ACTIVE_CUTOVER"
assert d["cutoverEnabled"] is True
assert d["activeWorkers"] == expected
assert d["activeLegacyWorkers"] == []
assert sorted(d["workers"]) == expected
for wid in expected:
    w = d["workers"][wid]
    assert w["enabled"] is True
    assert w["queueDepthTarget"] == 2
    assert w["programmedBacklogTarget"] == 12
    assert w["programmedBacklogRefillFloor"] == 4
    assert w["maxAttempts"] == 2
    assert w["reworkMax"] == 1
    assert w["secretName"] == f"JULES_{wid}_API_KEY"
    assert w["desiredSecretName"] == f"JULES_{wid}_API_KEY"
    assert w["credentialMigrationPending"] is False
print("J1_J6_REGISTRY_ACTIVE_OK")
