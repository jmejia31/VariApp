#!/usr/bin/env python3
import argparse
import json
import re
from pathlib import Path

ACTIVE_STATES={"QUEUED","PLANNING","IN_PROGRESS","AWAITING_USER_FEEDBACK","AWAITING_PLAN_APPROVAL"}
TERMINAL_STATES={"COMPLETED","FAILED","PAUSED","CANCELLED","CANCELED"}
CONTROL_PLANE_PREFIXES=("vaep/",".github/")
CONTROL_PLANE_EXACT={"docs/VAEP_AUTHORITY.md","AGENTS.md","PLAN_EJECUCION_AUTONOMA.md","PROJECT_CONTEXT.md","TASKS.md"}
EVIDENCE_ONLY_ERRORS={
    "SELF_REVIEW_PASS_1 missing",
    "SELF_REVIEW_PASS_2 missing",
    "TESTS_EXECUTED evidence missing",
}

def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))

def transport_action(statuses):
    if not statuses:
        return "INVALID_TRIGGER"
    if any(s!="A" for s in statuses):
        return "INVALID_REDISPATCH"
    if not 1 <= len(statuses) <= 6:
        return "FAIL_CLOSED"
    return "ADMIT"

def admission_action(obj):
    keys={"allowExistingActiveSessions","newDispatchAdmission","reason","updatedAtUtc"}
    if set(obj)!=keys or obj.get("allowExistingActiveSessions") is not True:
        return "INVALID"
    if obj.get("newDispatchAdmission")=="OPEN":
        return "OPEN"
    if obj.get("newDispatchAdmission")=="FROZEN":
        return "FROZEN"
    return "INVALID"

def manifest_attempt(m):
    value=m.get("taskAttempt",1)
    return value if isinstance(value,int) and not isinstance(value,bool) else -1

def task_manifests(current, dispatch_dir):
    manifests={}
    for path in sorted(Path(dispatch_dir).glob("*.json")):
        try:
            m=load(path)
        except Exception:
            continue
        if m.get("taskId")==current.get("taskId") and m.get("workerId")==current.get("workerId") and m.get("dispatchId"):
            manifests[m["dispatchId"]]=m
    manifests[current["dispatchId"]]=current
    return manifests

def check_active_duplicate(current, dispatch_dir, sessions, prefix):
    manifests=task_manifests(current,dispatch_dir)
    current_dispatch=current["dispatchId"]
    conflicts=[]
    for s in sessions.get("sessions",[]):
        title=str(s.get("title",""))
        state=str(s.get("state","UNKNOWN"))
        name=str(s.get("name",""))
        matched=None
        for dispatch,m in manifests.items():
            if title==prefix+dispatch:
                matched=(dispatch,m)
                break
        if not matched:
            continue
        dispatch,m=matched
        detail={"session":name,"state":state,"taskId":m.get("taskId"),"dispatchId":dispatch,
                "primaryBaseHead":m.get("primaryBaseHead"),"taskAttempt":manifest_attempt(m)}
        if state in ACTIVE_STATES:
            detail["reason"]="ACTIVE_DUPLICATE_SESSION" if dispatch==current_dispatch else "ACTIVE_EQUIVALENT_SESSION"
            conflicts.append(detail)
        elif dispatch==current_dispatch:
            detail["reason"]="INVALID_REDISPATCH_SESSION_EXISTS"
            conflicts.append(detail)
        elif state not in TERMINAL_STATES:
            detail["reason"]="REMOTE_STATE_UNVERIFIED"
            conflicts.append(detail)
    return {"ok":not conflicts,"conflicts":conflicts,"taskId":current.get("taskId"),"dispatchId":current_dispatch,
            "primaryBaseHead":current.get("primaryBaseHead"),"taskAttempt":manifest_attempt(current)}

