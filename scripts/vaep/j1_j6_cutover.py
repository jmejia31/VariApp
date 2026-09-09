#!/usr/bin/env python3
import json
from pathlib import Path
d=json.loads(Path("vaep/control/jules-workers.json").read_text())
assert d["cutoverEnabled"] is True
assert d["activeWorkers"] == ["J1","J2","J3","J4","J5","J6"]
assert d["activeLegacyWorkers"] == []
print("J1_J6_CUTOVER_ACTIVE_OK")
