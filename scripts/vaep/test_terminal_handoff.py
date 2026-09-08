import unittest
from terminal_handoff import decision, pending_items


class TerminalHandoffTests(unittest.TestCase):
    def test_second_attempt_invalid_requires_takeover_not_third_session(self):
        for state in ("FAILED", "COMPLETED", "AUTO_FEEDBACK_EXHAUSTED"):
            self.assertEqual(decision(state, False, 2, 2, "sessions/1"), "QA_TAKEOVER_REQUIRED")

    def test_completed_second_attempt_still_requires_review(self):
        self.assertEqual(decision("COMPLETED", True, 2, 2, "sessions/1"), "READY_FOR_VAEP")

    def test_pre_session_does_not_consume_content_retry(self):
        self.assertEqual(decision("FAILED", False, 2, 2, ""), "PRE_SESSION_RCA_REQUIRED")

    def test_initial_failure_requires_rca(self):
        self.assertEqual(decision("FAILED", False, 1, 2, "sessions/1"), "RCA_REQUIRED_BEFORE_R2")

    def test_late_patch_cannot_be_released_for_integration(self):
        self.assertEqual(decision("LATE_RESULT_SUPERSEDED", True, 2, 2, "sessions/1"), "LATE_RESULT_EVIDENCE_ONLY")

    def test_queue_correlates_identity_and_excludes_integrated_closed_other_parent(self):
        manifest = {"dispatchId": "D-R2", "taskId": "N4.11.E.3.UX", "workerId": "JULES_C", "taskAttempt": 2}
        issue = {"number": 10, "state": "open", "user": {"login": "github-actions[bot]"},
                 "body": "- Dispatch: `D-R2`\n- Task: `N4.11.E.3.UX`\n- Worker: `JULES_C`\n- Task attempt: `2/2`\n- Jules session: `sessions/1`\n- Terminal state: `FAILED`\n- Ready for VAEP: `false`"}
        def run(item=issue, parent="N4.11.E", integrated=set()):
            return pending_items([item], {"D-R2": manifest}, parent, "JULES_C", integrated)
        self.assertEqual(run()[0]["action"], "QA_TAKEOVER_REQUIRED")
        self.assertEqual(run(integrated={"D-R2"}), [])
        self.assertEqual(run(parent="N4.11.F"), [])
        self.assertEqual(run(dict(issue, state="closed")), [])
        self.assertEqual(run(dict(issue, user={"login": "unknown"})), [])
        self.assertEqual(run(dict(issue, body=issue["body"].replace("`2/2`", "`1/2`"))), [])

    def test_timeout_issue_has_plain_fields_and_requires_takeover(self):
        manifest = {"dispatchId": "D-R2", "taskId": "N4.11.E.3.UX", "workerId": "JULES_C", "taskAttempt": 2}
        issue = {"number": 11, "state": "open", "user": {"login": "github-actions[bot]"},
                 "title": "[VAEP-JULES-SUPERSEDED] D-R2",
                 "body": "- Dispatch: D-R2\n- Task: N4.11.E.3.UX; attempt: 2/2\n- Worker: JULES_C\n- Session: sessions/1"}
        self.assertEqual(pending_items([issue], {"D-R2": manifest}, "N4.11.E", "JULES_C", set())[0]["action"], "QA_TAKEOVER_REQUIRED")


if __name__ == "__main__":
    unittest.main()
