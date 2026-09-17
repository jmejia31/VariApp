"""Fail-closed GO_LIVE_GATE readiness and admission evaluation for VAEP N8.24.

This module is deliberately side-effect free. It calculates gate decisions but never
writes to Production, performs deployments, or persists owner authorization.
"""

from __future__ import annotations

from typing import Any, Mapping


REQUIRED_GATE_MODE = "DELIBERATE_OWNER_GATE"
READY_STATE = "READY_FOR_GO_LIVE"


def _base_result(*, technical_state: str, decision: str) -> dict[str, Any]:
    return {
        "technicalState": technical_state,
        "decision": decision,
        "failedPrerequisites": [],
        "productionWriteAllowed": False,
        "notificationRequired": False,
        "notificationDedupeKey": None,
        "futureExecutionAdmitted": False,
    }


def evaluate_go_live_gate(
    state: Mapping[str, Any],
    prerequisites: Mapping[str, bool],
    *,
    explicit_execution_authorized: bool = False,
) -> dict[str, Any]:
    """Evaluate technical readiness and future execution admission fail-closed.

    Owner authorization is accepted only when ``goLiveAuthorized`` is the literal
    boolean ``True``. A future execution additionally needs an explicit execution
    authorization for that execution. Even then, this evaluator reports admission
    only; ``productionWriteAllowed`` remains ``False`` because this module has no
    Production write capability.
    """

    if not isinstance(state, Mapping):
        result = _base_result(
            technical_state="NOT_READY",
            decision="DENY_GO_LIVE_INVALID_STATE",
        )
        result["failedPrerequisites"] = ["state:invalid"]
        return result

    if state.get("gateMode") != REQUIRED_GATE_MODE:
        result = _base_result(
            technical_state="NOT_READY",
            decision="DENY_GO_LIVE_INVALID_GATE_MODE",
        )
        result["failedPrerequisites"] = ["gateMode"]
        return result

    gate_version = str(state.get("gateVersion") or "").strip()
    if not gate_version:
        result = _base_result(
            technical_state="NOT_READY",
            decision="DENY_GO_LIVE_INVALID_STATE",
        )
        result["failedPrerequisites"] = ["gateVersion"]
        return result

    if not isinstance(prerequisites, Mapping) or not prerequisites:
        result = _base_result(
            technical_state="NOT_READY",
            decision="DENY_GO_LIVE_TECHNICAL_BLOCKER",
        )
        result["failedPrerequisites"] = ["prerequisites:missing"]
        return result

    failed = sorted(
        str(name)
        for name, satisfied in prerequisites.items()
        if satisfied is not True
    )
    if failed:
        result = _base_result(
            technical_state="NOT_READY",
            decision="DENY_GO_LIVE_TECHNICAL_BLOCKER",
        )
        result["failedPrerequisites"] = failed
        return result

    dedupe_key = f"{gate_version}:{READY_STATE}"
    ready_notification = state.get("readyNotification")
    previously_notified = (
        isinstance(ready_notification, Mapping)
        and ready_notification.get("dedupeKey") == dedupe_key
    )

    owner_authorized = state.get("goLiveAuthorized") is True
    if not owner_authorized:
        result = _base_result(
            technical_state=READY_STATE,
            decision="DENY_GO_LIVE_PENDING_OWNER_AUTHORIZATION",
        )
        result["notificationRequired"] = not previously_notified
        result["notificationDedupeKey"] = dedupe_key
        return result

    if explicit_execution_authorized is not True:
        result = _base_result(
            technical_state="AUTHORIZED_PENDING_EXPLICIT_EXECUTION",
            decision="DENY_GO_LIVE_PENDING_EXPLICIT_EXECUTION",
        )
        result["notificationDedupeKey"] = dedupe_key
        return result

    result = _base_result(
        technical_state="AUTHORIZED_FOR_FUTURE_EXECUTION",
        decision="ADMIT_FUTURE_AUTHORIZED_EXECUTION",
    )
    result["notificationDedupeKey"] = dedupe_key
    result["futureExecutionAdmitted"] = True
    return result
