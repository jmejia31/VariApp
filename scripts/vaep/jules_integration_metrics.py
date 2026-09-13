#!/usr/bin/env python3
import argparse
import json
import re
import subprocess
import sys
from collections import Counter
from pathlib import Path

TRAILER_RE = re.compile(r"^VAEP-([A-Za-z0-9-]+):\s*(.*?)\s*$")
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
SESSION_RE = re.compile(r"^sessions/[0-9]+$")
ACTIVE_WORKERS = {"J1", "J2", "J3", "J4", "J5", "J6"}
LEGACY_ALIASES = {"JULES_A":"J1", "JULES_B":"J2", "JULES_C":"J3", "JULES_D":"J4"}
WORKERS = ACTIVE_WORKERS | set(LEGACY_ALIASES)
REQUIRED = {
    "Dispatch", "Task", "Worker", "Session", "Task-Attempt",
    "Dispatch-Manifest", "Patch-SHA256", "Patch-Base",
    "Review", "Review-Evidence", "Scope-Decision", "Reviewed-Files",
    "Tests", "P0", "P1", "Integrated", "Integration-Branch"
}
QUARANTINE_PATH = Path("vaep/control/jules-integration-receipt-quarantine.json")
QUARANTINE_POLICY = "INVALID_RECEIPT_NO_COUNT_FORWARD_RECONCILIATION_NO_HISTORY_REWRITE"
# A REVIEW_FIRST / QA-takeover commit may carry Dispatch/Task/Worker/Session
# metadata to identify the source Jules work without claiming to be an
# integration receipt. Canonical receipt intent is signalled by immutable
# integration fields or the exact canonical `VAEP-Integrated: TRUE` token.
# Older controller metadata used lowercase `true`; that spelling is not the
# canonical receipt token from VAEP_AUTHORITY and remains evidence-only.
# Once canonical intent exists, the complete REQUIRED contract is validated
# fail-closed unless the exact historical commit has been explicitly reviewed
# and quarantined as zero-throughput evidence without rewriting Git history.
STRONG_INTEGRATION_INTENT = {
    "Dispatch-Manifest", "Patch-SHA256", "Patch-Base", "Integration-Branch"
}

def git(*args):
    return subprocess.check_output(["git", *args], text=True, stderr=subprocess.STDOUT).strip()

def parse_trailers(message):
    trailers = {}
    duplicates = []
    for raw in message.splitlines():
        match = TRAILER_RE.match(raw.strip())
        if not match:
            continue
        key, value = match.group(1), match.group(2)
        if key in trailers:
            duplicates.append(key)
        trailers[key] = value
    return trailers, duplicates

def split_semicolon(value):
    return [item.strip() for item in value.split(";") if item.strip()]

def has_integration_receipt_intent(trailers):
    if "Dispatch" not in trailers:
        return False
    if set(trailers) & STRONG_INTEGRATION_INTENT:
        return True
    return trailers.get("Integrated") == "TRUE"

