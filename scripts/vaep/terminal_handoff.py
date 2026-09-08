#!/usr/bin/env python3
"""Classify terminal work without creating sessions or claiming a review occurred."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess


def decision(state, ready, attempt, maximum, session, evidence_gap_only=False):
    if state == "LATE_RESULT_SUPERSEDED":
        return "LATE_RESULT_EVIDENCE_ONLY"
    if not session:
        return "PRE_SESSION_RCA_REQUIRED"
    if ready and state == "COMPLETED":
        return "READY_FOR_VAEP"
    if evidence_gap_only and state == "COMPLETED":
        # The patch/base/scope contract passed; only Jules' evidence markers
        # are missing. This is a review/QA handoff, never a content retry.
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
    (path / "result.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    lifecycle = json.loads((path / "lifecycle.json").read_text(encoding="utf-8"))
    lifecycle.update(controllerHandoff=handoff, correctionOwner="CHATGPT_VAEP", takeoverExecuted=False,
                     terminalClassification=contract.get("classification", "NOT_EVALUATED"),
                     evidenceGapOnly=evidence_gap_only)
    (path / "lifecycle.json").write_text(json.dumps(lifecycle, indent=2) + "\n", encoding="utf-8")
    print(handoff)


def field(body, name):
    match = re.search(r"^- " + re.escape(name) + r": (.+)$", body, re.M)
    if not match:
        return ""
    value = match.group(1).strip()
    return value.split("`")[1] if value.startswith("`") else value.split(";")[0].strip()


def pending_items(issues, manifests, parent, worker, integrated):
    pending = {}
    for issue in issues:
        # A terminal Jules issue may be closed administratively before VAEP
        # review. Closing the Issue does not satisfy REVIEW_FIRST and must not
        # erase the handoff from the durable review queue.
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
                                 action=action, correctionOwner="CHATGPT_VAEP", takeoverExecuted=False)
        previous = pending.get(manifest["taskId"])
        if previous is None or (attempt, item["issue"]) > (previous["taskAttempt"], previous["issue"]):
            pending[manifest["taskId"]] = item
    return sorted(pending.values(), key=lambda item: item["issue"])


def audit(worker, output):
    from jules_integration_metrics import inspect_commit
    repo = os.environ["GITHUB_REPOSITORY"]
    # Terminal result Issues can be closed administratively before REVIEW_FIRST.
    # Query all states so closure never erases an unresolved handoff.
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
                    # Old manifests remain visible for forensic traceability,
                    # but must not turn every current-parent checkpoint into a
                    # warning or a false operational failure.
                    historical_malformed_manifests.append(str(path))
                continue
            manifests[dispatch_id] = m
    # Only validated receipts resolve integration debt. Open Issues alone are
    # not a reason to ask for a second review of already integrated work.
    hashes = subprocess.check_output(["git", "log", "--format=%H", "--grep=^VAEP-Dispatch:", "HEAD"], text=True)
    integrated = set()
    for sha in hashes.splitlines():
        receipt = inspect_commit(sha)
        if receipt["receipt"] and not receipt["errors"]:
            integrated.add(receipt["trailers"]["Dispatch"])
    items = pending_items(issues, manifests, catalog["currentParent"], worker, integrated)
    report = dict(currentParent=current_parent, workerId=worker, pending=items,
                  malformedManifests=malformed_manifests,
                  historicalMalformedManifests=historical_malformed_manifests,
                  status="REVIEW_EXECUTOR_NOT_CONFIGURED" if items else "NO_PENDING_TERMINAL_REVIEW",
                  reviewExecuted=False, integrationExecuted=False)
    Path(output).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))
    if items:
        # Pending review is an expected business state, not a scheduler
        # failure. Keep it visible and fail-closed: this audit never refills
        # or integrates work.
        print("::warning::REVIEW_EXECUTOR_NOT_CONFIGURED: terminal work requires VAEP review/QA; see handoff artifact.")
    if malformed_manifests:
        print(f"::warning::MALFORMED_MANIFESTS_IGNORED: {len(malformed_manifests)} manifest(s) without dispatchId.")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--annotate")
    parser.add_argument("--audit", choices=["JULES_A", "JULES_B", "JULES_C", "JULES_D"])
    parser.add_argument("--output", default="terminal-handoff.json")
    args = parser.parse_args()
    if args.annotate:
        annotate(args.annotate)
    elif args.audit:
        raise SystemExit(audit(args.audit, args.output))
    else:
        parser.error("--annotate or --audit required")
