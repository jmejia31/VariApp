#!/usr/bin/env python3
"""Validate N8.16 matrix governance without depending on row order."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GOV = ROOT / "docs" / "matrices-evaluacion" / "00_GOBERNANZA"
CATALOG = GOV / "CATALOGO_MATRICES.md"
TEMPLATE = GOV / "PLANTILLA_MATRIZ_UI.md"

ID_RE = re.compile(r"^VAEP-MX::[A-Z0-9_]+::[A-Z0-9_]+$")
ROW_ID_RE = re.compile(r"^(?:ROW|MATRIX|MX)[-_]?\d+$", re.IGNORECASE)

REQUIRED_TEMPLATE_TOKENS = {
    "identity": ["MATRIX_ID:", "MATRIX_CHANGE_ID:", "MATRIX_VERSION:", "PARENT_MATRIX_ID:", "CONTRACT_KIND:"],
    "ownership": ["CONTRACT_OWNER:", "DATA_OWNER:", "DEPENDS_ON_MATRIX_IDS:"],
    "data": ["DATA_ENTITIES:", "DB_TABLES:", "DB_FIELDS:", "MIGRATION_REFS:", "FK_CONSTRAINTS:", "INDEX_REFS:", "TRANSACTION_BOUNDARY:", "INTEGRITY_RULES:"],
    "backend": ["API_ROUTE:", "HTTP_METHOD:", "CONTROLLER_ACTION:", "REQUEST_DTO:", "APPLICATION_USE_CASE:", "REPOSITORY:", "BACKGROUND_JOB:", "INTEGRATION_PROVIDER:", "CONFIG_KEYS:"],
    "frontend": ["PRIMARY_ROUTE_OR_SURFACE:", "COMPONENT_REFS:", "FORM_REFS:", "DIALOG_REFS:", "WIDGET_REFS:", "STATE_MODEL:", "INTERACTIONS:", "ACCESSIBILITY_CONTRACT:"],
    "security": ["AUTHN_REQUIRED:", "AUTHZ_POLICY_OR_PERMISSION:", "RBAC_MODULE_ACTION:", "TENANT_SCOPE:", "AUDIT_EVENTS:", "PII_CLASSIFICATION:", "LOG_REDACTION:", "RATE_LIMIT_POLICY:", "OBSERVABILITY_SIGNALS:"],
    "evidence": ["CI_RUN_REFS:", "RECEIPT_REF:", "REVIEW_FIRST:"],
}


def fail(errors: list[str]) -> int:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    return 1


def parse_catalog(text: str) -> tuple[list[dict[str, str]], list[str]]:
    rows: list[dict[str, str]] = []
    errors: list[str] = []
    for line_no, line in enumerate(text.splitlines(), start=1):
        if not line.startswith("| VAEP-MX::"):
            continue
        cells = [cell.strip().strip("`") for cell in line.strip().strip("|").split("|")]
        if len(cells) != 7:
            errors.append(f"catalog line {line_no}: expected 7 columns, found {len(cells)}")
            continue
        matrix_id, domain, kind, implementation, parent, status, classification = cells
        rows.append({
            "id": matrix_id,
            "domain": domain,
            "kind": kind,
            "implementation": implementation,
            "parent": parent,
            "status": status,
            "classification": classification,
            "line": str(line_no),
        })
    return rows, errors


def main() -> int:
    errors: list[str] = []
    if not CATALOG.is_file():
        errors.append(f"missing catalog: {CATALOG.relative_to(ROOT)}")
    if not TEMPLATE.is_file():
        errors.append(f"missing template: {TEMPLATE.relative_to(ROOT)}")
    if errors:
        return fail(errors)

    catalog_text = CATALOG.read_text(encoding="utf-8")
    template_text = TEMPLATE.read_text(encoding="utf-8")
    rows, parse_errors = parse_catalog(catalog_text)
    errors.extend(parse_errors)

    declared = re.search(r"Conteo exacto:\s*\*\*(\d+) contract roots\*\*", catalog_text)
    if not declared:
        errors.append("catalog does not declare exact contract-root count")
    elif int(declared.group(1)) != len(rows):
        errors.append(f"declared count {declared.group(1)} != parsed rows {len(rows)}")

    ids = [row["id"] for row in rows]
    if len(ids) != len(set(ids)):
        duplicates = sorted({matrix_id for matrix_id in ids if ids.count(matrix_id) > 1})
        errors.append(f"duplicate MATRIX_ID values: {duplicates}")

    id_set = set(ids)
    implementation_refs: set[str] = set()
    for row in rows:
        matrix_id = row["id"]
        line = row["line"]
        if not ID_RE.fullmatch(matrix_id):
            errors.append(f"line {line}: invalid stable MATRIX_ID {matrix_id!r}")
        tail = matrix_id.rsplit("::", 1)[-1]
        if ROW_ID_RE.fullmatch(tail):
            errors.append(f"line {line}: row/index-derived identity forbidden: {matrix_id}")
        if not row["domain"] or not row["kind"] or not row["implementation"]:
            errors.append(f"line {line}: blank required catalog column")
        if row["parent"] != "ROOT" and row["parent"] not in id_set:
            errors.append(f"line {line}: unknown PARENT_MATRIX_ID {row['parent']}")
        if row["status"] == "MATERIAL_WITHOUT_ID":
            errors.append(f"line {line}: forbidden status MATERIAL_WITHOUT_ID")
        if row["implementation"] in implementation_refs:
            errors.append(f"line {line}: duplicate implementation root {row['implementation']}")
        implementation_refs.add(row["implementation"])

    for section, tokens in REQUIRED_TEMPLATE_TOKENS.items():
        for token in tokens:
            if token not in template_text:
                errors.append(f"template missing {section} token: {token}")

    if "jamás se deriva de fila, índice, orden visual" not in template_text:
        errors.append("template does not explicitly reject row/index identity")
    if "MATERIAL_WITHOUT_ID" not in catalog_text:
        errors.append("catalog does not encode no-material-without-id invariant")

    if errors:
        return fail(errors)

    material = sum(1 for row in rows if row["status"] == "MATERIAL")
    containers = sum(1 for row in rows if row["status"] == "DISCOVERY_CONTAINER")
    print(f"matrix governance PASS: ids={len(rows)} unique={len(id_set)} material={material} discovery_containers={containers}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