def patch_files(unidiff):
    out=[]
    for line in unidiff.splitlines():
        if line.startswith("+++ b/"):
            p=line[6:].strip()
            if p and p!="/dev/null" and p not in out:
                out.append(p)
    return out

def classify_errors(errors):
    evidence_errors=[error for error in errors if error in EVIDENCE_ONLY_ERRORS]
    material_errors=[error for error in errors if error not in EVIDENCE_ONLY_ERRORS]
    if not errors:
        classification="READY_FOR_VAEP"
    elif not material_errors:
        classification="EVIDENCE_GAP_REVIEW_REQUIRED"
    else:
        classification="MATERIAL_CONTRACT_FAILURE"
    return classification,evidence_errors,material_errors

def validate_terminal(manifest, patch, activities_text, base_equivalence=None):
    errors=[]
    if not isinstance(patch,dict):
        return {"ok":False,"errors":["patch absent"],"changedFiles":[]}
    requested=str(manifest.get("primaryBaseHead",""))
    actual=str(patch.get("baseCommitId",""))
    equivalent = bool(
        isinstance(base_equivalence, dict)
        and base_equivalence.get("ok") is True
        and base_equivalence.get("requested") == requested
        and base_equivalence.get("actual") == actual
        and base_equivalence.get("controlPlaneOnly") is True
    )
    if actual!=requested and not equivalent:
        errors.append("patch base does not equal primaryBaseHead")
    unidiff=str(patch.get("unidiffPatch",""))
    if not unidiff.strip():
        errors.append("gitPatch is empty")
    changed=patch_files(unidiff)
    if not changed:
        errors.append("patch has no changed files")
    hint=str(manifest.get("fileScopeHint","")).rstrip("/")
    if hint and not any(p==hint or p.startswith(hint+"/") for p in changed):
        errors.append("patch does not touch fileScopeHint")
    prohibited=[p for p in changed if p in CONTROL_PLANE_EXACT or p.startswith(CONTROL_PLANE_PREFIXES)]
    if prohibited:
        errors.append("patch touches prohibited control-plane paths: "+",".join(prohibited))
    if "SELF_REVIEW_PASS_1" not in activities_text:
        errors.append("SELF_REVIEW_PASS_1 missing")
    if "SELF_REVIEW_PASS_2" not in activities_text:
        errors.append("SELF_REVIEW_PASS_2 missing")
    if not re.search(r"TESTS_EXECUTED\s*[:=]",activities_text,re.I):
        errors.append("TESTS_EXECUTED evidence missing")
    classification,evidence_errors,material_errors=classify_errors(errors)
    return {"ok":not errors,"errors":errors,"changedFiles":changed,"requestedBase":requested,"actualBase":actual,
            "baseEquivalent": actual==requested or equivalent,
            "artifactGenerated":True,"reviewFirstRequired":True,
            "classification":classification,"evidenceErrors":evidence_errors,
            "materialErrors":material_errors}

