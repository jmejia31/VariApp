import unittest

from go_live_gate import (
    apply_ready_notification,
    evaluate_go_live_gate,
    record_owner_authorization,
)


BASE_STATE = {
    "schemaVersion": 1,
    "gateMode": "DELIBERATE_OWNER_GATE",
    "technicalState": "NOT_READY",
    "goLiveAuthorized": False,
    "productionWriteAllowed": False,
    "gateVersion": "N8.24",
    "readyNotification": {"dedupeKey": None, "sentAtUtc": None},
    "authorizationAudit": {
        "authorizedBy": None,
        "authorizedAtUtc": None,
        "evidence": None,
    },
}

READY_PREREQUISITES = {
    "n8_23_certified": True,
    "p0_zero": True,
    "p1_zero": True,
    "staging_equivalent": True,
}


class GoLiveGateTests(unittest.TestCase):
    def test_not_ready_and_not_authorized_denies_go_live(self):
        result = evaluate_go_live_gate(
            BASE_STATE,
            {**READY_PREREQUISITES, "staging_equivalent": False},
        )
        self.assertEqual(result["technicalState"], "NOT_READY")
        self.assertEqual(result["decision"], "DENY_GO_LIVE_TECHNICAL_BLOCKER")
        self.assertEqual(result["failedPrerequisites"], ["staging_equivalent"])
        self.assertFalse(result["productionWriteAllowed"])
        self.assertFalse(result["futureExecutionAdmitted"])

    def test_ready_without_owner_authorization_is_ready_for_go_live_only(self):
        result = evaluate_go_live_gate(BASE_STATE, READY_PREREQUISITES)
        self.assertEqual(result["technicalState"], "READY_FOR_GO_LIVE")
        self.assertEqual(
            result["decision"], "DENY_GO_LIVE_PENDING_OWNER_AUTHORIZATION"
        )
        self.assertTrue(result["notificationRequired"])
        self.assertEqual(result["notificationDedupeKey"], "N8.24:READY_FOR_GO_LIVE")
        self.assertFalse(result["productionWriteAllowed"])
        self.assertFalse(result["futureExecutionAdmitted"])

    def test_owner_authorization_alone_does_not_admit_future_execution(self):
        state = {**BASE_STATE, "goLiveAuthorized": True}
        result = evaluate_go_live_gate(state, READY_PREREQUISITES)
        self.assertEqual(
            result["technicalState"], "AUTHORIZED_PENDING_EXPLICIT_EXECUTION"
        )
        self.assertEqual(
            result["decision"], "DENY_GO_LIVE_PENDING_EXPLICIT_EXECUTION"
        )
        self.assertFalse(result["productionWriteAllowed"])
        self.assertFalse(result["futureExecutionAdmitted"])

    def test_fresh_explicit_execution_authorization_is_required_for_admission(self):
        state = {**BASE_STATE, "goLiveAuthorized": True}
        result = evaluate_go_live_gate(
            state,
            READY_PREREQUISITES,
            explicit_execution_authorized=True,
        )
        self.assertEqual(
            result["technicalState"], "AUTHORIZED_FOR_FUTURE_EXECUTION"
        )
        self.assertEqual(result["decision"], "ADMIT_FUTURE_AUTHORIZED_EXECUTION")
        self.assertTrue(result["futureExecutionAdmitted"])
        self.assertFalse(result["productionWriteAllowed"])

    def test_ready_notification_is_deduplicated_by_gate_version(self):
        state = {
            **BASE_STATE,
            "readyNotification": {
                "dedupeKey": "N8.24:READY_FOR_GO_LIVE",
                "sentAtUtc": "2026-09-17T00:00:00Z",
            },
        }
        result = evaluate_go_live_gate(state, READY_PREREQUISITES)
        self.assertFalse(result["notificationRequired"])
        self.assertEqual(result["notificationDedupeKey"], "N8.24:READY_FOR_GO_LIVE")
        self.assertFalse(result["productionWriteAllowed"])

    def test_ready_notification_persistence_is_idempotent(self):
        evaluation = evaluate_go_live_gate(BASE_STATE, READY_PREREQUISITES)
        once = apply_ready_notification(
            BASE_STATE, evaluation, sent_at_utc="2026-09-17T01:00:00Z"
        )
        after = evaluate_go_live_gate(once, READY_PREREQUISITES)
        twice = apply_ready_notification(
            once, after, sent_at_utc="2026-09-17T01:05:00Z"
        )
        self.assertEqual(once, twice)
        self.assertFalse(after["notificationRequired"])
        self.assertFalse(twice["productionWriteAllowed"])

    def test_ready_notification_rejected_when_gate_is_not_ready(self):
        evaluation = evaluate_go_live_gate(
            BASE_STATE, {**READY_PREREQUISITES, "p0_zero": False}
        )
        with self.assertRaises(ValueError):
            apply_ready_notification(
                BASE_STATE, evaluation, sent_at_utc="2026-09-17T01:00:00Z"
            )

    def test_owner_authorization_requires_fresh_explicit_permission(self):
        with self.assertRaises(PermissionError):
            record_owner_authorization(
                BASE_STATE,
                authorized_by="owner",
                authorized_at_utc="2026-09-17T01:00:00Z",
                evidence="explicit approval evidence",
            )
        self.assertFalse(BASE_STATE["goLiveAuthorized"])

    def test_owner_authorization_requires_by_at_evidence_atomically(self):
        with self.assertRaises(ValueError):
            record_owner_authorization(
                BASE_STATE,
                explicit_owner_authorized=True,
                authorized_by="owner",
                authorized_at_utc="2026-09-17T01:00:00Z",
                evidence="",
            )
        self.assertFalse(BASE_STATE["goLiveAuthorized"])

    def test_explicit_owner_authorization_records_complete_audit_without_prod_write(self):
        updated = record_owner_authorization(
            BASE_STATE,
            explicit_owner_authorized=True,
            authorized_by="owner",
            authorized_at_utc="2026-09-17T01:00:00Z",
            evidence="approval://test-only",
        )
        self.assertTrue(updated["goLiveAuthorized"])
        self.assertEqual(updated["authorizationAudit"]["authorizedBy"], "owner")
        self.assertEqual(
            updated["authorizationAudit"]["authorizedAtUtc"],
            "2026-09-17T01:00:00Z",
        )
        self.assertEqual(updated["authorizationAudit"]["evidence"], "approval://test-only")
        self.assertFalse(updated["productionWriteAllowed"])
        self.assertFalse(BASE_STATE["goLiveAuthorized"])

    def test_invalid_gate_mode_fails_closed(self):
        state = {**BASE_STATE, "gateMode": "AUTO"}
        result = evaluate_go_live_gate(state, READY_PREREQUISITES)
        self.assertEqual(result["technicalState"], "NOT_READY")
        self.assertEqual(result["decision"], "DENY_GO_LIVE_INVALID_GATE_MODE")
        self.assertEqual(result["failedPrerequisites"], ["gateMode"])
        self.assertFalse(result["productionWriteAllowed"])

    def test_missing_prerequisites_fail_closed(self):
        result = evaluate_go_live_gate(BASE_STATE, {})
        self.assertEqual(result["technicalState"], "NOT_READY")
        self.assertEqual(result["decision"], "DENY_GO_LIVE_TECHNICAL_BLOCKER")
        self.assertEqual(result["failedPrerequisites"], ["prerequisites:missing"])
        self.assertFalse(result["productionWriteAllowed"])


if __name__ == "__main__":
    unittest.main()
