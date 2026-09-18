"""Fail-closed GO_LIVE_GATE controls for VAEP N8.24.

All functions are deliberately side-effect free. They calculate decisions or proposed
control-state transitions; they never write to Production, deploy, or silently invent
owner authorization.
"""

from __future__ import annotations

from copy import deepcopy
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
    """Evaluate technical readiness and future execution admission fail-closed."""

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


def apply_ready_notification(
    state: Mapping[str, Any],
    evaluation: Mapping[str, Any],
    *,
    sent_at_utc: str,
) -> dict[str, Any]:
    """Return proposed control state after the single READY notification is sent.

    The transition is accepted only for READY_FOR_GO_LIVE while owner authorization
    is still false. Re-applying the same gate-version notification is idempotent.
    """

    if not isinstance(state, Mapping) or not isinstance(evaluation, Mapping):
        raise ValueError("state and evaluation must be mappings")
    if state.get("goLiveAuthorized") is True:
        raise ValueError("ready notification is only for pending owner authorization")
    if evaluation.get("technicalState") != READY_STATE:
        raise ValueError("gate is not technically ready")

    dedupe_key = str(evaluation.get("notificationDedupeKey") or "").strip()
    if not dedupe_key:
        raise ValueError("notification dedupe key is required")
    if not str(sent_at_utc or "").strip():
        raise ValueError("sent_at_utc is required")

    updated = deepcopy(dict(state))
    current = updated.get("readyNotification")
    if isinstance(current, Mapping) and current.get("dedupeKey") == dedupe_key:
        return updated

    updated["readyNotification"] = {
        "dedupeKey": dedupe_key,
        "sentAtUtc": sent_at_utc,
    }
    updated["technicalState"] = READY_STATE
    updated["productionWriteAllowed"] = False
    updated["updatedAtUtc"] = sent_at_utc
    return updated


def record_owner_authorization(
    state: Mapping[str, Any],
    *,
    explicit_owner_authorized: bool = False,
    authorized_by: str | None = None,
    authorized_at_utc: str | None = None,
    evidence: str | None = None,
) -> dict[str, Any]:
    """Return proposed authorization state only with fresh explicit owner evidence.

    This function does not perform or admit Production execution. BY/AT/EVIDENCE are
    mandatory and are written together with the boolean authorization to avoid an
    unaudited or partially-audited transition.
    """

    if not isinstance(state, Mapping):
        raise ValueError("state must be a mapping")
    if explicit_owner_authorized is not True:
        raise PermissionError("fresh explicit owner authorization is required")

    audit = {
        "authorizedBy": str(authorized_by or "").strip(),
        "authorizedAtUtc": str(authorized_at_utc or "").strip(),
        "evidence": str(evidence or "").strip(),
    }
    if not all(audit.values()):
        raise ValueError("authorization audit requires BY/AT/EVIDENCE")

    updated = deepcopy(dict(state))
    updated["goLiveAuthorized"] = True
    updated["productionWriteAllowed"] = False
    updated["authorizationAudit"] = audit
    updated["updatedAtUtc"] = audit["authorizedAtUtc"]
    return updated