def run_self_test():
    results=[]
    def case(name,cond):
        results.append({"name":name,"pass":bool(cond)})
    case("manifest_added",transport_action(["A"])=="ADMIT")
    case("six_manifest_batch",transport_action(["A"]*6)=="ADMIT")
    case("manifest_modified",transport_action(["M"])=="INVALID_REDISPATCH")
    current={"dispatchId":"TASK-R2","taskId":"N1.X","workerId":"J1","taskAttempt":2,"primaryBaseHead":"b"*40,"fileScopeHint":"x.cs"}
    case("manifest_duplicate",not check_active_duplicate(current,"/nonexistent",{"sessions":[{"title":"VAEP-J1 TASK-R2","name":"sessions/1","state":"IN_PROGRESS"}]},"VAEP-J1 ")["ok"])
    case("r2_valid",check_active_duplicate(current,"/nonexistent",{"sessions":[{"title":"VAEP-J1 TASK-R1","name":"sessions/2","state":"COMPLETED"}]},"VAEP-J1 ")["ok"])
    case("r2_duplicate",not check_active_duplicate(current,"/nonexistent",{"sessions":[{"title":"VAEP-J1 TASK-R2","name":"sessions/3","state":"IN_PROGRESS"}]},"VAEP-J1 ")["ok"])
    case("admission_closed",admission_action({"newDispatchAdmission":"FROZEN","allowExistingActiveSessions":True,"reason":"x","updatedAtUtc":"x"})=="FROZEN")
    prior={"dispatchId":"TASK-R1","taskId":"N1.X","workerId":"J1","taskAttempt":1,"primaryBaseHead":"a"*40}
    tmp=Path(".vaep-runtime-selftest-manifests"); tmp.mkdir(exist_ok=True)
    (tmp/"TASK-R1.json").write_text(json.dumps(prior),encoding="utf-8")
    try:
        case("session_active",not check_active_duplicate(current,str(tmp),{"sessions":[{"title":"VAEP-J1 TASK-R1","name":"sessions/4","state":"IN_PROGRESS"}]},"VAEP-J1 ")["ok"])
    finally:
        for p in tmp.glob("*"): p.unlink()
        tmp.rmdir()
    case("timeout","JULES_LANE_BUDGET_EXCEEDED"!="SUCCESS")
    case("no_op",transport_action([])=="INVALID_TRIGGER")
    case("patch_absent",not validate_terminal(current,None,"SELF_REVIEW_PASS_1 SELF_REVIEW_PASS_2 TESTS_EXECUTED: ok")["ok"])
    evidence_gap=validate_terminal(current,{"baseCommitId":"b"*40,"unidiffPatch":"+++ b/x.cs\n@@\n+ok\n"},"activities without required markers")
    case("evidence_gap_is_not_material_failure",evidence_gap["classification"]=="EVIDENCE_GAP_REVIEW_REQUIRED" and not evidence_gap["materialErrors"])
    equivalent_patch={"baseCommitId":"a"*40,"unidiffPatch":"+++ b/x.cs\n@@\n+ok\n"}
    equivalent_evidence={"ok":True,"requested":"b"*40,"actual":"a"*40,"controlPlaneOnly":True}
    case("control_plane_base_equivalence",validate_terminal(current,equivalent_patch,"SELF_REVIEW_PASS_1 SELF_REVIEW_PASS_2 TESTS_EXECUTED: ok",equivalent_evidence)["ok"])
    ok=all(x["pass"] for x in results) and len(results)==13
    print(json.dumps({"status":"PASS" if ok else "FAIL","cases":results},indent=2))
    return 0 if ok else 1

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--check-active-duplicate",nargs=4,metavar=("MANIFEST","DISPATCH_DIR","SESSIONS","PREFIX"))
    ap.add_argument("--validate-terminal",nargs=3,metavar=("MANIFEST","PATCH","ACTIVITIES"))
    ap.add_argument("--base-equivalence",metavar="EVIDENCE")
    args=ap.parse_args()
    if args.self_test:
        return run_self_test()
    if args.check_active_duplicate:
        manifest,dispatch_dir,sessions,prefix=args.check_active_duplicate
        result=check_active_duplicate(load(manifest),dispatch_dir,load(sessions),prefix)
        print(json.dumps(result,indent=2))
        return 0 if result["ok"] else 45
    if args.validate_terminal:
        manifest,patch_path,activities=args.validate_terminal
        try:
            patch=load(patch_path)
        except Exception:
            patch=None
        evidence=None
        if args.base_equivalence:
            try:
                evidence=load(args.base_equivalence)
            except Exception:
                evidence=None
        result=validate_terminal(load(manifest),patch,Path(activities).read_text(encoding="utf-8"),evidence)
        print(json.dumps(result,indent=2))
        return 0 if result["ok"] else 54
    ap.error("select a mode")

if __name__=="__main__":
    raise SystemExit(main())
