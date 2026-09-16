#!/usr/bin/env python3
"""Validate N8.17.C field/persistence contract coverage for every frozen matrix root."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs" / "matrices-evaluacion" / "00_GOBERNANZA"
CATALOG = GOV / "CATALOGO_MATRICES.md"
CONTRACTS = GOV / "N8_17_C_DATA_CONTRACTS.json"
ENTITY_ROOT = ROOT / "backend" / "src" / "Domain" / "Entities"
APP_DB_CONTEXT = ROOT / "backend" / "src" / "Infrastructure" / "Persistence" / "AppDbContext.cs"
CONFIG_ROOT = ROOT / "backend" / "src" / "Infrastructure" / "Persistence" / "Configurations"
MIGRATION_ROOT = ROOT / "backend" / "src" / "Infrastructure" / "Persistence" / "Migrations"

PLACEHOLDER_RE = re.compile(r"(?i)(?<![A-Z0-9_])(?:TBD|TODO|POR\s+DEFINIR|PENDIENTE\s+DE\s+DEFINIR)(?![A-Z0-9_])")
CLASS_RE_TEMPLATE = r"\b(?:public\s+)?(?:sealed\s+|abstract\s+|partial\s+)*class\s+{name}\b"
PROPERTY_RE = re.compile(
    r"^\s*public\s+(?!class\b|interface\b|enum\b)(?:virtual\s+)?[^()=;]+?\s+[A-Za-z_][A-Za-z0-9_]*\s*\{[^\n{}]*(?:get|set|init)[^\n{}]*\}",
    re.MULTILINE,
)
ALLOWED_KINDS = {
    "EF_ENTITY_AGGREGATE",
    "EF_CONFIG_PLUS_DERIVED_READ_MODEL",
    "DERIVED_FINANCIAL_STATE",
    "DERIVED_READ_MODEL",
    "ORCHESTRATION_NO_OWN_TABLE",
}
DIRECT_KINDS = {"EF_ENTITY_AGGREGATE", "EF_CONFIG_PLUS_DERIVED_READ_MODEL"}
DERIVED_KINDS = {"DERIVED_FINANCIAL_STATE", "DERIVED_READ_MODEL", "ORCHESTRATION_NO_OWN_TABLE"}
REQUIRED_CANONICAL_KEYS = {
    "db_context",
    "entity_root",
    "configuration_root",
    "migration_root",
    "field_coverage",
    "constraint_coverage",
    "index_coverage",
    "default_autofill_coverage",
    "server_calculated_coverage",
    "transaction_coverage",
    "traceability_rule",
}


def parse_catalog() -> tuple[list[dict[str, str]], list[str]]:
    errors: list[str] = []
    rows: list[dict[str, str]] = []
    if not CATALOG.is_file():
        return rows, [f"missing catalog: {CATALOG.relative_to(ROOT)}"]
    for line_no, line in enumerate(CATALOG.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.startswith("| VAEP-MX::"):
            continue
        cells = [cell.strip().strip("`") for cell in line.strip().strip("|").split("|")]
        if len(cells) != 7:
            errors.append(f"catalog line {line_no}: expected 7 columns, found {len(cells)}")
            continue
        rows.append({"id": cells[0], "domain": cells[1]})
    return rows, errors


def source_index() -> dict[str, list[Path]]:
    index: dict[str, list[Path]] = {}
    for path in ENTITY_ROOT.rglob("*.cs"):
        index.setdefault(path.stem, []).append(path)
    return index


def resolve_entity(name: str, index: dict[str, list[Path]], errors: list[str], matrix_id: str) -> tuple[Path | None, int]:
    matches = index.get(name, [])
    if len(matches) != 1:
        errors.append(f"N8.17.C {matrix_id}: entity type {name!r} resolves to {len(matches)} source files, expected exactly 1")
        return None, 0
    path = matches[0]
    text = path.read_text(encoding="utf-8")
    if not re.search(CLASS_RE_TEMPLATE.format(name=re.escape(name)), text):
        errors.append(f"N8.17.C {matrix_id}: {path.relative_to(ROOT)} does not declare class {name}")
        return path, 0
    properties = PROPERTY_RE.findall(text)
    if not properties:
        errors.append(f"N8.17.C {matrix_id}: {name} has no resolvable public property contract")
    return path, len(properties)


def main() -> int:
    errors: list[str] = []
    catalog, catalog_errors = parse_catalog()
    errors.extend(catalog_errors)
    if not CONTRACTS.is_file():
        errors.append(f"missing N8.17.C data contracts: {CONTRACTS.relative_to(ROOT)}")
        return fail(errors)
    for required in (ENTITY_ROOT, APP_DB_CONTEXT, CONFIG_ROOT, MIGRATION_ROOT):
        if not required.exists():
            errors.append(f"canonical persistence source missing: {required.relative_to(ROOT)}")
    try:
        doc = json.loads(CONTRACTS.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        return fail([f"invalid N8.17.C data contract JSON: {exc}"])

    if doc.get("parent") != "N8.17.C":
        errors.append(f"N8.17.C parent mismatch: {doc.get('parent')!r}")
    if doc.get("unknowns") != []:
        errors.append("N8.17.C top-level unknowns must be an explicit empty array for closure")

    canonical = doc.get("canonical_persistence")
    if not isinstance(canonical, dict):
        errors.append("N8.17.C canonical_persistence must be an object")
        canonical = {}
    for key in sorted(REQUIRED_CANONICAL_KEYS):
        value = canonical.get(key)
        if not isinstance(value, str) or not value.strip():
            errors.append(f"N8.17.C canonical_persistence.{key} must be a non-empty string")
    expected_sources = {
        "db_context": "backend/src/Infrastructure/Persistence/AppDbContext.cs",
        "entity_root": "backend/src/Domain/Entities",
        "configuration_root": "backend/src/Infrastructure/Persistence/Configurations",
        "migration_root": "backend/src/Infrastructure/Persistence/Migrations",
    }
    for key, expected in expected_sources.items():
        if canonical.get(key) != expected:
            errors.append(f"N8.17.C canonical_persistence.{key} must be {expected!r}")

    contracts = doc.get("contracts")
    if not isinstance(contracts, list):
        return fail(errors + ["N8.17.C contracts must be an array"])

    by_catalog = {row["id"]: row for row in catalog}
    catalog_ids = set(by_catalog)
    ids: list[str] = []
    index = source_index()
    resolved_files: set[Path] = set()
    resolved_properties = 0
    direct_contracts = 0
    derived_contracts = 0

    for pos, contract in enumerate(contracts, start=1):
        if not isinstance(contract, dict):
            errors.append(f"N8.17.C contract {pos}: expected object")
            continue
        matrix_id = contract.get("matrix_id")
        domain = contract.get("domain")
        kind = contract.get("persistence_kind")
        entities = contract.get("entity_types")
        authoritative = contract.get("authoritative_entity_types")
        reason = contract.get("reason")
        if not isinstance(matrix_id, str) or not matrix_id:
            errors.append(f"N8.17.C contract {pos}: missing matrix_id")
            continue
        ids.append(matrix_id)
        if matrix_id not in by_catalog:
            errors.append(f"N8.17.C unknown MATRIX_ID {matrix_id}")
        elif domain != by_catalog[matrix_id]["domain"]:
            errors.append(f"N8.17.C {matrix_id}: domain {domain!r} != catalog {by_catalog[matrix_id]['domain']!r}")
        if kind not in ALLOWED_KINDS:
            errors.append(f"N8.17.C {matrix_id}: invalid persistence_kind {kind!r}")
        if not isinstance(entities, list) or not all(isinstance(x, str) and x for x in entities):
            errors.append(f"N8.17.C {matrix_id}: entity_types must be a string array")
            entities = []
        if not isinstance(authoritative, list) or not all(isinstance(x, str) and x for x in authoritative):
            errors.append(f"N8.17.C {matrix_id}: authoritative_entity_types must be a string array")
            authoritative = []
        if not isinstance(reason, str) or not reason.strip():
            errors.append(f"N8.17.C {matrix_id}: reason must be non-empty")
        elif PLACEHOLDER_RE.search(reason):
            errors.append(f"N8.17.C {matrix_id}: reason contains a silent placeholder")

        if kind in DIRECT_KINDS:
            direct_contracts += 1
            if not entities:
                errors.append(f"N8.17.C {matrix_id}: direct persistence contract requires entity_types")
        if kind in DERIVED_KINDS:
            derived_contracts += 1
            if entities:
                errors.append(f"N8.17.C {matrix_id}: derived/orchestration contract must not claim owned entity_types")
            if not authoritative:
                errors.append(f"N8.17.C {matrix_id}: derived/orchestration contract requires authoritative_entity_types")

        if len(entities) != len(set(entities)):
            errors.append(f"N8.17.C {matrix_id}: duplicate entity_types")
        if len(authoritative) != len(set(authoritative)):
            errors.append(f"N8.17.C {matrix_id}: duplicate authoritative_entity_types")
        for entity in entities + authoritative:
            path, property_count = resolve_entity(entity, index, errors, matrix_id)
            if path is not None:
                resolved_files.add(path)
                resolved_properties += property_count

    duplicates = sorted({matrix_id for matrix_id in ids if ids.count(matrix_id) > 1})
    if duplicates:
        errors.append(f"N8.17.C duplicate MATRIX_ID values: {duplicates}")
    missing = sorted(catalog_ids - set(ids))
    extra = sorted(set(ids) - catalog_ids)
    if missing:
        errors.append(f"N8.17.C missing catalog MATRIX_ID values: {missing}")
    if extra:
        errors.append(f"N8.17.C contains extra MATRIX_ID values: {extra}")
    if doc.get("expected_total") != len(catalog):
        errors.append(f"N8.17.C expected_total {doc.get('expected_total')!r} != catalog count {len(catalog)}")
    if len(contracts) != len(catalog):
        errors.append(f"N8.17.C contract count {len(contracts)} != catalog count {len(catalog)}")
    if resolved_properties <= 0:
        errors.append("N8.17.C resolved zero source properties; field traceability is not effective")

    if errors:
        return fail(errors)
    print(
        "N8.17.C data contracts PASS: "
        f"roots={len(contracts)} direct={direct_contracts} derived_or_orchestrated={derived_contracts} "
        f"entity_source_files={len(resolved_files)} source_properties_resolved={resolved_properties} unknowns=0"
    )
    return 0


def fail(errors: list[str]) -> int:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
