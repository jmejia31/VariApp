import unittest

from go_live_gate import evaluate_go_live_gate


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
