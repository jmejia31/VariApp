#!/usr/bin/env python3
"""Resolve Jules clarification requests from repository evidence or escalate."""
import argparse
import json
import re
from pathlib import Path


def resolve(question, task_id, scope, root="."):
    text = question.strip()
    if not text or text == "QUESTION_NOT_EXPOSED_BY_JULES_SESSION_API":
        return {"action": "QA_TAKEOVER", "reason": "question_not_exposed"}
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
