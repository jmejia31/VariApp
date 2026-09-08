import json
from pathlib import Path
import tempfile
import unittest

import terminal_handoff
from terminal_handoff import accepted_review_items, decision, pending_items, REVIEW_EXECUTORS


class TerminalHandoffTests(unittest.TestCase):
    def test_second_attempt_invalid_requires_takeover_not_third_session(self):
        for state in ("FAILED", "COMPLETED", "AUTO_FEEDBACK_EXHAUSTED"):
            self.assertEqual(decision(state, False, 2, 2, "sessions/1"), "QA_TAKEOVER_REQUIRED")

    def test_completed_second_attempt_still_requires_review(self):
        self.assertEqual(decision("COMPLETED", True, 2, 2, "sessions/1"), "READY_FOR_VAEP")

    def test_evidence_gap_goes_to_review_without_r2(self):
        self.assertEqual(
            decision("COMPLETED", False, 1, 2, "sessions/1", True),
            "EVIDENCE_GAP_REVIEW_REQUIRED",
        )
        self.assertEqual(
            decision("COMPLETED", False, 2, 2, "sessions/1", True),
            "QA_TAKEOVER_REQUIRED",
        )

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
        self.assertEqual(run(dict(issue, state="closed"))[0]["action"], "QA_TAKEOVER_REQUIRED")
        self.assertEqual(run(dict(issue, user={"login": "unknown"})), [])
        self.assertEqual(run(dict(issue, body=issue["body"].replace("`2/2`", "`1/2`"))), [])

    def test_timeout_issue_has_plain_fields_and_requires_takeover(self):
        manifest = {"dispatchId": "D-R2", "taskId": "N4.11.E.3.UX", "workerId": "JULES_C", "taskAttempt": 2}
        issue = {"number": 11, "state": "open", "user": {"login": "github-actions[bot]"},
                 "title": "[VAEP-JULES-SUPERSEDED] D-R2",
                 "body": "- Dispatch: D-R2\n- Task: N4.11.E.3.UX; attempt: 2/2\n- Worker: JULES_C\n- Session: sessions/1"}
        self.assertEqual(pending_items([issue], {"D-R2": manifest}, "N4.11.E", "JULES_C", set())[0]["action"], "QA_TAKEOVER_REQUIRED")

    def test_malformed_manifest_cannot_break_identity_audit(self):
        manifest = {"taskId": "N4.11.E.3.UX"}
        issue = {"number": 12, "state": "open", "user": {"login": "github-actions[bot]"},
                 "body": "- Dispatch: `D-R2`\n- Task: `N4.11.E.3.UX`\n- Worker: `JULES_C`"}
        self.assertEqual(pending_items([issue], {}, "N4.11.E", "JULES_C", set()), [])

    def test_closed_terminal_issue_remains_review_debt_until_receipt(self):
        manifest = {"dispatchId": "D", "taskId": "N4.11.G.2.SERVICE", "workerId": "JULES_B", "taskAttempt": 2}
        issue = {
            "number": 13,
            "state": "closed",
            "user": {"login": "github-actions[bot]"},
            "body": "\n".join([
                "- Dispatch: `D`",
                "- Task: `N4.11.G.2.SERVICE`",
                "- Worker: `JULES_B`",
                "- Task attempt: `2/2`",
                "- Jules session: `sessions/2`",
                "- Terminal state: `COMPLETED`",
                "- Ready for VAEP: `false`",
                "- Patch present: `true`",
            ]),
        }
        pending = pending_items([issue], {"D": manifest}, "N4.11.G", "JULES_B", set())
        self.assertEqual(pending[0]["action"], "QA_TAKEOVER_REQUIRED")
        self.assertEqual(pending[0]["authorizedReviewExecutors"], list(REVIEW_EXECUTORS))
        self.assertEqual(pending_items([issue], {"D": manifest}, "N4.11.G", "JULES_B", {"D"}), [])

    def test_issue_with_only_evidence_gap_is_review_not_r2(self):
        manifest = {"dispatchId": "E", "taskId": "N4.11.G.4.DOC", "workerId": "JULES_D", "taskAttempt": 1}
        issue = {
            "number": 14,
            "state": "open",
            "user": {"login": "github-actions[bot]"},
            "body": "\n".join([
                "- Dispatch: `E`",
                "- Task: `N4.11.G.4.DOC`",
                "- Worker: `JULES_D`",
                "- Task attempt: `1/2`",
                "- Jules session: `sessions/3`",
                "- Terminal state: `COMPLETED`",
                "- Ready for VAEP: `false`",
                "- Terminal contract classification: `EVIDENCE_GAP_REVIEW_REQUIRED`",
            ]),
        }
        pending = pending_items([issue], {"E": manifest}, "N4.11.G", "JULES_D", set())
        self.assertEqual(pending[0]["action"], "EVIDENCE_GAP_REVIEW_REQUIRED")

    def test_explicit_qa_receipt_clears_only_correlated_evidence_task(self):
        receipt = {
            "authority": "docs/VAEP_AUTHORITY.md",
            "parent": "N4.11.H",
            "review": "PASS",
            "reviewExecuted": True,
            "tasks": [
                {
                    "taskId": "N4.11.H.1.FRONTEND_DOCUMENTATION",
                    "dispatchId": "H1-R2",
                    "review": "PASS",
                    "qaTakeoverEvidenceAccepted": True,
                    "integrationRequired": False,
                },
                {
                    "taskId": "N4.11.H.2.API_DOCUMENTATION",
                    "dispatchId": "H2-R2",
                    "review": "PASS",
                    "qaTakeoverEvidenceAccepted": True,
                    "integrationRequired": True,
                },
            ],
        }
        original_dir = terminal_handoff.REVIEW_RECEIPT_DIR
        with tempfile.TemporaryDirectory() as root:
            terminal_handoff.REVIEW_RECEIPT_DIR = Path(root)
            Path(root, "review.json").write_text(json.dumps(receipt), encoding="utf-8")
            dispatches, tasks, receipts = accepted_review_items("N4.11.H")
        terminal_handoff.REVIEW_RECEIPT_DIR = original_dir

        self.assertEqual(dispatches, {"H1-R2"})
        self.assertEqual(tasks, {"N4.11.H.1.FRONTEND_DOCUMENTATION"})
        self.assertEqual(len(receipts), 1)


if __name__ == "__main__":
    unittest.main()
