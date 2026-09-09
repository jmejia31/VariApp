#!/usr/bin/env python3
"""ALEX material-backlog planner for VAEP.

ALEX is a control-plane planner, not a Jules lane. It measures real unused
material work in the canonical J1-J6 catalog, classifies it under A16-A25,
detects starvation/backlog debt, and emits deterministic runtime evidence.
It never certifies LISTO_REAL, never bypasses REVIEW_FIRST and never invents
busywork.
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


def load_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise SystemExit(f"ALEX_ERROR invalid_object path={path}")
    return data


def manifest_exists(worker: str, dispatch_id: str) -> bool:
    return (LANE_PATHS[worker] / f"{dispatch_id}.json").exists()


def task_text(task: dict[str, Any]) -> str:
    return " " + " ".join(str(task.get(k, "")) for k in (
        "taskId", "dispatchId", "fileScopeHint", "prompt", "reason"
    )).lower() + " "


def classify(task: dict[str, Any]) -> list[str]:
    text = task_text(task)
    hits = [cid for cid, words in CAPABILITY_KEYWORDS.items() if any(w in text for w in words)]
    return hits or ["A25"]


def build_runtime(catalog: dict[str, Any], capabilities: dict[str, Any], auth: dict[str, Any]) -> dict[str, Any]:
    canonical = ["J1", "J2", "J3", "J4", "J5", "J6"]
    lanes = catalog.get("lanes") or {}
    if list(sorted(lanes)) != canonical:
        raise SystemExit("ALEX_ERROR catalog_lanes_not_canonical")
    if capabilities.get("authority") != "docs/VAEP_AUTHORITY.md":
        raise SystemExit("ALEX_ERROR authority_mismatch")
    if capabilities.get("operational") is not True or capabilities.get("isJulesLane") is not False:
        raise SystemExit("ALEX_ERROR invalid_operational_contract")
    if auth.get("component") != "ALEX" or auth.get("authorization") != "AUTHORIZED_NOW" or auth.get("status") != "ACTIVE_AUTHORIZATION":
        raise SystemExit("ALEX_ERROR owner_authorization_missing")

    policy = catalog.get("policy") or {}
    current_parent = str(catalog.get("currentParent") or "")
    next_parent = str((catalog.get("throughputPlan") or {}).get("nextParent") or "")
    eligible_min = int(policy.get("dispatchEligibleMinPerWorker", 2))
    programmed_target = int(policy.get("programmedBacklogTargetPerWorker", 12))
    refill_floor = int(policy.get("programmedBacklogRefillFloorPerWorker", 4))

    capability_rows = {
        item["id"]: {
            "id": item["id"],
            "name": item["name"],
            "status": "ACTIVE_SCAN",
            "candidateTaskIds": [],
            "currentParentCandidateCount": 0,
            "nextParentCandidateCount": 0,
        }
        for item in capabilities.get("capabilities", [])
    }

    lane_rows: dict[str, dict[str, Any]] = {}
    starved: list[str] = []
    low_programmed: list[str] = []

    for worker in canonical:
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

        if eligible_unused < eligible_min:
            starved.append(worker)
        if programmed_unused <= refill_floor:
            low_programmed.append(worker)
        lane_rows[worker] = {
            "programmedUnused": programmed_unused,
            "programmedTarget": programmed_target,
            "refillFloor": refill_floor,
            "eligibleUnused": eligible_unused,
            "eligibleMinimum": eligible_min,
            "currentParentUnused": current_unused,
            "nextParentUnused": next_unused,
            "starved": eligible_unused < eligible_min,
            "refillAction": "WAKE_AUTOREFILL" if eligible_unused > 0 else "MATERIAL_SCOPE_GENERATION_REQUIRED",
        }

    a25 = capability_rows.get("A25")
    if a25 is not None:
        a25["status"] = "ACTIVE_WATCH"
        a25["starvedLanes"] = starved
        a25["lowProgrammedLanes"] = low_programmed
        a25["semanticDedupe"] = "TASK_IDENTITY_UNIQUE_NO_BUSYWORK"

    return {
        "authority": "docs/VAEP_AUTHORITY.md",
        "ownerAuthorization": "vaep/control/alex-owner-authorization.json",
        "engine": "ALEX",
        "role": capabilities.get("role"),
        "operational": True,
        "generatedAtUtc": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "currentParent": current_parent,
        "nextParent": next_parent,
        "lanes": lane_rows,
        "capabilities": list(capability_rows.values()),
        "summary": {
            "starvedLaneCount": len(starved),
            "starvedLanes": starved,
            "lowProgrammedLaneCount": len(low_programmed),
            "lowProgrammedLanes": low_programmed,
            "wakePolicy": "EVENT_DRIVEN_EXISTING_LANE_REFILL_PLUS_5_MINUTE_ALEX_WATCHDOG",
            "materialScopeGenerationRequired": len(starved) > 0,
            "busyworkAllowed": False,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    runtime = build_runtime(load_json(CATALOG_PATH), load_json(CAPABILITIES_PATH), load_json(AUTH_PATH))
    rendered = json.dumps(runtime, indent=2, ensure_ascii=False) + "\n"
    if not args.check:
        Path(args.output).write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
