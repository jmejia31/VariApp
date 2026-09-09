#!/usr/bin/env python3
"""Classify terminal work without creating sessions or claiming a review occurred."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess

REVIEW_EXECUTORS = ("CHATGPT_VAEP", "CHATGPT_BUSINESS")
REVIEW_RECEIPT_DIR = Path("vaep/evidence/reviews")


def review_executor_metadata():
    """Return authorized reviewers without claiming either one executed review."""
    return {
        "authorizedReviewExecutors": list(REVIEW_EXECUTORS),
        "reviewExecutionRequired": True,
        "reviewExecuted": False,
    }


def decision(state, ready, attempt, maximum, session, evidence_gap_only=False):
    if state == "LATE_RESULT_SUPERSEDED":
        return "LATE_RESULT_EVIDENCE_ONLY"
    if not session:
        return "PRE_SESSION_RCA_REQUIRED"
    if ready and state == "COMPLETED":
        return "READY_FOR_VAEP"
    if evidence_gap_only and state == "COMPLETED":
        return "QA_TAKEOVER_REQUIRED" if attempt >= maximum else "EVIDENCE_GAP_REVIEW_REQUIRED"
    if attempt >= maximum or "QA_TAKEOVER" in state:
        return "QA_TAKEOVER_REQUIRED"
    return "RCA_REQUIRED_BEFORE_R2"


def annotate(directory):
    path = Path(directory)
    result = json.loads((path / "result.json").read_text(encoding="utf-8"))
    contract = json.loads((path / "terminal-contract.json").read_text(encoding="utf-8"))
    evidence_gap_only = contract.get("classification") == "EVIDENCE_GAP_REVIEW_REQUIRED"
    handoff = decision(result["state"], result["readyForVaep"], result["taskAttempt"],
                       result["maxAttempts"], result.get("session"), evidence_gap_only)
    result.update(controllerHandoff=handoff, correctionOwner="CHATGPT_VAEP",
                  takeoverExecuted=False, retryBudgetExhausted=result["taskAttempt"] >= result["maxAttempts"],
                  rejectionReasons=contract.get("errors", [contract.get("reason")] if contract.get("reason") else []),
                  terminalClassification=contract.get("classification", "NOT_EVALUATED"),
                  evidenceGapOnly=evidence_gap_only,
                  nextLaneAction="REFILL_ONLY_DEPENDENCY_SAFE_NON_OVERLAPPING_SCOPE")
    result.update(review_executor_metadata())
    (path / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    lifecycle = json.loads((path / "lifecycle.json").read_text(encoding="utf-8"))
    lifecycle.update(controllerHandoff=handoff, correctionOwner="CHATGPT_VAEP", takeoverExecuted=False,
                     terminalClassification=contract.get("classification", "NOT_EVALUATED"),
                     evidenceGapOnly=evidence_gap_only)
    lifecycle.update(review_executor_metadata())
    (path / "lifecycle.json").write_text(json.dumps(lifecycle, indent=2) + "\n", encoding="utf-8")
    print(handoff)


def field(body, name):
    match = re.search(r"^- " + re.escape(name) + r": (.+)$", body, re.M)
    if not match:
        return ""
    value = match.group(1).strip()
    return value.split("`")[1] if value.startswith("`") else value.split(";")[0].strip()


def _receipt_dispatch(item):
    dispatch = item.get("dispatchId") or item.get("latestDispatchId")
    return dispatch if isinstance(dispatch, str) else ""


def _receipt_scope(item):
    evidence_path = item.get("evidencePath")
    scope = item.get("fileScopeHint") or evidence_path
    if not isinstance(evidence_path, str) or not isinstance(scope, str):
        return "", ""
    return evidence_path, scope


def _receipt_integration_not_required(item):
    """Accept canonical field or the older direct-takeover receipt spelling.

    Compatibility never turns a Jules integration claim into review evidence:
    the legacy form is accepted only when the item explicitly says the Jules
    patch was not integrated and QA takeover evidence was accepted.
    """
    if "integrationRequired" in item:
        return item.get("integrationRequired") is False
    return item.get("julesPatchIntegrated") is False


def accepted_review_items(parent):
    """Load only explicit, task-correlated QA receipts."""
    reviewed_dispatches = set()
    reviewed_tasks = set()
    receipts = []
    if not REVIEW_RECEIPT_DIR.is_dir():
        return reviewed_dispatches, reviewed_tasks, receipts

    for path in sorted(REVIEW_RECEIPT_DIR.glob("*.json")):
        try:
            receipt = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            continue
        if not isinstance(receipt, dict):
            continue
        if (receipt.get("authority") != "docs/VAEP_AUTHORITY.md"
                or receipt.get("parent") != parent
                or receipt.get("review") != "PASS"
                or receipt.get("reviewExecuted") is not True):
            continue

        accepted = 0
        for item in receipt.get("tasks", []):
            if not isinstance(item, dict):
                continue
            if (item.get("review") != "PASS"
                    or item.get("qaTakeoverEvidenceAccepted") is not True
                    or not _receipt_integration_not_required(item)):
                continue
            evidence_path, scope = _receipt_scope(item)
            if (not evidence_path
                    or evidence_path != scope
                    or not Path(scope).is_file()):
                continue
            dispatch = _receipt_dispatch(item)
            task = item.get("taskId")
            if not dispatch:
                continue
            if not isinstance(task, str) or not task.startswith(parent + "."):
                continue
            reviewed_dispatches.add(dispatch)
            reviewed_tasks.add(task)
            accepted += 1
        if accepted:
            receipts.append(str(path))
    return reviewed_dispatches, reviewed_tasks, receipts


def pending_items(issues, manifests, parent, worker, integrated,
                  reviewed_dispatches=None, reviewed_tasks=None):
    reviewed_dispatches = reviewed_dispatches or set()
    reviewed_tasks = reviewed_tasks or set()
    pending = {}
    for issue in issues:
        if issue.get("user", {}).get("login") != "github-actions[bot]":
            continue
        body = issue.get("body") or ""
        dispatch = field(body, "Dispatch")
        manifest = manifests.get(dispatch)
        if not manifest or dispatch in integrated:
            continue
        if manifest.get("workerId") != worker or not manifest.get("taskId", "").startswith(parent + "."):
            continue
        if field(body, "Task") != manifest["taskId"] or field(body, "Worker") != worker:
            continue
        if dispatch in reviewed_dispatches or manifest["taskId"] in reviewed_tasks:
            continue
        state = field(body, "Terminal state")
        session = field(body, "Jules session") or field(body, "Session")
        evidence_gap_only = field(body, "Terminal contract classification") == "EVIDENCE_GAP_REVIEW_REQUIRED"
        attempt_text = field(body, "Task attempt")
        if issue.get("title") == "[VAEP-JULES-SUPERSEDED] " + dispatch:
            state = "SUPERSEDED_QA_TAKEOVER"
            attempt_match = re.search(r"attempt: ([0-9]+/[0-9]+)", body)
            attempt_text = attempt_match.group(1) if attempt_match else ""
        if not re.fullmatch(r"[1-9][0-9]*/[1-9][0-9]*", attempt_text):
            continue
        attempt, maximum = map(int, attempt_text.split("/"))
        if attempt != manifest.get("taskAttempt", 1):
            continue
        action = decision(state, field(body, "Ready for VAEP") == "true", attempt, maximum, session, evidence_gap_only)
        if action not in {"READY_FOR_VAEP", "EVIDENCE_GAP_REVIEW_REQUIRED", "QA_TAKEOVER_REQUIRED", "RCA_REQUIRED_BEFORE_R2"}:
            continue
        item = dict(dispatchId=dispatch, taskId=manifest["taskId"], workerId=worker,
                    issue=issue["number"], session=session, taskAttempt=attempt,
                    action=action, correctionOwner="CHATGPT_VAEP",
                    authorizedReviewExecutors=list(REVIEW_EXECUTORS),
                    takeoverExecuted=False)
        previous = pending.get(manifest["taskId"])
        if previous is None or (attempt, item["issue"]) > (previous["taskAttempt"], previous["issue"]):
            pending[manifest["taskId"]] = item
    return sorted(pending.values(), key=lambda item: item["issue"])


def audit(worker, output):
    from jules_integration_metrics import inspect_commit
    repo = os.environ["GITHUB_REPOSITORY"]
    raw = subprocess.check_output(["gh", "api", "--paginate", "--slurp",
        f"repos/{repo}/issues?state=all&per_page=100"], text=True, encoding="utf-8")
    issues = [issue for page in json.loads(raw) for issue in page]
    catalog = json.loads(Path("vaep/control/jules-autorefill-catalog.json").read_text(encoding="utf-8"))
    manifests = {}
    malformed_manifests = []
    historical_malformed_manifests = []
    current_parent = catalog["currentParent"]
    for directory in ("jules", "jules-b", "jules-c", "jules-d"):
        for path in Path(f"vaep/{directory}/dispatch").glob("*.json"):
            m = json.loads(path.read_text(encoding="utf-8"))
            dispatch_id = m.get("dispatchId")
            if not dispatch_id:
                if str(m.get("taskId", "")).startswith(current_parent + "."):
                    malformed_manifests.append(str(path))
                else:
                    historical_malformed_manifests.append(str(path))
                continue
            manifests[dispatch_id] = m
    hashes = subprocess.check_output(["git", "log", "--format=%H", "--grep=^VAEP-Dispatch:", "HEAD"], text=True)
    integrated = set()
    for sha in hashes.splitlines():
        receipt = inspect_commit(sha)
        if receipt["receipt"] and not receipt["errors"]:
            integrated.add(receipt["trailers"]["Dispatch"])
    reviewed_dispatches, reviewed_tasks, review_receipts = accepted_review_items(catalog["currentParent"])
    items = pending_items(issues, manifests, catalog["currentParent"], worker, integrated,
                          reviewed_dispatches, reviewed_tasks)
    report = dict(currentParent=current_parent, workerId=worker, pending=items,
                  malformedManifests=malformed_manifests,
                  historicalMalformedManifests=historical_malformed_manifests,
                  status="REVIEW_EXECUTOR_NOT_CONFIGURED" if items else "NO_PENDING_TERMINAL_REVIEW",
                  reviewExecuted=bool(reviewed_tasks), integrationExecuted=False,
                  reviewExecutionRequired=bool(items),
                  reviewReceipts=review_receipts,
                  reviewedTaskCount=len(reviewed_tasks),
                  authorizedReviewExecutors=list(REVIEW_EXECUTORS))
    Path(output).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))
    if items:
        print("::warning::REVIEW_EXECUTOR_NOT_CONFIGURED: terminal work requires VAEP review/QA; see handoff artifact.")
    if malformed_manifests:
        print(f"::warning::MALFORMED_MANIFESTS_IGNORED: {len(malformed_manifests)} manifest(s) without dispatchId.")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--annotate")
    parser.add_argument("--audit", choices=["J1", "J2", "J3", "J4", "J5", "J6"])
    parser.add_argument("--output", default="terminal-handoff.json")
    args = parser.parse_args()
    if args.annotate:
        annotate(args.annotate)
    elif args.audit:
        raise SystemExit(audit(args.audit, args.output))
    else:
        parser.error("--annotate or --audit required")
