#!/usr/bin/env python3
"""Dependency-safe transitions using the catalog's explicit, source-backed roadmap.

This module does not certify work, call providers or publish refs. Closure
receipts and the live causal/ownership gates remain the controller's job.
"""
import argparse
import copy
import json
from pathlib import Path
import re
import sys

MASTER = "docs/VAEP_AUTHORITY.md"
SHA40 = re.compile(r"[0-9a-fA-F]{40}")
STATES = {"FROZEN", "OPEN"}


def valid_closure(receipt, parent):
    return (isinstance(receipt, dict)
            and receipt.get("authority") == MASTER
            and receipt.get("parent") == parent
            and receipt.get("decision") == "LISTO_REAL"
            and isinstance(receipt.get("functionalHead"), str)
            and SHA40.fullmatch(receipt["functionalHead"]) is not None
            and receipt.get("review") == "PASS"
            and receipt.get("combinedStatus") == "SUCCESS"
            and receipt.get("causalGates") == "TERMINAL_SUCCESS_APPLICABLE"
            and type(receipt.get("p0Open")) is int and receipt["p0Open"] == 0
            and type(receipt.get("p1Open")) is int and receipt["p1Open"] == 0
            and receipt.get("productionTouched") is False
            and receipt.get("mergePerformed") is False)


def read_closures(directory):
    closed = set()
    for path in sorted(Path(directory).glob("*_LISTO_REAL_*.json")):
        try:
            receipt = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        if isinstance(receipt, dict) and valid_closure(receipt, receipt.get("parent")):
            if isinstance(receipt.get("parent"), str) and receipt["parent"]:
                closed.add(receipt["parent"])
    return closed


def roadmap_nodes(catalog):
    roadmap = catalog.get("roadmap")
    if not isinstance(roadmap, dict) or roadmap.get("authority") != MASTER:
        raise ValueError("CANONICAL_ROADMAP_MISSING")
    if not isinstance(roadmap.get("sourceSpreadsheetId"), str) or not roadmap["sourceSpreadsheetId"]:
        raise ValueError("ROADMAP_SOURCE_MISSING")
    items = roadmap.get("nodes")
    if not isinstance(items, list) or not items:
        raise ValueError("ROADMAP_NODES_MISSING")
    nodes, orders = {}, set()
    for node in items:
        if not isinstance(node, dict):
            raise ValueError("INVALID_ROADMAP_NODE")
        ident, order, deps = node.get("id"), node.get("order"), node.get("dependencies")
        if (not isinstance(ident, str) or not ident or ident in nodes
                or type(order) is not int or order in orders
                or not isinstance(deps, list) or any(not isinstance(d, str) or not d for d in deps)
                or len(deps) != len(set(deps)) or ident in deps
                or node.get("type") not in {"MICROTAREA", "GATE_FASE"}):
            raise ValueError("INVALID_OR_DUPLICATE_ROADMAP_NODE")
        if node.get("type") == "MICROTAREA" and not isinstance(node.get("phaseGate"), str):
            raise ValueError("PHASE_GATE_REQUIRED")
        nodes[ident] = node
        orders.add(order)
    for ident, node in nodes.items():
        for dep in node["dependencies"]:
            if dep in nodes and nodes[dep]["order"] >= node["order"]:
                raise ValueError("DEPENDENCY_ORDER_OR_CYCLE")
    return nodes


def choose_next(catalog, closed):
    nodes = roadmap_nodes(catalog)
    current = catalog.get("currentParent")
    if current not in nodes:
        raise ValueError("CURRENT_PARENT_NOT_IN_ROADMAP")
    if current not in closed:
        raise ValueError("CURRENT_PARENT_NOT_CERTIFIED")
    # Do not skip an unclosed gate/node simply because a later task looks runnable.
    remaining = [n for n in nodes.values()
                 if n["order"] > nodes[current]["order"] and n["id"] not in closed]
    if not remaining:
        return None
    candidate = min(remaining, key=lambda n: n["order"])
    required = set(candidate["dependencies"])
    if candidate.get("phaseGate"):
        required.add(candidate["phaseGate"])
    missing = sorted(required - set(closed))
    if missing:
        raise ValueError("DEPENDENCIES_NOT_CLOSED:" + ",".join(missing))
    return candidate["id"]


