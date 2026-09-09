#!/usr/bin/env python3
import json
import pathlib

P = pathlib.Path("vaep/control/jules-workers.json")
data = json.loads(P.read_text())
expected = [f"J{i}" for i in range(1, 7)]

assert data["preparedWorkers"] == expected
assert data["cutoverEnabled"] is False
assert data["activeLegacyWorkers"] == ["JULES_A", "JULES_B", "JULES_C", "JULES_D"]

for worker in expected:
    config = data["workers"][worker]
    assert config["enabled"] is False
    assert config["secretName"] == f"JULES_{worker}_API_KEY"
    assert config["queueDepthTarget"] == 2
    assert config["programmedBacklogTarget"] == 12
    assert config["programmedBacklogRefillFloor"] == 4
    assert config["maxAttempts"] == 2
    assert config["reworkMax"] == 1

print("J1_J6_REGISTRY_PREPARED_OK")
