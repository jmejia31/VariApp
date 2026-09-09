#!/usr/bin/env python3
"""ALEX material-backlog planner for VAEP.

ALEX is a control-plane planner, not a Jules lane. It reads the live Jules
catalog, measures real unused/eligible material work, classifies candidates by
capability A16-A25, and emits deterministic runtime telemetry. It never creates
synthetic busywork, never certifies LISTO_REAL and never bypasses REVIEW_FIRST.
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
    "A17": ("frontend", "component", "route", "ux", "ui", "a11y"),
    "A18": ("data", "database", "db", "migration", "persistence", "ef", "model"),
    "A19": ("api", "integration", "endpoint", "http", "dto"),
    "A20": ("security", "rbac", "permission", "audit", "authorization"),
    "A21": ("unit", "contract", "spec", "test"),
    "A22": ("e2e", "regression", "playwright"),
    "A23": ("performance", "perf", "accessibility", "a11y", "resilience"),
    "A24": ("docs", "documentation", "runbook", "release", "certification"),
}


def load_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise SystemExit(f"ALEX_ERROR invalid_object path={path}")
    return data


def manifest_exists(worker: str, dispatch_id: str) -> bool:
    lane = LANE_PATHS[worker]
    return (lane / f"{dispatch_id}.json").exists()


def task_text(task: dict[str, Any]) -> str:
    return " ".join(
        str(task.get(key, ""))
        for key in ("taskId", "dispatchId", "fileScopeHint", "prompt", "reason")
    ).lower()


def classify(task: dict[str, Any]) -> list[str]:
    text = task_text(task)
    hits = [
        capability
        for capability, words in CAPABILITY_KEYWORDS.items()
        if any(word in text for word in words)
    ]
    return hits or ["A25"]


def build_runtime(catalog: dict[str, Any], capabilities: dict[str, Any]) -> dict[str, Any]:
    canonical = ["J1", "J2", "J3", "J4", "J5", "J6"]
    lanes = catalog.get("lanes") or {}
    if sorted(lanes) != canonical:
        raise SystemExit("ALEX_ERROR catalog_lanes_not_canonical")
    if capabilities.get("authority") != "docs/VAEP_AUTHORITY.md":
        raise SystemExit("ALEX_ERROR authority_mismatch")
    if capabilities.get("operational") is not True:
        raise SystemExit("ALEX_ERROR capabilities_not_operational")

    policy = catalog.get("policy") or {}
    current_parent = str(catalog.get("currentParent") or "")
    next_parent = str((catalog.get("throughputPlan") or {}).get("nextParent") or "")
    eligible_min = int(policy.get("dispatchEligibleMinPerWorker", 2))
    programmed_target = int(policy.get("programmedBacklogTargetPerWorker", 12))
    refill_floor = int(policy.get("programmedBacklogRefillFloorPerWorker", 4))

    capability_rows: dict[str, dict[str, Any]] = {}
    for item in capabilities.get("capabilities", []):
        capability_rows[item["id"]] = {
            "id": item["id"],
            "name": item["name"],
            "status": "ACTIVE_SCAN",
            "candidateTaskIds": [],
            "currentParentCandidateCount": 0,
            "nextParentCandidateCount": 0,
        }

    lane_rows: dict[str, dict[str, Any]] = {}
    starved: list[str] = []
    low_programmed: list[str] = []

    for worker in canonical:
        tasks = list(lanes.get(worker) or [])
        programmed_unused = 0
        eligible_unused = 0
        current_parent_unused = 0
        next_parent_unused = 0
        for task in tasks:
            dispatch_id = str(task.get("dispatchId") or "")
            if not dispatch_id or manifest_exists(worker, dispatch_id):
                continue
            programmed_unused += 1
            if task.get("dispatchEligible") is not False:
                eligible_unused += 1
            planned_parent = str(task.get("plannedParent") or "")
            if planned_parent == current_parent:
                current_parent_unused += 1
            if planned_parent == next_parent:
                next_parent_unused += 1
            for capability in classify(task):
                row = capability_rows.get(capability)
                if row is None:
                    continue
                task_id = str(task.get("taskId") or dispatch_id)
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
            "currentParentUnused": current_parent_unused,
            "nextParentUnused": next_parent_unused,
            "refillAction": "WAKE_AUTOREFILL" if eligible_unused > 0 else "NO_SAFE_ELIGIBLE_ENTRY",
        }

    a25 = capability_rows.get("A25")
    if a25 is not None:
        a25["status"] = "ACTIVE_WATCH"
        a25["starvedLanes"] = starved
        a25["lowProgrammedLanes"] = low_programmed
        a25["semanticDedupe"] = "TASK_IDENTITY_UNIQUE_NO_BUSYWORK"

    return {
        "authority": "docs/VAEP_AUTHORITY.md",
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
            "wakePolicy": "EVENT_DRIVEN_PLUS_5_MINUTE_WATCHDOG",
            "busyworkAllowed": False,
        },
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT))
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    runtime = build_runtime(load_json(CATALOG_PATH), load_json(CAPABILITIES_PATH))
    rendered = json.dumps(runtime, indent=2, ensure_ascii=False) + "\n"
    output = Path(args.output)
    if not args.check:
        output.write_text(rendered, encoding="utf-8")
    print(rendered, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