def validate_contract(trailers, duplicates, manifest, changed_files):
    errors = []
    missing = sorted(REQUIRED - set(trailers))
    if missing:
        errors.append("missing trailers: " + ", ".join(missing))
    if duplicates:
        errors.append("duplicate trailers: " + ", ".join(sorted(set(duplicates))))
    if errors:
        return errors

    if trailers["Worker"] not in WORKERS:
        errors.append("Worker must be J1..J6 (legacy A-D accepted only for historical receipts)")
    if trailers["Review"] != "ACCEPTED":
        errors.append("Review must be ACCEPTED")
    if trailers["Scope-Decision"] != "PASS":
        errors.append("Scope-Decision must be PASS")
    if trailers["Integrated"] != "TRUE":
        errors.append("Integrated must be TRUE")
    if trailers["Integration-Branch"] != "Desarrollo":
        errors.append("Integration-Branch must be Desarrollo")
    if trailers["P0"] != "0" or trailers["P1"] != "0":
        errors.append("P0 and P1 must both be 0")
    if not SHA256_RE.fullmatch(trailers["Patch-SHA256"]):
        errors.append("Patch-SHA256 must be lowercase SHA256")
    if not SHA40_RE.fullmatch(trailers["Patch-Base"]):
        errors.append("Patch-Base must be SHA40")
    if not SESSION_RE.fullmatch(trailers["Session"]):
        errors.append("Session must be sessions/<numeric-id>")
    try:
        attempt = int(trailers["Task-Attempt"])
    except ValueError:
        attempt = 0
    if attempt not in (1, 2):
        errors.append("Task-Attempt must be 1 or 2")
    if not trailers["Review-Evidence"].strip():
        errors.append("Review-Evidence must be non-empty")
    tests = trailers["Tests"].strip()
    if not tests or tests.upper() in {"NONE", "N/A", "NA"}:
        errors.append("Tests must contain real evidence or NOT_APPLICABLE:<reason>")
    reviewed_files = split_semicolon(trailers["Reviewed-Files"])
    if not reviewed_files:
        errors.append("Reviewed-Files must be non-empty")
    if len(reviewed_files) != len(set(reviewed_files)):
        errors.append("Reviewed-Files contains duplicates")
    if set(reviewed_files) != set(changed_files):
        errors.append("Reviewed-Files must exactly match integration commit changed files")
    if any(re.match(r"^(?:vaep/jules(?:-b|-c|-d)?/dispatch|vaep/j[56]/dispatch)/", path) for path in changed_files):
        errors.append("integration commit cannot add/modify Jules dispatch manifests")

    if manifest is not None:
        if manifest.get("dispatchId") != trailers["Dispatch"]:
            errors.append("Dispatch does not match manifest")
        if manifest.get("taskId") != trailers["Task"]:
            errors.append("Task does not match manifest")
        if manifest.get("workerId") != trailers["Worker"]:
            errors.append("Worker does not match manifest")
        if manifest.get("primaryBaseHead") != trailers["Patch-Base"]:
            errors.append("Patch-Base does not match manifest primaryBaseHead")
        manifest_attempt = manifest.get("taskAttempt", 1)
        if manifest_attempt != attempt:
            errors.append("Task-Attempt does not match manifest")
    return errors

def inspect_commit(sha):
    message = git("show", "-s", "--format=%B", sha)
    trailers, duplicates = parse_trailers(message)
    if not has_integration_receipt_intent(trailers):
        return {"receipt": False, "sha": sha}

    manifest_path = trailers.get("Dispatch-Manifest", "")
    manifest = None
    load_error = None
    if manifest_path:
        if not re.match(r"^(?:vaep/jules(?:-b|-c|-d)?/dispatch|vaep/j[56]/dispatch)/[^/]+\.json$", manifest_path):
            load_error = "Dispatch-Manifest path is not a Jules dispatch manifest"
        else:
            try:
                manifest = json.loads(git("show", sha + ":" + manifest_path))
            except Exception as exc:
                load_error = "cannot load Dispatch-Manifest from integration commit: " + str(exc)

    changed_text = git("diff-tree", "--no-commit-id", "--name-only", "-r", sha)
    changed_files = [line for line in changed_text.splitlines() if line.strip()]
    errors = validate_contract(trailers, duplicates, manifest, changed_files)
    if load_error:
        errors.append(load_error)
    return {"receipt": True, "sha": sha, "trailers": trailers, "changedFiles": changed_files, "errors": errors}

