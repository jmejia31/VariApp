#!/usr/bin/env python3
"""ALEX tasks-first planner for VAEP.

ALEX is a read-only/control-plane planner. It identifies the shortest material
path to close CURRENT_PARENT and safe future roadmap candidates. It never
creates manifests, dispatches Jules, writes product code, certifies LISTO_REAL,
or treats an idle Jules worker as debt.

J1-J6 are optional accelerators under TASKS_FIRST_JULES_ON_DEMAND. ALEX may
surface a possible offload candidate, but only the active scheduled
controller may approve it after fresh HEAD, dependency, dedupe, lease and
non-overlap checks.
"""
from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
CATALOG_PATH = ROOT / "vaep/control/jules-autorefill-catalog.json"
CAPABILITIES_PATH = ROOT / "vaep/control/alex-capabilities.json"
AUTH_PATH = ROOT / "vaep/control/alex-owner-authorization.json"
ADMISSION_PATH = ROOT / "vaep/control/dispatch-admission.json"
DEFAULT_OUTPUT = ROOT / "vaep/control/alex-runtime.json"

CANONICAL_WORKERS = ["J1", "J2", "J3", "J4", "J5", "J6"]


def load_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise SystemExit(f"ALEX_ERROR invalid_object path={path}")
    return data


def load_optional_json(path: Path) -> dict[str, Any]:
    return load_json(path) if path.exists() else {}


def _order(node: dict[str, Any]) -> int:
    try:
        return int(node.get("order", 10**9))
    except (TypeError, ValueError):
        return 10**9


def roadmap_candidates(catalog: dict[str, Any], current_parent: str) -> list[dict[str, Any]]:
    roadmap = catalog.get("roadmap") or {}
    nodes = list(roadmap.get("nodes") or [])
    receipts = set((catalog.get("closureReceipts") or {}).keys())
    current_order = next((_order(n) for n in nodes if str(n.get("id") or "") == current_parent), -1)

    candidates: list[dict[str, Any]] = []
    for node in sorted(nodes, key=_order):
        parent_id = str(node.get("id") or "")
        if not parent_id or parent_id == current_parent or parent_id in receipts:
            continue
        order = _order(node)
        if current_order >= 0 and order <= current_order:
            continue
        dependencies = [str(d) for d in (node.get("dependencies") or []) if str(d)]
        unmet = [d for d in dependencies if d not in receipts]
        candidates.append(
            {
                "parentId": parent_id,
                "order": order,
                "dependencies": dependencies,
                "dependencyState": "READY" if not unmet else "GATED",
                "unmetDependencies": unmet,
                "phaseGate": str(node.get("phaseGate") or ""),
            }
        )
    return candidates


