#!/usr/bin/env python3
"""Validate N8.18.B parent->child domain ownership and demonstrated consolidation."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs/matrices-evaluacion/00_GOBERNANZA"


def load_json(path: Path):
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def fail(message: str) -> None:
    raise SystemExit(f"N8.18.B DOMAIN FAIL: {message}")


manifest = load_json(GOV / "N8_17_A_BATCH_MANIFEST.json")
boundaries = load_json(GOV / "N8_18_B_DOMAIN_BOUNDARIES.json")
cert = load_json(GOV / "N8_17_H_CERTIFICATION.json")
catalog_text = (GOV / "CATALOGO_MATRICES.md").read_text(encoding="utf-8")
routes_text = (ROOT / "frontend/src/app/app.routes.ts").read_text(encoding="utf-8")
service_text = (ROOT / "frontend/src/app/services/catalogo-producto.service.ts").read_text(encoding="utf-8")

batches = manifest.get("batches", [])
if manifest.get("expected_batch_count") != 9 or boundaries.get("domain_count") != 9:
    fail("canonical domain count must remain 9")
manifest_map = {}
for batch in batches:
    domain = batch.get("batch")
    for mid in batch.get("matrix_ids", []):
        if mid in manifest_map:
            fail(f"duplicate MATRIX_ID in manifest: {mid}")
        manifest_map[mid] = domain
if len(manifest_map) != 49 or boundaries.get("contract_root_count") != 49:
    fail("frozen contract root count must remain 49")

# Canonical domain declarations must be exact and unique.
domain_rows = boundaries.get("domains", [])
domain_names = [row.get("domain") for row in domain_rows]
if len(domain_names) != 9 or len(set(domain_names)) != 9:
    fail("domain boundary declarations are missing/duplicated")
if set(domain_names) != {b.get("batch") for b in batches}:
    fail("domain declarations diverge from frozen manifest batches")
for row in domain_rows:
    if row.get("parent_id") != f"VAEP-DOMAIN::{row['domain']}":
        fail(f"noncanonical parent id for {row['domain']}")
    if not row.get("ownership"):
        fail(f"missing ownership statement for {row['domain']}")

# Catalog ownership must agree with the MATRIX_ID domain segment and frozen manifest.
row_re = re.compile(r"^\|\s*(VAEP-MX::[^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", re.M)
catalog = {}
for match in row_re.finditer(catalog_text):
    mid, domain, kind, impl, parent = [part.strip() for part in match.groups()]
    if mid in catalog:
        fail(f"duplicate catalog MATRIX_ID {mid}")
    catalog[mid] = {"domain": domain, "kind": kind, "impl": impl, "parent": parent}
if set(catalog) != set(manifest_map):
    fail(f"catalog/manifest mismatch missing={sorted(set(manifest_map)-set(catalog))} extra={sorted(set(catalog)-set(manifest_map))}")
for mid, expected_domain in manifest_map.items():
    id_parts = mid.split("::")
    if len(id_parts) != 3 or id_parts[1] != expected_domain:
        fail(f"MATRIX_ID domain segment mismatch: {mid}")
    if catalog[mid]["domain"] != expected_domain:
        fail(f"catalog ownership mismatch: {mid}")

# Historical navigation composition remains acyclic: APP_SHELL is root, all others compose beneath it.
shell = "VAEP-MX::GOV_CONFIG_INTEGRATIONS::APP_SHELL"
if catalog[shell]["parent"] != "ROOT":
    fail("APP_SHELL must remain the navigation composition root")
for mid, row in catalog.items():
    if mid == shell:
        continue
    if row["parent"] != shell:
        fail(f"unexpected parent alias/cycle risk for {mid}: {row['parent']}")

# N8.17 contract publication remains intact.
if cert.get("expected_total") != 49 or cert.get("spec_complete_count") != 49:
    fail("N8.17 certification no longer preserves 49/49 SPEC_COMPLETE")
if cert.get("missing_matrix_ids") or cert.get("unjustified_tbd") or cert.get("unresolved"):
    fail("N8.17 certification has reopened gaps")

# ARCH-01: demonstrated generic product lookups must remain one implementation, not four aliases.
consolidations = {item.get("id"): item for item in boundaries.get("consolidations", [])}
arch01 = consolidations.get("ARCH-01")
if not arch01 or arch01.get("status") != "CONSOLIDATED_AND_GUARDED":
    fail("ARCH-01 consolidation is not guarded")
shared_root = ROOT / arch01["shared_frontend_root"]
shared_service = ROOT / arch01["shared_service"]
if not shared_root.is_dir() or not shared_service.is_file():
    fail("ARCH-01 shared implementation is missing")
for forbidden in arch01.get("forbidden_parallel_feature_roots", []):
    if (ROOT / forbidden).exists():
        fail(f"parallel duplicate feature root appeared: {forbidden}")

route_to_type = dict(zip(arch01["routes"], arch01["typed_interfaces"]))
for route, tipo in route_to_type.items():
    route_pattern = re.compile(
        r"path:\s*['\"]" + re.escape(route) + r"['\"].*?tipo:\s*['\"]" + re.escape(tipo) + r"['\"].*?features/catalogos-producto/catalogo-producto-list\.component",
        re.S,
    )
    if not route_pattern.search(routes_text):
        # app.routes has loadComponent before/after data depending on formatting; use a bounded route record fallback.
        record_match = re.search(r"\{\s*path:\s*['\"]" + re.escape(route) + r"['\"].*?\},", routes_text, re.S)
        if not record_match or "features/catalogos-producto/catalogo-producto-list.component" not in record_match.group(0) or f"tipo: '{tipo}'" not in record_match.group(0):
            fail(f"route {route} no longer reuses shared CatalogoProducto component as {tipo}")
    if not re.search(r"\b" + re.escape(tipo) + r"\s*:\s*['\"]" + re.escape(route) + r"['\"]", service_text):
        fail(f"CatalogoProductoService mapping missing {tipo}->{route}")

for sibling in arch01.get("siblings_not_aliases", []):
    if sibling not in catalog:
        fail(f"ARCH-01 sibling root missing: {sibling}")
    impl_refs = re.findall(r"`([^`]+)`", catalog[sibling]["impl"])
    if not impl_refs or not any((ROOT / ref).exists() for ref in impl_refs):
        fail(f"ARCH-01 sibling lacks physical implementation: {sibling}")

# ARCH-03 remains fail-closed: ownership policy may be explicit now, but physical relocation belongs to D.
arch03 = consolidations.get("ARCH-03")
if not arch03 or arch03.get("status") != "OWNERSHIP_EXPLICIT_MOVE_DEFERRED_TO_BACKEND_STAGE":
    fail("ARCH-03 must not claim a speculative physical consolidation in DOMAIN")
for rel in arch03.get("paths", []):
    if not (ROOT / rel).exists():
        fail(f"ARCH-03 observed layout path disappeared without backend-stage evidence: {rel}")

policy = boundaries.get("deletion_policy", {})
if policy.get("remove_safe_count") != 0 or policy.get("deprecate_count") != 0:
    fail("N8.18.B cannot invent removable/deprecated assets")
if policy.get("unknown_blocks_deletion") is not True or policy.get("speculative_delete_allowed") is not False:
    fail("fail-closed deletion policy weakened")
if policy.get("destructive_db_change_allowed") is not False:
    fail("destructive DB changes are forbidden before N8.21")

acceptance = boundaries.get("acceptance", {})
required_true = [
    "all_49_contract_roots_owned_by_exactly_one_domain",
    "domain_name_matches_matrix_id_segment",
    "certified_contracts_preserved",
]
for key in required_true:
    if acceptance.get(key) is not True:
        fail(f"acceptance assertion not true: {key}")
if acceptance.get("parent_child_cycles") != 0 or acceptance.get("ambiguous_business_rule_aliases") != 0:
    fail("cycles or ambiguous aliases remain")
if acceptance.get("product_lookup_parallel_feature_roots") != 0:
    fail("parallel product lookup feature roots remain")

print("N8.18.B DOMAIN PASS")
print("domains=9 contract_roots=49 ownership=49/49")
print("parent_child_cycles=0 ambiguous_aliases=0")
print("ARCH-01 product_lookup_consolidation=4/4 routes + shared service; parallel_roots=0")
print("ARCH-03 ownership_explicit physical_move_deferred_to_N8.18.D")
print("speculative_delete=0 destructive_db_change=0")
