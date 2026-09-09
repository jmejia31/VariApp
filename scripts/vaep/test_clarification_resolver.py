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

    def test_spanish_generic_plan_confirmation_is_auto_answered(self):
        result = resolve(
            "¿Les parece correcto este plan y se ajusta al enfoque previsto? ¿Debo proceder a definir estos DTO?",
            "N5.2.D.1.VALUATION_BACKEND_API",
            "backend/src/Application/ReportesInventario/Valorizacion/**",
        )
        self.assertEqual(result["action"], "RESPOND_SPECIFIC")
        self.assertEqual(result["reason"], "safe_generic_plan_confirmation_resolved_by_master_and_scope")
        self.assertIn("no additional user approval is required", result["answer"])

    def test_english_generic_plan_confirmation_is_auto_answered(self):
        result = resolve("Does this plan look correct, and should I proceed?", "T", "src/**")
        self.assertEqual(result["action"], "RESPOND_SPECIFIC")

    def test_production_confirmation_stays_fail_closed(self):
        result = resolve("Should I proceed with the Production deploy?", "T", "src/**")
        self.assertEqual(result["action"], "QA_TAKEOVER")


if __name__ == "__main__":
    unittest.main()