def build_runtime(
    catalog: dict[str, Any],
    capabilities: dict[str, Any],
    auth: dict[str, Any],
    admission: dict[str, Any],
) -> dict[str, Any]:
    if capabilities.get("authority") != "docs/VAEP_AUTHORITY.md":
        raise SystemExit("ALEX_ERROR authority_mismatch")
    if capabilities.get("operational") is not True or capabilities.get("isJulesLane") is not False:
        raise SystemExit("ALEX_ERROR invalid_operational_contract")
    if (
        auth.get("component") != "ALEX"
        or auth.get("authorization") != "AUTHORIZED_NOW"
        or auth.get("status") != "ACTIVE_AUTHORIZATION"
    ):
        raise SystemExit("ALEX_ERROR owner_authorization_missing")

    execution_model = str(
        catalog.get("executionModel")
        or (catalog.get("policy") or {}).get("executionModel")
        or ""
    )
    if execution_model != "TASKS_FIRST_JULES_ON_DEMAND":
        raise SystemExit(f"ALEX_ERROR execution_model_mismatch value={execution_model or 'MISSING'}")

    current_parent = str(catalog.get("currentParent") or "")
    throughput = catalog.get("throughputPlan") or {}
    next_parent = str(throughput.get("nextParent") or "")
    admission_state = str(admission.get("newDispatchAdmission") or "UNKNOWN")
    candidates = roadmap_candidates(catalog, current_parent)
    ready_future = [c for c in candidates if c["dependencyState"] == "READY"]

    # ALEX may suggest, never approve, a Jules offload. The active controller
    # must still prove materiality, independent write-scope and critical-path
    # gain on fresh state. GATED work is never presented as dispatchable.
    offload_candidates = [
        {
            **candidate,
            "status": "CANDIDATE_ONLY_NOT_APPROVED",
            "requiresFreshHead": True,
            "requiresLeaseNonOverlapProof": True,
            "requiresCriticalPathGainProof": True,
            "mayBlockCurrentParent": False,
        }
        for candidate in ready_future
    ]

    capability_rows: list[dict[str, Any]] = []
    for item in capabilities.get("capabilities", []):
        if not isinstance(item, dict) or not item.get("id"):
            continue
        capability_rows.append(
            {
                "id": item["id"],
                "name": item.get("name", item["id"]),
                "mode": item.get("mode", "ALEX_CAPABILITY"),
                "status": "AVAILABLE__NOT_ACTIVE_REAL",
                "outputType": "PLANNING_ADVICE",
            }
        )

    worker_rows = {
        worker: {
            "operatingMode": "AVAILABLE_ON_DEMAND",
            "requiredForProgress": False,
            "minimumUtilizationTarget": 0,
            "queueFloor": 0,
            "starved": False,
            "debt": False,
        }
        for worker in CANONICAL_WORKERS
    }

    return {
        "authority": "docs/VAEP_AUTHORITY.md",
        "ownerAuthorization": "vaep/control/alex-owner-authorization.json",
        "engine": "ALEX",
        "role": capabilities.get("role"),
        "operational": True,
        "executionModel": execution_model,
        "runtimeState": "CONTROL_PLANE_SNAPSHOT__NOT_ACTIVE_REAL",
        "generatedAtUtc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "currentParent": current_parent,
        "nextParent": next_parent,
        "admission": {
            "newDispatchAdmission": admission_state,
            "allowExistingActiveSessions": admission.get("allowExistingActiveSessions") is True,
            "reason": str(admission.get("reason") or ""),
            "note": "Admission controls optional Jules dispatch only; it never gates direct task execution.",
        },
        "directPlan": {
            "priority": "CURRENT_PARENT_SHORTEST_MATERIAL_GAP_TO_LISTO_REAL",
            "executor": "SCHEDULED_AUTOMATION_CONTROLLER",
            "currentParent": current_parent,
            "nextParent": next_parent,
            "leaseRequired": True,
            "reviewFirstRequired": True,
            "closureChainSameRun": True,
            "directNextSafePrearm": True,
            "waitForJules": False,
        },
        "workers": worker_rows,
        # Backward-compatible key intentionally empty. Generation requests were
        # a Jules-first concept and are not production work in the new model.
        "generationRequests": [],
        "julesOffloadCandidates": offload_candidates,
        "roadmapCandidates": candidates,
        "capabilities": capability_rows,
        "summary": {
            "productionKpi": "LISTO_REAL_PARENT_CLOSE",
            "julesUtilizationIsKpi": False,
            "julesBacklogIsGate": False,
            "automaticRefill": False,
            "queueFloor": 0,
            "readyFutureCandidateCount": len(ready_future),
            "offloadCandidateCount": len(offload_candidates),
            "approvedOffloadCount": 0,
            "busyworkAllowed": False,
            "genericRegenerationAllowed": False,
            "decision": "DIRECT_EXECUTION_FIRST",
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    runtime = build_runtime(
        load_json(CATALOG_PATH),
        load_json(CAPABILITIES_PATH),
        load_json(AUTH_PATH),
        load_optional_json(ADMISSION_PATH),
    )
    rendered = json.dumps(runtime, indent=2, ensure_ascii=False) + "\n"
    if not args.check:
        Path(args.output).write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
