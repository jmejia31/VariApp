#!/usr/bin/env python3
"""Dependency-safe transitions using the catalog's explicit, source-backed roadmap.

This module does not certify work, call providers or publish refs. Closure
receipts and the live causal/ownership gates remain the controller's job.

Global dispatch admission is an OPEN-only invariant. Unsafe or unavailable work
is blocked at task/lane eligibility, never by closing the whole factory.
"""
import argparse
import copy
import json
from pathlib import Path
import re
import sys

MASTER = "docs/VAEP_AUTHORITY.md"
SHA40 = re.compile(r"[0-9a-fA-F]{40}")
ACCEPTED_INPUT_STATES = {"OPEN"}


def receipt_parent(receipt):
    if not isinstance(receipt, dict):
        return None
    return receipt.get("parent") or receipt.get("parentId")


def _legacy_closure_contract(receipt):
    return (receipt.get("decision") == "LISTO_REAL"
            and receipt.get("review") == "PASS"
            and receipt.get("combinedStatus") == "SUCCESS"
            and receipt.get("causalGates") == "TERMINAL_SUCCESS_APPLICABLE"
            and receipt.get("productionTouched") is False
            and receipt.get("mergePerformed") is False)


def _current_closure_contract(receipt):
    gates = receipt.get("causalGates")
    return (receipt.get("state") == "LISTO_REAL"
            and isinstance(receipt.get("review"), dict)
            and bool(receipt["review"].get("mode"))
            and receipt.get("headRevalidated") is True
            and isinstance(gates, list) and len(gates) > 0
            and all(isinstance(gate, dict) and gate.get("conclusion") == "success" for gate in gates)
            and receipt.get("mainTouched") is False
            and receipt.get("prMerged") is False)


def valid_closure(receipt, parent):
    return (isinstance(receipt, dict)
            and receipt.get("authority") == MASTER
            and receipt_parent(receipt) == parent
            and isinstance(receipt.get("functionalHead"), str)
            and SHA40.fullmatch(receipt["functionalHead"]) is not None
            and type(receipt.get("p0Open")) is int and receipt["p0Open"] == 0
            and type(receipt.get("p1Open")) is int and receipt["p1Open"] == 0
            and (_legacy_closure_contract(receipt) or _current_closure_contract(receipt)))


def read_closures(directory):
    closed = set()
    for path in sorted(Path(directory).glob("*_LISTO_REAL_*.json")):
        try:
            receipt = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            continue
        parent = receipt_parent(receipt)
        if isinstance(parent, str) and parent and valid_closure(receipt, parent):
            closed.add(parent)
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


def _roadmap_successor(catalog, parent):
    nodes = roadmap_nodes(catalog)
    if parent not in nodes:
        return None
    later = [n for n in nodes.values() if n["order"] > nodes[parent]["order"]]
    if not later:
        return None
    return min(later, key=lambda n: n["order"])["id"]


def _material_scope_count(catalog, parent):
    if not parent:
        return 0
    count = 0
    for tasks in catalog.get("lanes", {}).values():
        for task in tasks:
            if (task.get("plannedParent") == parent
                    and task.get("readyForDispatch") is True
                    and isinstance(task.get("fileScopeHint"), str) and task["fileScopeHint"].strip()
                    and isinstance(task.get("prompt"), str) and task["prompt"].strip()):
                count += 1
    return count