def validate_quarantine_payload(payload, verify_closure=True):
    errors = []
    if not isinstance(payload, dict):
        return ["quarantine root must be object"]
    if payload.get("authority") != "docs/VAEP_AUTHORITY.md":
        errors.append("quarantine authority must be docs/VAEP_AUTHORITY.md")
    if payload.get("policy") != QUARANTINE_POLICY:
        errors.append("quarantine policy mismatch")
    entries = payload.get("entries")
    if not isinstance(entries, list):
        return errors + ["quarantine entries must be array"]
    seen = set()
    for index, entry in enumerate(entries):
        prefix = f"entry[{index}]"
        if not isinstance(entry, dict):
            errors.append(prefix + " must be object")
            continue
        sha = entry.get("commit")
        parent = entry.get("parent")
        task_id = entry.get("taskId")
        closure_path = entry.get("closureReceipt")
        if not isinstance(sha, str) or not SHA40_RE.fullmatch(sha):
            errors.append(prefix + " commit must be lowercase SHA40")
        elif sha in seen:
            errors.append(prefix + " duplicate commit")
        else:
            seen.add(sha)
        if not isinstance(parent, str) or not parent:
            errors.append(prefix + " parent required")
        if not isinstance(task_id, str) or not task_id.startswith(str(parent) + "."):
            errors.append(prefix + " taskId must belong to parent")
        if entry.get("disposition") != "QUARANTINED_NO_PRODUCTIVITY":
            errors.append(prefix + " disposition mismatch")
        if entry.get("countsAsUsefulThroughput") is not False:
            errors.append(prefix + " must not count as throughput")
        if not isinstance(entry.get("reason"), str) or not entry["reason"].strip():
            errors.append(prefix + " reason required")
        if not isinstance(closure_path, str) or not re.fullmatch(r"vaep/evidence/fragments/[^/]+_LISTO_REAL_[^/]+\.json", closure_path):
            errors.append(prefix + " closureReceipt must be LISTO_REAL fragment")
        elif verify_closure:
            try:
                closure = json.loads(Path(closure_path).read_text(encoding="utf-8"))
            except (OSError, ValueError) as exc:
                errors.append(prefix + " closureReceipt unreadable: " + str(exc))
            else:
                closure_parent = closure.get("parent") or closure.get("parentId")
                if closure.get("authority") != "docs/VAEP_AUTHORITY.md" or closure_parent != parent:
                    errors.append(prefix + " closureReceipt authority/parent mismatch")
                if closure.get("state") != "LISTO_REAL" and closure.get("decision") != "LISTO_REAL":
                    errors.append(prefix + " closureReceipt is not LISTO_REAL")
                p0 = closure.get("p0", closure.get("p0Open"))
                p1 = closure.get("p1", closure.get("p1Open"))
                if p0 != 0 or p1 != 0:
                    errors.append(prefix + " closureReceipt must have P0/P1=0")
    return errors

