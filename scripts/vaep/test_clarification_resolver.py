import tempfile
import unittest
from pathlib import Path
from clarification_resolver import resolve


class ClarificationResolverTests(unittest.TestCase):
    def test_unique_route_is_answered_with_evidence(self):
        with tempfile.TemporaryDirectory() as root:
            path = Path(root) / "routes.ts"
            path.write_text("path: 'reportes-administrativos'", encoding="utf-8")
            result = resolve("Should I use /reportes-administrativos or /finanzas?", "T", "routes.ts", root)
            self.assertEqual(result["action"], "RESPOND_SPECIFIC")
            self.assertEqual(result["selectedRoute"], "/reportes-administrativos")

    def test_two_supported_candidates_escalate(self):
        with tempfile.TemporaryDirectory() as root:
            path = Path(root) / "routes.ts"
            path.write_text("/reportes-administrativos /finanzas", encoding="utf-8")
            self.assertEqual(resolve("Use /reportes-administrativos or /finanzas?", "T", "routes.ts", root)["action"], "QA_TAKEOVER")

    def test_missing_question_escalates(self):
        self.assertEqual(resolve("QUESTION_NOT_EXPOSED_BY_JULES_SESSION_API", "T", "x")["action"], "QA_TAKEOVER")
