#!/usr/bin/env python3
"""Resolve Jules clarification requests from repository evidence or escalate."""
import argparse
import json
import re
from pathlib import Path


_GENERIC_CONFIRMATION_PATTERNS = (
    r"\bshould\s+i\s+proceed\b",
    r"\bcan\s+i\s+proceed\b",
    r"\bmay\s+i\s+proceed\b",
    r"\bdoes\s+this\s+plan\b",
    r"\bis\s+this\s+plan\b",
    r"\bdo\s+you\s+agree\s+with\s+this\s+plan\b",
    r"\bdebo\s+proceder\b",
    r"\bpuedo\s+proceder\b",
    r"\bprocedo\s+con\b",
    r"\bles\s+parece\s+correcto\b",
    r"\bse\s+ajusta\s+al\s+enfoque\b",
    r"\beste\s+plan\b",
)

# These requests must remain fail-closed because answering them automatically can
# cross the assigned implementation scope or an irreversible boundary.
_FAIL_CLOSED_PATTERNS = (
    r"\bproduction\b",
    r"\bproducci[oó]n\b",
    r"\bdeploy\b",
    r"\bmerge\b",
    r"\bsecret(?:s)?\b",
    r"\bcredencial(?:es)?\b",
    r"\bdrop\s+(?:table|database)\b",
    r"\beliminar\s+(?:tabla|base\s+de\s+datos)\b",
)


def _is_safe_generic_confirmation(text):
    lowered = text.casefold()
    if any(re.search(pattern, lowered, flags=re.IGNORECASE) for pattern in _FAIL_CLOSED_PATTERNS):
        return False
    return any(re.search(pattern, lowered, flags=re.IGNORECASE) for pattern in _GENERIC_CONFIRMATION_PATTERNS)


def _generic_confirmation_answer(task_id, scope):
    return (
        f"Proceed autonomously with task {task_id}; no additional user approval is required for this plan. "
        f"Stay strictly inside the assigned FILE_SCOPE_HINT ({scope}) and direct dependencies. Prefer the "
        "existing repository service/controller/DTO architecture already used by that scope; extend it when the "
        "task requires it instead of creating a parallel architecture. If the implementation would require files "
        "outside the assigned scope, Production/deploy/merge/secrets, or an irreversible/destructive change, stop "
        "and report that concrete blocker. Otherwise continue, run proportional tests, and return the required "
        "reviewable ChangeSet with the MASTER evidence markers."
    )


def resolve(question, task_id, scope, root="."):
    text = question.strip()
    if not text or text == "QUESTION_NOT_EXPOSED_BY_JULES_SESSION_API":
        return {"action": "QA_TAKEOVER", "reason": "question_not_exposed"}

    # A generic plan/proceed confirmation is not a material ambiguity. MASTER
    # already authorizes the worker to execute inside its exclusive scope, so a
    # human round-trip here only starves the lane. Fail closed only at explicit
    # production/destructive boundaries.
    if _is_safe_generic_confirmation(text):
        return {
            "action": "RESPOND_SPECIFIC",
            "answer": _generic_confirmation_answer(task_id, scope),
            "evidence": ["docs/VAEP_AUTHORITY.md", scope],
            "reason": "safe_generic_plan_confirmation_resolved_by_master_and_scope",
        }

    base = Path(root)
    candidates = re.findall(r"(?<![A-Za-z0-9_])(/[A-Za-z0-9._/-]+)", text)
    evidence = []
    for candidate in dict.fromkeys(candidates):
        hits = []
        lookup = candidate.lstrip("/")
        for path in base.rglob("*"):
            if not path.is_file() or ".git" in path.parts or "node_modules" in path.parts:
                continue
            try:
                content = path.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            if candidate in content or lookup in content:
                hits.append(str(path).replace("\\", "/"))
        if hits:
            evidence.append((candidate, sorted(hits)[:5]))
    if len(evidence) == 1:
        route, files = evidence[0]
        return {"action": "RESPOND_SPECIFIC", "answer": (
            f"Use endpoint/route {route} for task {task_id}. It is the only requested route "
            f"supported by current repository evidence; verify the assigned scope {scope} "
            f"against these files: {', '.join(files)}. Do not use an alternative route "
            "unless a direct dependency in the assigned scope proves it."
        ), "evidence": files, "selectedRoute": route}
    return {"action": "QA_TAKEOVER", "reason": "ambiguous_or_insufficient_repository_evidence",
            "candidates": [item[0] for item in evidence] or candidates}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--question", required=True)
    parser.add_argument("--task", required=True)
    parser.add_argument("--scope", required=True)
    parser.add_argument("--root", default=".")
    args = parser.parse_args()
    print(json.dumps(resolve(args.question, args.task, args.scope, args.root)))