def load_quarantine():
    if not QUARANTINE_PATH.exists():
        return {}, []
    try:
        payload = json.loads(QUARANTINE_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        return {}, ["cannot load quarantine: " + str(exc)]
    errors = validate_quarantine_payload(payload, verify_closure=True)
    if errors:
        return {}, errors
    return {entry["commit"]: entry for entry in payload["entries"]}, []

def print_valid_metrics(result):
    t = result["trailers"]
    print("VAEP_METRIC stage=REVIEW_ACCEPTED value=true worker={} dispatch={} task={} session={} integration_commit={}".format(
        t["Worker"], t["Dispatch"], t["Task"], t["Session"], result["sha"]))
    print("VAEP_METRIC stage=INTEGRATED value=true worker={} dispatch={} task={} session={} integration_commit={}".format(
        t["Worker"], t["Dispatch"], t["Task"], t["Session"], result["sha"]))

def validate_head(sha):
    result = inspect_commit(sha)
    if not result["receipt"]:
        print(json.dumps({"status":"NO_INTEGRATION_RECEIPT","commit":sha,"countsAsUsefulThroughput":False,"reason":"ordinary/review commit; no integration-receipt intent"}, indent=2))
        return 0
    if result["errors"]:
        print(json.dumps({"status":"INVALID_INTEGRATION_RECEIPT","commit":sha,"countsAsUsefulThroughput":False,"errors":result["errors"]}, indent=2))
        return 2
    print_valid_metrics(result)
    t = result["trailers"]
    print(json.dumps({"status":"INTEGRATED","commit":sha,"dispatchId":t["Dispatch"],"taskId":t["Task"],"worker":t["Worker"],"session":t["Session"],"reviewAccepted":True,"integrated":True,"countsAsUsefulThroughput":True}, indent=2))
    return 0

def read_targets():
    text = Path("docs/VAEP_AUTHORITY.md").read_text(encoding="utf-8")
    per = re.search(r"^JULES_TASKS_TARGET_ROLLING_24H_PER_WORKER=([0-9]+)$", text, re.M)
    total = re.search(r"^JULES_TASKS_TARGET_ROLLING_24H_TOTAL=([0-9]+)$", text, re.M)
    return (int(per.group(1)) if per else None, int(total.group(1)) if total else None)

def rolling(hours):
    quarantine, quarantine_errors = load_quarantine()
    if quarantine_errors:
        print(json.dumps({"status":"INVALID_QUARANTINE_CONTROL","errors":quarantine_errors,"countsAsUsefulThroughput":False}, indent=2))
        return 2

    hashes_text = git("rev-list", "--since={} hours ago".format(hours), "HEAD")
    hashes = [line for line in hashes_text.splitlines() if line.strip()]
    valid, invalid, quarantined, seen_dispatch = [], [], [], set()
    for sha in hashes:
        result = inspect_commit(sha)
        if not result["receipt"]:
            continue
        if result["errors"]:
            if sha in quarantine:
                quarantined.append({
                    "commit": sha,
                    "parent": quarantine[sha]["parent"],
                    "taskId": quarantine[sha]["taskId"],
                    "disposition": quarantine[sha]["disposition"],
                    "countsAsUsefulThroughput": False,
                    "receiptErrors": result["errors"]
                })
                continue
            invalid.append({"commit":sha,"errors":result["errors"]})
            continue
        if sha in quarantine:
            invalid.append({"commit":sha,"errors":["valid integration receipt must not be quarantined"]})
            continue
        dispatch = result["trailers"]["Dispatch"]
        if dispatch in seen_dispatch:
            invalid.append({"commit":sha,"errors":["duplicate integrated dispatchId: " + dispatch]})
            continue
        seen_dispatch.add(dispatch)
        valid.append(result)

    if invalid:
        print(json.dumps({"status":"INVALID_RECEIPTS_PRESENT","rollingHours":hours,"invalid":invalid,"quarantinedInvalidReceipts":quarantined,"countsAsUsefulThroughput":False}, indent=2))
        return 2

    by_worker = Counter(LEGACY_ALIASES.get(item["trailers"]["Worker"], item["trailers"]["Worker"]) for item in valid)
    per_target, total_target = read_targets()
    per_deficit = {}
    if hours == 24 and per_target is not None:
        per_deficit = {worker:max(0, per_target-by_worker.get(worker,0)) for worker in sorted(ACTIVE_WORKERS)}
    payload = {
        "status":"OK",
        "rollingHours":hours,
        "productivityAuthority":"VALIDATED_INTEGRATED_COMMITS_ONLY",
        "integratedTasks":len(valid),
        "byWorker":{worker:by_worker.get(worker,0) for worker in sorted(ACTIVE_WORKERS)},
        "perWorkerTarget24h":per_target if hours == 24 else None,
        "totalTarget24h":total_target if hours == 24 else None,
        "deficitByWorker":per_deficit,
        "totalDeficit":max(0,total_target-len(valid)) if hours == 24 and total_target is not None else None,
        "quarantinedInvalidReceipts":quarantined,
        "excludedFromProductivity":["dispatch commits","manifests","autorefill","reservations","workflow success without session","SESSION_COMPLETED without validated integration","REVIEW_FIRST/QA takeover metadata without canonical integration-receipt intent","explicitly reviewed invalid historical receipts quarantined with LISTO_REAL evidence and zero throughput"],
        "integrations":[{"commit":item["sha"],"dispatchId":item["trailers"]["Dispatch"],"taskId":item["trailers"]["Task"],"worker":item["trailers"]["Worker"],"session":item["trailers"]["Session"]} for item in valid]
    }
    print(json.dumps(payload, indent=2))
    return 0

def self_test():
    manifest = {"dispatchId":"N4-11-B-1-ENTITY-A","taskId":"N4.11.B.1.ENTITY_INVARIANTS","workerId":"JULES_A","taskAttempt":1,"primaryBaseHead":"a"*40}
    message = "\n".join([
        "feat(n4.11.b): integrate centro costo entity","",
        "VAEP-Dispatch: N4-11-B-1-ENTITY-A",
        "VAEP-Task: N4.11.B.1.ENTITY_INVARIANTS",
        "VAEP-Worker: JULES_A",
        "VAEP-Session: sessions/123456",
        "VAEP-Task-Attempt: 1",
        "VAEP-Dispatch-Manifest: vaep/jules/dispatch/N4-11-B-1-ENTITY-A.json",
        "VAEP-Patch-SHA256: " + "b"*64,
        "VAEP-Patch-Base: " + "a"*40,
        "VAEP-Review: ACCEPTED",
        "VAEP-Review-Evidence: artifact=vaep-jules-123;review=PASS",
        "VAEP-Scope-Decision: PASS",
        "VAEP-Reviewed-Files: backend/src/Domain/Entities/Contabilidad/CentroCosto.cs",
        "VAEP-Tests: dotnet test --filter CentroCosto",
        "VAEP-P0: 0","VAEP-P1: 0",
        "VAEP-Integrated: TRUE",
        "VAEP-Integration-Branch: Desarrollo"
    ])
    trailers, dup = parse_trailers(message)
    errors = validate_contract(trailers, dup, manifest, ["backend/src/Domain/Entities/Contabilidad/CentroCosto.cs"])
    if errors:
        print("SELFTEST_VALID_RECEIPT_FAILED=" + json.dumps(errors))
        return 1
    if not has_integration_receipt_intent(trailers):
        print("SELFTEST_VALID_RECEIPT_INTENT_MISSED")
        return 1
    bad = dict(trailers)
    bad["Review"] = "REQUIRED"
    if not validate_contract(bad, [], manifest, ["backend/src/Domain/Entities/Contabilidad/CentroCosto.cs"]):
        print("SELFTEST_INVALID_REVIEW_ACCEPTED")
        return 1
    review_only = {
        "Dispatch":"N5-2-B-1-DOMAIN-J2",
        "Task":"N5.2.B.1.INVENTORY_REPORT_DOMAIN_CONTRACTS",
        "Worker":"J2",
        "Session":"sessions/123456",
        "Review":"PASS",
        "Takeover":"CHATGPT_VAEP",
        "Artifact":"10110597206"
    }
    if has_integration_receipt_intent(review_only):
        print("SELFTEST_REVIEW_SESSION_METADATA_MISCLASSIFIED_AS_RECEIPT")
        return 1
    legacy_controller_metadata = dict(review_only)
    legacy_controller_metadata["Integrated"] = "true"
    if has_integration_receipt_intent(legacy_controller_metadata):
        print("SELFTEST_LOWERCASE_CONTROLLER_METADATA_MISCLASSIFIED_AS_RECEIPT")
        return 1
    malformed_intent = dict(review_only)
    malformed_intent["Integrated"] = "TRUE"
    if not has_integration_receipt_intent(malformed_intent):
        print("SELFTEST_MALFORMED_RECEIPT_INTENT_MISSED")
        return 1
    if not validate_contract(malformed_intent, [], None, ["docs/example.md"]):
        print("SELFTEST_MALFORMED_RECEIPT_NOT_REJECTED")
        return 1
    quarantine_fixture = {
        "authority": "docs/VAEP_AUTHORITY.md",
        "policy": QUARANTINE_POLICY,
        "entries": [{
            "commit": "c" * 40,
            "parent": "N5.4.E",
            "taskId": "N5.4.E.1.TEST",
            "disposition": "QUARANTINED_NO_PRODUCTIVITY",
            "countsAsUsefulThroughput": False,
            "closureReceipt": "vaep/evidence/fragments/N5.4.E_LISTO_REAL_TEST.json",
            "reason": "fixture"
        }]
    }
    quarantine_errors = validate_quarantine_payload(quarantine_fixture, verify_closure=False)
    if quarantine_errors:
        print("SELFTEST_QUARANTINE_VALID_FAILED=" + json.dumps(quarantine_errors))
        return 1
    quarantine_fixture["entries"][0]["countsAsUsefulThroughput"] = True
    if not validate_quarantine_payload(quarantine_fixture, verify_closure=False):
        print("SELFTEST_QUARANTINE_THROUGHPUT_OVERRIDE_ACCEPTED")
        return 1
    print("JULES_INTEGRATION_RECEIPT_SELFTEST=PASS")
    print("PRODUCTIVITY_KPI_INTEGRATED_ONLY=PASS")
    print("REVIEW_METADATA_EXCLUDED_FROM_INTEGRATION_RECEIPTS=PASS")
    print("LOWERCASE_LEGACY_CONTROLLER_METADATA_EXCLUDED=PASS")
    print("INVALID_RECEIPT_QUARANTINE_NO_COUNT=PASS")
    return 0

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--validate-head")
    parser.add_argument("--rolling-hours", type=int)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    if args.validate_head:
        return validate_head(args.validate_head)
    if args.rolling_hours:
        if args.rolling_hours < 1:
            print("rolling hours must be positive", file=sys.stderr)
            return 2
        return rolling(args.rolling_hours)
    parser.error("use --validate-head SHA, --rolling-hours N, or --self-test")

if __name__ == "__main__":
    sys.exit(main())
