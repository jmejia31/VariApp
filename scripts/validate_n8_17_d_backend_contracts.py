#!/usr/bin/env python3
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs/matrices-evaluacion/00_GOBERNANZA"
A = GOV / "N8_17_A_BATCH_MANIFEST.json"
C = GOV / "N8_17_C_DATA_CONTRACTS.json"
D = GOV / "N8_17_D_BACKEND_CONTRACTS.json"


def load(path: Path):
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def flatten_batches(doc):
    values = []
    for batch in doc.get("batches", []):
        values.extend(batch.get("matrix_ids", []))
    return values


def fail(message):
    raise SystemExit(f"N8.17.D FAIL: {message}")


a = load(A)
c = load(C)
d = load(D)

expected = flatten_batches(a)
actual = flatten_batches(d)
data_ids = [item.get("matrix_id") for item in c.get("contracts", [])]

if len(expected) != 49 or a.get("expected_total") != 49:
    fail("source manifest is not the frozen 49-root set")
if len(actual) != 49 or d.get("expected_total") != 49:
    fail("backend contract does not enumerate exactly 49 roots")
if len(set(actual)) != len(actual):
    fail("duplicate MATRIX_ID in backend contract")
if set(actual) != set(expected):
    fail("backend contract MATRIX_ID set differs from N8.17.A")
if set(data_ids) != set(expected) or len(data_ids) != 49:
    fail("N8.17.C data-contract parity is not exact")
if d.get("unresolved"):
    fail("unresolved backend contracts remain")
if d.get("runtime_delta") != "NONE_RECONCILIATION_ONLY":
    fail("unexpected runtime-delta claim")

required_contract_keys = {
    "endpoint_contract",
    "dto_contract",
    "service_contract",
    "error_contract",
    "idempotency_contract",
    "backend_authority",
    "permissions_contract",
    "trace_contract",
    "audit_contract",
}
shared = d.get("shared_backend_contract", {})
missing = sorted(required_contract_keys - set(shared))
if missing:
    fail(f"shared backend contract missing keys: {missing}")

bad_tokens = ("TODO", "TBD", "UNKNOWN", "PROVISIONAL", "UNRESOLVED")
for key, value in shared.items():
    text = str(value).upper()
    if not text.strip() or any(token in text for token in bad_tokens):
        fail(f"non-final shared contract value: {key}")

roots = d.get("evidence_roots", {})
for name, relative in roots.items():
    path = ROOT / relative
    if not path.exists():
        fail(f"evidence root missing: {name} -> {relative}")

exceptions = d.get("endpoint_exceptions", [])
exception_ids = [item.get("matrix_id") for item in exceptions]
if len(exception_ids) != len(set(exception_ids)):
    fail("duplicate endpoint exception")
if not set(exception_ids).issubset(set(expected)):
    fail("endpoint exception references non-frozen MATRIX_ID")
for item in exceptions:
    if item.get("resolution") != "NO_BUSINESS_ENDPOINT_REQUIRED" or not item.get("reason"):
        fail(f"invalid endpoint exception: {item.get('matrix_id')}")

assertions = d.get("review_assertions", {})
required_assertions = {
    "all_49_have_backend_contract",
    "all_49_have_error_contract",
    "all_49_have_idempotency_disposition",
    "all_49_have_backend_authority_rule",
    "all_49_have_permission_disposition",
    "all_49_trace_to_n8_17_c",
    "no_new_runtime_claimed",
}
if any(assertions.get(key) is not True for key in required_assertions):
    fail("one or more review assertions are not true")

# The shared contract is intentionally uniform, but coverage is still explicit:
# every frozen MATRIX_ID is enumerated by batch and inherits every required
# backend dimension unless it has a narrowly documented endpoint exception.
covered = {
    matrix_id: {
        "endpoint": next((e["resolution"] for e in exceptions if e["matrix_id"] == matrix_id), shared["endpoint_contract"]),
        "dto": shared["dto_contract"],
        "service": shared["service_contract"],
        "errors": shared["error_contract"],
        "idempotency": shared["idempotency_contract"],
        "authority": shared["backend_authority"],
        "permissions": shared["permissions_contract"],
        "trace": shared["trace_contract"],
    }
    for matrix_id in actual
}
if len(covered) != 49 or any(len(v) != 8 for v in covered.values()):
    fail("computed 49x8 backend coverage is incomplete")

print("N8.17.D PASS: 49/49 MATRIX_ID; endpoint/DTO/service/errors/idempotency/backend-authority/permissions/trace reconciled; unresolved=0")
