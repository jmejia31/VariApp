#!/usr/bin/env python3
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs/matrices-evaluacion/00_GOBERNANZA"
A = GOV / "N8_17_A_BATCH_MANIFEST.json"
E = GOV / "N8_17_E_FRONTEND_CONTRACTS.json"
CATALOG = GOV / "CATALOGO_MATRICES.md"


def load(path):
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def fail(msg):
    raise SystemExit(f"N8.17.E FAIL: {msg}")


a = load(A)
e = load(E)
expected = [mid for batch in a.get("batches", []) for mid in batch.get("matrix_ids", [])]
if len(expected) != 49 or len(set(expected)) != 49 or e.get("expected_total") != 49:
    fail("frozen root set is not exact 49")
if e.get("unresolved"):
    fail("unresolved UI contracts remain")
if e.get("runtime_delta") != "NONE_RECONCILIATION_ONLY":
    fail("unexpected runtime delta claim")

shared = e.get("shared_ui_contract", {})
required = {
    "route_contract", "component_contract", "action_contract", "loading_state", "empty_state",
    "error_state", "forbidden_state", "readonly_state", "responsive_contract",
    "accessibility_contract", "modal_contract", "feedback_contract", "primitive_contract"
}
missing = sorted(required - set(shared))
if missing:
    fail(f"shared UI contract missing {missing}")
for k, v in shared.items():
    text = str(v).upper()
    if not str(v).strip() or any(x in text for x in ("TODO", "TBD", "UNKNOWN", "UNRESOLVED")):
        fail(f"non-final UI contract value: {k}")

for rel in e.get("shared_primitives_evidence", []):
    if not (ROOT / rel).exists():
        fail(f"shared primitive evidence missing: {rel}")

text = CATALOG.read_text(encoding="utf-8")
pattern = re.compile(r"^\|\s*(VAEP-MX::[^|]+?)\s*\|[^\n]*?\|\s*`([^`]+)`\s*\|", re.MULTILINE)
refs = {m.group(1).strip(): m.group(2).strip() for m in pattern.finditer(text)}
if set(refs) != set(expected):
    missing_ids = sorted(set(expected) - set(refs))
    extra_ids = sorted(set(refs) - set(expected))
    fail(f"catalog implementation-ref parity mismatch missing={missing_ids} extra={extra_ids}")

for mid, rel in refs.items():
    if "*" in rel:
        matches = list(ROOT.glob(rel))
        if not matches:
            fail(f"implementation glob resolves empty: {mid} -> {rel}")
    else:
        path = ROOT / rel
        if not path.exists():
            fail(f"implementation root missing: {mid} -> {rel}")
        if path.is_dir() and not any(p.suffix in {".ts", ".html", ".scss", ".css"} for p in path.rglob("*")):
            fail(f"feature root has no UI source files: {mid} -> {rel}")

if not (ROOT / "frontend/src/app/app.routes.ts").exists():
    fail("canonical Angular route table missing")

overrides = e.get("matrix_overrides", [])
ids = [x.get("matrix_id") for x in overrides]
if len(ids) != len(set(ids)) or not set(ids).issubset(set(expected)):
    fail("invalid or duplicate matrix override")
for item in overrides:
    if not item.get("resolution") or not item.get("reason"):
        fail(f"incomplete matrix override: {item.get('matrix_id')}")

assertions = e.get("review_assertions", {})
if not assertions or any(v is not True for v in assertions.values()):
    fail("one or more review assertions are not true")

print("N8.17.E PASS: 49/49 catalog implementation roots; routes/components/actions/states/responsive/a11y/modals/primitives reconciled; unresolved=0")
