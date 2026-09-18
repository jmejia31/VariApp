#!/usr/bin/env python3
"""Validate N8.18.C non-destructive DB migration integrity decision."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs/matrices-evaluacion/00_GOBERNANZA"


def load_json(path: Path):
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def fail(message: str) -> None:
    raise SystemExit(f"N8.18.C DB_MIG FAIL: {message}")


evidence = load_json(GOV / "N8_18_C_DB_MIG_INTEGRITY.json")
data_contracts = load_json(GOV / "N8_17_C_DATA_CONTRACTS.json")
boundaries = load_json(GOV / "N8_18_B_DOMAIN_BOUNDARIES.json")
app_db_context = (ROOT / "backend/src/Infrastructure/Persistence/AppDbContext.cs").read_text(encoding="utf-8")

if evidence.get("parent") != "N8.18.C" or evidence.get("stage") != "DB_MIG":
    fail("evidence identity mismatch")
if evidence.get("decision") != "NO_SCHEMA_DELTA_REQUIRED__PRESERVE_HISTORY":
    fail("unexpected decision")

review = evidence.get("review_first", {})
if review.get("result") != "PASS" or any(review.get(k) != 0 for k in ("p0_count", "p1_count", "p2_count")):
    fail("REVIEW_FIRST must be PASS with P0=P1=P2=0")

unknowns = data_contracts.get("unknowns")
if unknowns != []:
    fail(f"N8.17.C data contract unknowns reopened: {unknowns}")
if data_contracts.get("expected_total") != 49 or len(data_contracts.get("contracts", [])) != 49:
    fail("frozen data contract count must remain 49")

roots = evidence.get("migration_roots", [])
expected_roots = {
    "backend/src/Infrastructure/Migrations",
    "backend/src/Infrastructure/Persistence/Migrations",
}
if {item.get("path") for item in roots} != expected_roots:
    fail("migration roots must remain the two certified historical roots")
for item in roots:
    if item.get("classification") != "KEEP" or item.get("action") != "PRESERVE_UNCHANGED":
        fail(f"migration root not fail-closed: {item}")
    root = ROOT / item["path"]
    if not root.is_dir() or not any(root.iterdir()):
        fail(f"migration root missing or empty: {item['path']}")

if "ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)" not in app_db_context:
    fail("AppDbContext configuration discovery changed")

acceptance = evidence.get("acceptance", {})
required = {
    "non_destructive_model_change_required": False,
    "non_destructive_model_changes_applied": 0,
    "destructive_ddl_applied": 0,
    "migration_history_preserved": True,
    "migration_roots_preserved": 2,
    "n8_17_data_contract_unknowns": 0,
    "app_db_context_configuration_discovery_preserved": True,
    "production_touched": False,
    "main_touched": False,
    "deploy_touched": False,
    "secrets_exposed": 0,
    "pr2_touched": False,
}
for key, expected in required.items():
    if acceptance.get(key) != expected:
        fail(f"acceptance mismatch {key}: expected {expected!r}, got {acceptance.get(key)!r}")

if boundaries.get("deletion_policy", {}).get("destructive_db_change_allowed") is not False:
    fail("N8.18.B deletion policy no longer forbids destructive DB changes")

pending = evidence.get("deferred_to_n8_21", [])
if len(pending) != 1:
    fail("ARCH-02 deferred cleanup must remain explicit")
arch02 = pending[0]
if arch02.get("id") != "ARCH-02" or arch02.get("classification") != "REMOVE_SAFE_DB_AFTER_BACKUP":
    fail("ARCH-02 deferred classification mismatch")
if "N8.21" not in arch02.get("gate", "") or arch02.get("current_action") != "DO_NOT_MOVE_DELETE_REGENERATE_OR_SQUASH_MIGRATION_HISTORY":
    fail("ARCH-02 backup/restore gate weakened")

print("N8.18.C DB_MIG PASS")
print("data_contracts=49 unknowns=0")
print("migration_roots=2 history_preserved=true")
print("schema_delta_required=false destructive_ddl=0")
print("ARCH-02=REMOVE_SAFE_DB_AFTER_BACKUP -> N8.21")
