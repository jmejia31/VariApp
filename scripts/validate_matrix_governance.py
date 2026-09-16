#!/usr/bin/env python3
"""Validate N8.16 matrix governance without depending on row order."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MATRIX_ROOT = ROOT / "docs" / "matrices-evaluacion"
GOV = MATRIX_ROOT / "00_GOBERNANZA"
CATALOG = GOV / "CATALOGO_MATRICES.md"
TEMPLATE = GOV / "PLANTILLA_MATRIZ_UI.md"

ID_RE = re.compile(r"^VAEP-MX::[A-Z0-9_]+::[A-Z0-9_]+$")
ROW_ID_RE = re.compile(r"^(?:ROW|MATRIX|MX)[-_]?\d+$", re.IGNORECASE)
PLACEHOLDER_RE = re.compile(r"(?i)(?<![A-Z0-9_])(?:TBD|TODO|POR\s+DEFINIR|PENDIENTE\s+DE\s+DEFINIR)(?![A-Z0-9_])")

REQUIRED_TEMPLATE_TOKENS = {
    "identity": ["MATRIX_ID:", "MATRIX_CHANGE_ID:", "MATRIX_VERSION:", "PARENT_MATRIX_ID:", "CONTRACT_KIND:"],
    "ownership": ["CONTRACT_OWNER:", "DATA_OWNER:", "DEPENDS_ON_MATRIX_IDS:"],
    "data": ["DATA_ENTITIES:", "DB_TABLES:", "DB_FIELDS:", "MIGRATION_REFS:", "FK_CONSTRAINTS:", "INDEX_REFS:", "TRANSACTION_BOUNDARY:", "INTEGRITY_RULES:"],
    "backend": ["API_ROUTE:", "HTTP_METHOD:", "CONTROLLER_ACTION:", "REQUEST_DTO:", "APPLICATION_USE_CASE:", "REPOSITORY:", "BACKGROUND_JOB:", "INTEGRATION_PROVIDER:", "CONFIG_KEYS:"],
    "frontend": ["PRIMARY_ROUTE_OR_SURFACE:", "COMPONENT_REFS:", "FORM_REFS:", "DIALOG_REFS:", "WIDGET_REFS:", "STATE_MODEL:", "INTERACTIONS:", "ACCESSIBILITY_CONTRACT:"],
    "security": ["AUTHN_REQUIRED:", "AUTHZ_POLICY_OR_PERMISSION:", "RBAC_MODULE_ACTION:", "TENANT_SCOPE:", "AUDIT_EVENTS:", "PII_CLASSIFICATION:", "LOG_REDACTION:", "RATE_LIMIT_POLICY:", "OBSERVABILITY_SIGNALS:"],
    "evidence": ["CI_RUN_REFS:", "RECEIPT_REF:", "REVIEW_FIRST:"],
}

REQUIRED_MATERIAL_TOKENS = tuple(
    token
    for tokens in REQUIRED_TEMPLATE_TOKENS.values()
    for token in tokens
)


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


def extract_material_field(text: str, token: str) -> str | None:
    match = re.search(
        rf"^[ \t]*-[ \t]*{re.escape(token)}[ \t]*(.*?)[ \t]*$",
        text,
        flags=re.MULTILINE,
    )
    if match is None:
        return None
    return match.group(1).strip().strip("`").strip()


def validate_material_matrix_text(text: str, label: str) -> list[str]:
    """Validate one material matrix document against the required governance fields."""
    errors: list[str] = []
    for token in REQUIRED_MATERIAL_TOKENS:
        value = extract_material_field(text, token)
        if value is None:
            errors.append(f"{label}: missing required field {token}")
            continue
        if not value:
            errors.append(f"{label}: blank required field {token}; use N/A:<reason> when not applicable")
            continue
        if PLACEHOLDER_RE.search(value):
            errors.append(f"{label}: unjustified placeholder in {token}: {value!r}")

    matrix_id = extract_material_field(text, "MATRIX_ID:")
    if matrix_id and not ID_RE.fullmatch(matrix_id):
        errors.append(f"{label}: invalid stable MATRIX_ID {matrix_id!r}")
    return errors


def self_test_negative_contracts() -> list[str]:
    """Exercise the negative matrix rules on in-memory fixtures on every gate run."""
    errors: list[str] = []
    valid_lines = []
    for token in REQUIRED_MATERIAL_TOKENS:
        value = "VAEP-MX::SELF_TEST::VALID" if token == "MATRIX_ID:" else "N/A:self-test"
        valid_lines.append(f"- {token} {value}")
    valid = "\n".join(valid_lines) + "\n"

    valid_errors = validate_material_matrix_text(valid, "self-test valid fixture")
    if valid_errors:
        errors.append(f"self-test valid fixture rejected: {valid_errors}")

    missing = valid.replace("- CI_RUN_REFS: N/A:self-test\n", "")
    missing_errors = validate_material_matrix_text(missing, "self-test missing-field fixture")
    if not any("missing required field CI_RUN_REFS:" in error for error in missing_errors):
        errors.append("self-test missing-field fixture did not trigger the required-field rejection")

    blank = valid.replace("- REQUEST_DTO: N/A:self-test", "- REQUEST_DTO:")
    blank_errors = validate_material_matrix_text(blank, "self-test blank-field fixture")
    if not any("blank required field REQUEST_DTO:" in error for error in blank_errors):
        errors.append("self-test blank-field fixture did not trigger the blank-field rejection")

    placeholder = valid.replace("- API_ROUTE: N/A:self-test", "- API_ROUTE: TBD")
    placeholder_errors = validate_material_matrix_text(placeholder, "self-test placeholder fixture")
    if not any("unjustified placeholder in API_ROUTE:" in error for error in placeholder_errors):
        errors.append("self-test placeholder fixture did not trigger the placeholder rejection")

    invalid_id = valid.replace("VAEP-MX::SELF_TEST::VALID", "ROW-17")
    invalid_id_errors = validate_material_matrix_text(invalid_id, "self-test invalid-id fixture")
    if not any("invalid stable MATRIX_ID" in error for error in invalid_id_errors):
        errors.append("self-test invalid-id fixture did not trigger the stable-ID rejection")

    return errors


def validate_material_matrices() -> tuple[int, list[str]]:
    """Reject incomplete governed matrices and unjustified placeholder field values."""
    governed = 0
    errors: list[str] = []
    for path in sorted(MATRIX_ROOT.rglob("*.md")):
        if GOV in path.parents:
            continue
        text = path.read_text(encoding="utf-8")
        if "MATRIX_ID:" not in text:
            continue
        governed += 1
        relative = path.relative_to(ROOT)
        errors.extend(validate_material_matrix_text(text, str(relative)))
    return governed, errors


def main() -> int:
    errors: list[str] = []
    if not CATALOG.is_file():
        errors.append(f"missing catalog: {CATALOG.relative_to(ROOT)}")
    if not TEMPLATE.is_file():
        errors.append(f"missing template: {TEMPLATE.relative_to(ROOT)}")
    if errors:
        return fail(errors)

    errors.extend(self_test_negative_contracts())

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
        for column in ("domain", "kind", "implementation", "parent", "status", "classification"):
            if PLACEHOLDER_RE.search(row[column]):
                errors.append(f"line {line}: unjustified placeholder in catalog {column}: {row[column]!r}")
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

    governed_matrices, material_errors = validate_material_matrices()
    errors.extend(material_errors)

    if errors:
        return fail(errors)

    material = sum(1 for row in rows if row["status"] == "MATERIAL")
    containers = sum(1 for row in rows if row["status"] == "DISCOVERY_CONTAINER")
    print(
        "matrix governance PASS: "
        f"ids={len(rows)} unique={len(id_set)} material={material} "
        f"discovery_containers={containers} governed_matrices={governed_matrices}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