def transition(catalog, admission, closed, receipt_path, functional, now, hardening_ok=False):
    if admission.get("newDispatchAdmission") not in STATES or admission.get("allowExistingActiveSessions") is not True:
        raise ValueError("INVALID_ADMISSION_CONTRACT")
    current = catalog.get("currentParent")
    target = choose_next(catalog, closed)
    result, access = copy.deepcopy(catalog), copy.deepcopy(admission)
    result.setdefault("closureReceipts", {})[current] = receipt_path
    result["lastClosedParent"] = current
    result["lastClosureFunctionalHead"] = functional
    result["generatedAt"] = now
    if target is None:
        result["regenerationReason"] = "CURRENT_PARENT_CERTIFIED__NEXT_PARENT_MISSING"
        access.update(newDispatchAdmission="FROZEN", updatedAtUtc=now)
        if admission["newDispatchAdmission"] == "OPEN" or str(admission.get("reason", "")).startswith((
                "FROZEN_GATE_N4_CURRENT_PARENT__", "FROZEN_N4.11.H_REVIEW_DEBT_RECONCILED_NEXT_PARENT_MISSING_",
                "FROZEN_NEXT_PARENT_MISSING_NO_SAFE_WORK", "FROZEN_PHASE_GATE_REQUIRED__")):
            access["reason"] = "FROZEN_NEXT_PARENT_MISSING_NO_SAFE_WORK"
        for tasks in result.get("lanes", {}).values():
            for task in tasks:
                task["dispatchEligible"] = False
        return result, access
    node = roadmap_nodes(catalog)[target]
    result["currentParent"] = target
    result["regenerationReason"] = "AUTOMATIC_PARENT_CLOSE__" + current + "_LISTO_REAL__" + target + "_CURRENT"
    material = 0
    for tasks in result.get("lanes", {}).values():
        for task in tasks:
            enabled = (node["type"] == "MICROTAREA"
                       and task.get("plannedParent") == target
                       and task.get("readyForDispatch") is True
                       and isinstance(task.get("fileScopeHint"), str) and bool(task["fileScopeHint"].strip())
                       and isinstance(task.get("prompt"), str) and bool(task["prompt"].strip()))
            task["dispatchEligible"] = enabled
            material += int(enabled)
    reason = admission.get("reason", "")
    # Only an explicitly recognized, reconciled roadmap freeze can be reopened.
    permitted = isinstance(reason, str) and reason.startswith((
        "FROZEN_GATE_N4_CURRENT_PARENT__", "FROZEN_N4.11.H_REVIEW_DEBT_RECONCILED_NEXT_PARENT_MISSING_",
        "FROZEN_NEXT_PARENT_MISSING_NO_SAFE_WORK", "FROZEN_PHASE_GATE_REQUIRED__"))
    opened = (node["type"] == "MICROTAREA" and material > 0 and hardening_ok
              and (admission["newDispatchAdmission"] == "OPEN" or permitted))
    access.update(updatedAtUtc=now)
    if opened:
        access.update(newDispatchAdmission="OPEN", reason="VERIFIED_ROADMAP_PROMOTION__" + current + "__" + target)
    elif admission["newDispatchAdmission"] == "FROZEN" and not permitted:
        # Preserve a manual/security freeze reason; future runs must not mistake it
        # for a roadmap freeze that they are permitted to clear.
        pass
    else:
        access.update(newDispatchAdmission="FROZEN", reason=("FROZEN_PHASE_GATE_REQUIRED__" + target
                      if node["type"] == "GATE_FASE" else "FROZEN_PROMOTION_HARDENING_OR_SAFE_WORK_REQUIRED__" + target))
    return result, access


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["select", "prepare"])
    parser.add_argument("--catalog", required=True)
    parser.add_argument("--fragments", default="vaep/evidence/fragments")
    parser.add_argument("--admission")
    parser.add_argument("--receipt")
    parser.add_argument("--functional")
    parser.add_argument("--now")
    parser.add_argument("--hardening-ok", action="store_true")
    args = parser.parse_args()
    try:
        catalog = json.loads(Path(args.catalog).read_text(encoding="utf-8"))
        closed = read_closures(args.fragments)
        if args.mode == "select":
            target = choose_next(catalog, closed)
            if target:
                print(target)
            return 0
        if not all([args.admission, args.receipt, args.functional, args.now]):
            raise ValueError("PREPARE_ARGUMENTS_MISSING")
        receipt = json.loads(Path(args.receipt).read_text(encoding="utf-8"))
        if not valid_closure(receipt, catalog.get("currentParent")) or receipt["functionalHead"] != args.functional:
            raise ValueError("CURRENT_CLOSURE_RECEIPT_MISMATCH")
        admission = json.loads(Path(args.admission).read_text(encoding="utf-8"))
        result, access = transition(catalog, admission, closed, args.receipt, args.functional, args.now, args.hardening_ok)
        print(json.dumps({"catalog": result, "admission": access}, ensure_ascii=False, indent=2))
        return 0
    except (OSError, ValueError, TypeError, KeyError) as exc:
        print("VAEP_TRANSITION_BLOCKED=" + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
