#!/usr/bin/env python3
"""Fail-closed multi-tenant certification gate for Priority 3 / N6.2.

The script does not pretend that the existence of Empresa proves tenant
isolation. It records structural prerequisites across identity, resource
authorization, persistence, reports, files, caches and background processing.
Use --require-certified only when N6.2 is ready to claim complete isolation.
"""
from __future__ import annotations

import argparse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def text(relative: str) -> str:
    path = ROOT / relative
    return path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""


def any_source_contains(folder: str, tokens: tuple[str, ...]) -> bool:
    base = ROOT / folder
    if not base.exists():
        return False
    for path in base.rglob("*.cs"):
        content = path.read_text(encoding="utf-8", errors="replace")
        if all(token in content for token in tokens):
            return True
    return False


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--require-certified", action="store_true")
    args = parser.parse_args()

    usuario = text("backend/src/Domain/Entities/Usuario.cs")
    scope = text("backend/src/Application/Interfaces/IUsuarioScopeService.cs")
    scope_impl = text("backend/src/Infrastructure/Services/UsuarioScopeService.cs")
    sucursal = text("backend/src/Application/Services/SucursalService.cs")
    db = text("backend/src/Infrastructure/Persistence/AppDbContext.cs")
    factura_share = text("backend/src/Application/Services/FacturaCompartirService.cs")

    checks = {
        "identity_company_binding": "EmpresaId" in usuario or "UsuarioEmpresa" in usuario,
        "live_scope_company_binding": "EmpresaId" in scope and "EmpresaId" in scope_impl,
        "resource_company_authorization": (
            "EmpresaId" in sucursal and
            ("EmpresaAutoriz" in sucursal or "Tenant" in sucursal or "AlcanceEmpresa" in sucursal)
        ),
        "persistence_tenant_filter_or_explicit_scoping": (
            "HasQueryFilter" in db and ("EmpresaId" in db or "Tenant" in db)
        ) or any_source_contains("backend/src/Infrastructure/Repositories", ("EmpresaId", "UsuarioScope")),
        "reports_tenant_aware": any_source_contains(
            "backend/src/Application/Services", ("Reporte", "EmpresaId")),
        "files_tenant_aware": any_source_contains(
            "backend/src", ("CompraDocumento", "EmpresaId")),
        "cache_keys_tenant_aware": (
            "EmpresaId" in factura_share and
            ("CorreoLocks" in factura_share or "CorreosProcesados" in factura_share)
        ),
        "background_process_tenant_aware": any_source_contains(
            "backend/src", ("BackgroundService", "EmpresaId")),
    }

    missing = [name for name, ok in checks.items() if not ok]
    certified = not missing
    print("PRIORITY3_MULTITENANT_CERTIFICATION=" + ("PASS" if certified else "NOT_CERTIFIED"))
    for name, ok in checks.items():
        print(f"TENANT_CHECK {name}={'PASS' if ok else 'MISSING'}")
    if missing:
        print("TENANT_MISSING=" + ",".join(missing))
        print("SECURITY_RULE=Empresa root or N6.1 closure alone MUST NOT be represented as full isolation")

    if args.require_certified and not certified:
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