def _sync_operational_metadata(result, closed_parent, current_parent, current_material):
    next_parent = _roadmap_successor(result, current_parent) if current_parent else None
    next_material = _material_scope_count(result, next_parent)
    plan = result.setdefault("throughputPlan", {})
    plan.update(
        mode=plan.get("mode", "MATERIAL_SWARM_FIRST"),
        currentParent=current_parent,
        currentParentMaterialScopeCount=current_material,
        currentParentReason=(
            f"{closed_parent} is LISTO_REAL. {current_parent} is dependency-valid with "
            f"{current_material} material scope(s); no additional busywork is fabricated."
            if current_parent else
            f"{closed_parent} is LISTO_REAL and no successor exists in the current source-backed roadmap."
        ),
        nextParent=next_parent,
        nextParentParallelMaterialScopes=next_material,
        nextParentScopesNonOverlapping=True,
        laneRefill=plan.get("laneRefill", "POST_TERMINAL_AUTOREFILL_PLUS_CANONICAL_CHECKPOINTS"),
    )
    roadmap = result.get("roadmap")
    if isinstance(roadmap, dict):
        if current_parent:
            suffix = (f" Next roadmap node in this catalog is {next_parent}." if next_parent
                      else " This catalog excerpt has no successor; extend only from fresh Plan/COLA evidence.")
            roadmap["note"] = (f"Dependency order derived from live Plan/COLA. {closed_parent} is LISTO_REAL; "
                               f"{current_parent} is current.{suffix}")
        else:
            roadmap["note"] = (f"Dependency order derived from live Plan/COLA. {closed_parent} is LISTO_REAL; "
                               "no successor exists in the current source-backed roadmap.")


def _open_only(access, now, reason):
    access.update(newDispatchAdmission="OPEN", allowExistingActiveSessions=True,
                  reason=reason, updatedAtUtc=now)


def transition(catalog, admission, closed, receipt_path, functional, now, hardening_ok=False):
    state = admission.get("newDispatchAdmission")
    allow_active = admission.get("allowExistingActiveSessions")
    if state not in ACCEPTED_INPUT_STATES or allow_active is not True:
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
        for tasks in result.get("lanes", {}).values():
            for task in tasks:
                task["dispatchEligible"] = False
                if task.get("plannedParent") == current:
                    task["reason"] = "CLOSED_BY_" + current + "_LISTO_REAL_RECEIPT"
        _sync_operational_metadata(result, current, current, 0)
        _open_only(access, now,
                   "OPEN_NO_SUCCESSOR__" + current + "__NO_ELIGIBLE_DISPATCH__OPEN_ONLY")
        return result, access

    node = roadmap_nodes(catalog)[target]
    result["currentParent"] = target
    result["regenerationReason"] = "AUTOMATIC_PARENT_CLOSE__" + current + "_LISTO_REAL__" + target + "_CURRENT"
    material = 0
    eligible_count = 0
    for tasks in result.get("lanes", {}).values():
        for task in tasks:
            material_scope = (node["type"] == "MICROTAREA"
                              and task.get("plannedParent") == target
                              and task.get("readyForDispatch") is True
                              and isinstance(task.get("fileScopeHint"), str) and bool(task["fileScopeHint"].strip())
                              and isinstance(task.get("prompt"), str) and bool(task["prompt"].strip()))
            if material_scope:
                material += 1
            enabled = material_scope and hardening_ok
            task["dispatchEligible"] = enabled
            eligible_count += int(enabled)
            if enabled:
                task["reason"] = "CURRENT_PARENT__DEPENDENCIES_CLOSED__MATERIAL_SCOPE"
            elif material_scope and not hardening_ok:
                task["reason"] = "HARDENING_REQUIRED__MATERIAL_SCOPE_NOT_DISPATCHED"
            elif task.get("plannedParent") == current:
                task["reason"] = "CLOSED_BY_" + current + "_LISTO_REAL_RECEIPT"

    _sync_operational_metadata(result, current, target, material)

    if eligible_count > 0:
        reason = "VERIFIED_ROADMAP_PROMOTION__" + current + "__" + target
    elif node["type"] == "GATE_FASE":
        reason = "OPEN_PHASE_GATE_REQUIRED__" + target + "__NO_JULES_DISPATCH"
    elif not hardening_ok:
        reason = "OPEN_HARDENING_REQUIRED__" + target + "__NO_ELIGIBLE_DISPATCH"
    else:
        reason = "OPEN_NO_SAFE_MATERIAL__" + target + "__NO_ELIGIBLE_DISPATCH"

    _open_only(access, now, reason + "__OPEN_ONLY")
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
        if not valid_closure(receipt, catalog.get("currentParent")) or receipt.get("functionalHead") != args.functional:
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
