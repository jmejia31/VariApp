#!/usr/bin/env python3
"""ALEX material-backlog planner for VAEP.

ALEX is a control-plane planner, not a Jules lane. It measures real unused
material work in the canonical J1-J6 catalog, classifies it under A16-A25,
detects starvation/backlog debt, and emits deterministic generation requests
for exact material scopes derived from the canonical roadmap. It never
certifies LISTO_REAL, never bypasses REVIEW_FIRST, never promotes a dependency
prematurely and never invents busywork merely to satisfy a numeric backlog.
Capability availability and a successful planner run never imply ACTIVE_REAL.
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

LANE_PATHS = {
    "J1": ROOT / "vaep/jules/dispatch",
    "J2": ROOT / "vaep/jules-b/dispatch",
    "J3": ROOT / "vaep/jules-c/dispatch",
    "J4": ROOT / "vaep/jules-d/dispatch",
    "J5": ROOT / "vaep/j5/dispatch",
    "J6": ROOT / "vaep/j6/dispatch",
}

CAPABILITY_KEYWORDS = {
    "A16": ("backend", "service", "controller", "application", "domain"),
    "A17": ("frontend", "component", "route", "ux", " ui ", "a11y"),
    "A18": ("data", "database", " db ", "migration", "persistence", " ef ", "model"),
    "A19": (" api", "integration", "endpoint", "http", "dto"),
    "A20": ("security", "rbac", "permission", "audit", "authorization"),
    "A21": ("unit", "contract", " spec", " test"),
    "A22": ("e2e", "regression", "playwright"),
    "A23": ("performance", " perf", "accessibility", "a11y", "resilience"),
    "A24": ("docs/", "documentation", "runbook", "release", "certification"),
}

CANONICAL_LANES = ["J1", "J2", "J3", "J4", "J5", "J6"]


def load_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise SystemExit(f"ALEX_ERROR invalid_object path={path}")
    return data


def load_optional_json(path: Path) -> dict[str, Any]:
    if not path.exists():
        return {}
    return load_json(path)


def manifest_exists(worker: str, dispatch_id: str) -> bool:
    return (LANE_PATHS[worker] / f"{dispatch_id}.json").exists()


def task_text(task: dict[str, Any]) -> str:
    return " " + " ".join(
        str(task.get(k, ""))
        for k in ("taskId", "dispatchId", "fileScopeHint", "prompt", "reason")
    ).lower() + " "


def classify(task: dict[str, Any]) -> list[str]:
    text = task_text(task)
    hits = [cid for cid, words in CAPABILITY_KEYWORDS.items() if any(w in text for w in words)]
    return hits or ["A25"]


def roadmap_candidates(catalog: dict[str, Any], current_parent: str) -> list[dict[str, Any]]:
    roadmap = catalog.get("roadmap") or {}
    nodes = list(roadmap.get("nodes") or [])
    receipts = set((catalog.get("closureReceipts") or {}).keys())
    current_order = -1
    for node in nodes:
        if str(node.get("id") or "") == current_parent:
            try:
                current_order = int(node.get("order", -1))
            except (TypeError, ValueError):
                current_order = -1
            break

    candidates: list[dict[str, Any]] = []
    for node in sorted(nodes, key=lambda item: int(item.get("order", 10**9))):
        parent_id = str(node.get("id") or "")
        if not parent_id or parent_id == current_parent or parent_id in receipts:
            continue
        if str(node.get("type") or "") != "MICROTAREA":
            continue
        try:
            order = int(node.get("order", 10**9))
        except (TypeError, ValueError):
            order = 10**9
        if current_order >= 0 and order <= current_order:
            continue
        dependencies = [str(dep) for dep in (node.get("dependencies") or []) if str(dep)]
        unmet = [dep for dep in dependencies if dep not in receipts]
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
    lanes = catalog.get("lanes") or {}
    if list(sorted(lanes)) != CANONICAL_LANES:
        raise SystemExit("ALEX_ERROR catalog_lanes_not_canonical")
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

    policy = catalog.get("policy") or {}
    current_parent = str(catalog.get("currentParent") or "")
    next_parent = str((catalog.get("throughputPlan") or {}).get("nextParent") or "")
    eligible_min = int(policy.get("dispatchEligibleMinPerWorker", 2))
    programmed_target = int(policy.get("programmedBacklogTargetPerWorker", 12))
    refill_floor = int(policy.get("programmedBacklogRefillFloorPerWorker", 4))
    admission_state = str(admission.get("newDispatchAdmission") or "UNKNOWN")
    allow_existing_sessions = admission.get("allowExistingActiveSessions") is True
    candidates = roadmap_candidates(catalog, current_parent)
    source_ranges = list(((catalog.get("roadmap") or {}).get("sourceRanges") or []))

    capability_rows: dict[str, dict[str, Any]] = {}
    for item in capabilities.get("capabilities", []):
        capability_rows[item["id"]] = {
            "id": item["id"],
            "name": item["name"],
            "mode": item.get("mode", "ALEX_CAPABILITY"),
            "status": item.get("status", "AVAILABLE__NOT_ACTIVE_REAL"),
            "outputType": item.get("outputType", "TASK_CANDIDATE"),
            "candidateTaskIds": [],
            "currentParentCandidateCount": 0,
            "nextParentCandidateCount": 0,
        }

    lane_rows: dict[str, dict[str, Any]] = {}
    generation_requests: list[dict[str, Any]] = []
    starved: list[str] = []
    low_programmed: list[str] = []

    for worker in CANONICAL_LANES:
        programmed_unused = eligible_unused = current_unused = next_unused = 0
        for task in list(lanes.get(worker) or []):
            dispatch_id = str(task.get("dispatchId") or "")
            if not dispatch_id or manifest_exists(worker, dispatch_id):
                continue
            programmed_unused += 1
            eligible = task.get("dispatchEligible") is not False
            if eligible:
                eligible_unused += 1
            planned_parent = str(task.get("plannedParent") or "")
            if planned_parent == current_parent:
                current_unused += 1
            if planned_parent == next_parent:
                next_unused += 1
            for capability in classify(task):
                row = capability_rows.get(capability)
                if row is None:
                    continue
                task_id = str(task.get("taskId") or dispatch_id)
                if task_id not in row["candidateTaskIds"]:
                    row["candidateTaskIds"].append(task_id)
                if planned_parent == current_parent:
                    row["currentParentCandidateCount"] += 1
                if planned_parent == next_parent:
                    row["nextParentCandidateCount"] += 1

        programmed_deficit = max(programmed_target - programmed_unused, 0)
        eligible_deficit = max(eligible_min - eligible_unused, 0)
        is_starved = eligible_unused < eligible_min
        is_low_programmed = programmed_unused <= refill_floor
        if is_starved:
            starved.append(worker)
        if is_low_programmed:
            low_programmed.append(worker)

        if is_starved or is_low_programmed:
            requested = max(programmed_deficit, eligible_deficit)
            request_candidates = candidates[: max(1, min(requested, len(candidates)))] if candidates else []
            generation_requests.append(
                {
                    "workerId": worker,
                    "action": "MATERIALIZE_ROADMAP_DERIVED_SCOPES",
                    "programmedDeficit": programmed_deficit,
                    "eligibleDeficit": eligible_deficit,
                    "requestedMaterialScopes": requested,
                    "currentParent": current_parent,
                    "nextParent": next_parent,
                    "admissionState": admission_state,
                    "materializationOnly": admission_state != "OPEN",
                    "candidateParents": request_candidates,
                    "roadmapSourceRanges": source_ranges,
                    "requiresRoadmapExpansion": requested > len(request_candidates),
                    "constraints": [
                        "MATERIAL_ONLY",
                        "SEMANTIC_DEDUPE",
                        "DEPENDENCY_GATES",
                        "DISTINCT_WRITE_SCOPE",
                        "FUTURE_PARENT_NOT_DISPATCH_ELIGIBLE",
                        "NO_BUSYWORK",
                    ],
                }
            )

        lane_rows[worker] = {
            "programmedUnused": programmed_unused,
            "programmedTarget": programmed_target,
            "programmedDeficit": programmed_deficit,
            "refillFloor": refill_floor,
            "eligibleUnused": eligible_unused,
            "eligibleMinimum": eligible_min,
            "eligibleDeficit": eligible_deficit,
            "currentParentUnused": current_unused,
            "nextParentUnused": next_unused,
            "starved": is_starved,
            "refillAction": (
                "ALEX_ROADMAP_GENERATION_REQUEST"
                if is_starved or is_low_programmed
                else "BACKLOG_HEALTHY"
            ),
        }

    a25 = capability_rows.get("A25")
    if a25 is not None:
        a25["watchState"] = "CAPABILITY_AVAILABLE__NOT_RUNTIME_PROOF"
        a25["starvedLanes"] = starved
        a25["lowProgrammedLanes"] = low_programmed
        a25["semanticDedupe"] = "TASK_IDENTITY_UNIQUE_NO_BUSYWORK"
        a25["generationRequestCount"] = len(generation_requests)

    return {
        "authority": "docs/VAEP_AUTHORITY.md",
        "ownerAuthorization": "vaep/control/alex-owner-authorization.json",
        "engine": "ALEX",
        "role": capabilities.get("role"),
        "operational": True,
        "runtimeState": "CONTROL_PLANE_SNAPSHOT__NOT_ACTIVE_REAL",
        "generatedAtUtc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "currentParent": current_parent,
        "nextParent": next_parent,
        "admission": {
            "newDispatchAdmission": admission_state,
            "allowExistingActiveSessions": allow_existing_sessions,
            "reason": str(admission.get("reason") or ""),
        },
        "lanes": lane_rows,
        "capabilities": list(capability_rows.values()),
        "generationRequests": generation_requests,
        "summary": {
            "starvedLaneCount": len(starved),
            "starvedLanes": starved,
            "lowProgrammedLaneCount": len(low_programmed),
            "lowProgrammedLanes": low_programmed,
            "generationRequestCount": len(generation_requests),
            "wakePolicy": "EVENT_DRIVEN_PUSH_AND_WORKFLOW_RUN",
            "watchdogCadence": "NO_INDEPENDENT_5_MINUTE_TIMER",
            "deepRefillMode": "ROADMAP_GENERATION_REQUESTS_AVAILABLE__NOT_ACTIVE_REAL",
            "materialScopeGenerationRequired": bool(generation_requests),
            "genericRegenerationAllowed": False,
            "busyworkAllowed": False,
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
