#!/usr/bin/env python3
"""Causal validator for N8.18.E FRONTEND_UX architecture cleanup."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "frontend" / "src" / "app"
NAV = APP / "shared" / "navigation" / "app-navigation-menu.component.ts"
CONTRACT = APP / "shared" / "navigation" / "navigation-contract.json"
ALERT = APP / "shared" / "alerts" / "app-alert.service.ts"
TOAST = APP / "shared" / "alerts" / "app-toast.service.ts"
SHELL = APP / "app.component.ts"


def fail(message: str) -> None:
    raise SystemExit(f"N8.18.E FAIL: {message}")


def read(path: Path) -> str:
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def main() -> None:
    nav_text = read(NAV)
    contract = json.loads(read(CONTRACT))
    alert_text = read(ALERT)
    toast_text = read(TOAST)
    shell_text = read(SHELL)

    if contract.get("schema") != "variapp-navigation-contract/v1":
        fail("unexpected navigation contract schema")
    if contract.get("remove_safe_deletions") != 0:
        fail("N8.18.E must not claim REMOVE_SAFE deletions")

    groups = contract.get("groups") or []
    if len(groups) < 8:
        fail("navigation contract must expose explicit parent groups")

    contract_routes: list[str] = []
    for group in groups:
        children = group.get("children") or []
        if not group.get("id") or not children:
            fail("every navigation parent requires an id and at least one child")
        for child in children:
            route = child.get("route")
            if not route or not child.get("modulo") or not child.get("accion"):
                fail(f"incomplete child contract in parent {group.get('id')}")
            contract_routes.append(route)

    if len(contract_routes) != len(set(contract_routes)):
        fail("a route is classified under more than one parent")

    nav_routes = re.findall(r'routerLink="(/[^"#?]+)"', nav_text)
    if sorted(nav_routes) != sorted(contract_routes):
        missing = sorted(set(contract_routes) - set(nav_routes))
        extra = sorted(set(nav_routes) - set(contract_routes))
        fail(f"navigation template/contract drift; missing={missing}; extra={extra}")

    required_parent_ids = {
        "nav-inicio", "nav-catalogo", "nav-operacion", "nav-compras",
        "nav-ventas", "nav-inventario", "nav-finanzas", "nav-administracion",
    }
    for parent_id in required_parent_ids:
        if f'aria-labelledby="{parent_id}"' not in nav_text:
            fail(f"missing accessible parent group {parent_id}")

    report_guard = re.compile(
        r"@if \(permisosRuntime\.esAdministrador\(\) && "
        r"permisosRuntime\.puede\('ReportesAdministrativos', 'Ver'\)\) \{\s*"
        r"<a routerLink=\"/centro-reportes\"",
        re.MULTILINE,
    )
    if not report_guard.search(nav_text):
        fail("centro-reportes menu visibility must preserve admin + ReportesAdministrativos guard")

    if "<app-navigation-menu />" not in shell_text:
        fail("app shell does not render the canonical navigation component")
    if "aria-label=\"Navegación principal\"" not in shell_text:
        fail("main navigation lost its accessibility label")
    for marker in ("skip-link", "routeAnnouncement", "aria-controls=\"main-sidebar\"", "aria-expanded"):
        if marker not in shell_text:
            fail(f"shell accessibility regression: {marker}")

    for semantic in ("'info'", "'success'", "'warning'", "'error'", "'confirm'"):
        if semantic not in alert_text:
            fail(f"global alert primitive missing semantic {semantic}")
    for legacy in ("'advertencia'", "'peligro'"):
        if legacy not in alert_text:
            fail(f"legacy alert compatibility missing: {legacy}")

    if "MatSnackBar" not in toast_text:
        fail("shared transient notification channel must use MatSnackBar")
    for method in ("info(", "success(", "warning(", "error("):
        if method not in toast_text:
            fail(f"toast channel missing method {method[:-1]}")

    forbidden: list[str] = []
    for path in APP.rglob("*"):
        if path.suffix not in {".ts", ".html"} or not path.is_file():
            continue
        text = path.read_text(encoding="utf-8")
        if re.search(r"\bwindow\.(?:alert|confirm)\s*\(", text):
            forbidden.append(str(path.relative_to(ROOT)))
    if forbidden:
        fail(f"screen-local browser alert/confirm usage remains: {forbidden}")

    print(
        "N8.18.E PASS: "
        f"parents={len(groups)} routes={len(contract_routes)} "
        "alert_semantics=5 toast_channel=shared report_guard=preserved "
        "remove_safe_deletions=0 shell_accessibility=preserved"
    )


if __name__ == "__main__":
    main()
