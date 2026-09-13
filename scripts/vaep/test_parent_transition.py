import copy
import json
from pathlib import Path
import tempfile
import unittest

from parent_transition import MASTER, choose_next, transition, valid_closure, read_closures, roadmap_nodes


def receipt(parent, **changes):
    data = dict(authority=MASTER, parent=parent, decision="LISTO_REAL", functionalHead="a" * 40,
                review="PASS", combinedStatus="SUCCESS", causalGates="TERMINAL_SUCCESS_APPLICABLE",
                p0Open=0, p1Open=0, productionTouched=False, mergePerformed=False)
    data.update(changes)
    return data


def catalog(current="N4.11.H"):
    return {"currentParent": current, "roadmap": {"authority": MASTER, "sourceSpreadsheetId": "test",
        "nodes": [
            dict(id="N4.11.H", order=50, type="MICROTAREA", dependencies=[], phaseGate=""),
            dict(id="GATE-N4", order=51, type="GATE_FASE", dependencies=["N4.11.H"]),
            dict(id="N5.1.A", order=52, type="MICROTAREA", dependencies=["GATE-N4"], phaseGate="GATE-N4"),
            dict(id="N5.1.B", order=53, type="MICROTAREA", dependencies=["N5.1.A"], phaseGate="GATE-N4")
        ]}, "lanes": {"JULES_A": [dict(taskId="N5.1.A.1", plannedParent="N5.1.A",
            readyForDispatch=True, fileScopeHint="docs/preflight.md", prompt="Inspect real code", dispatchEligible=False)]}}


def access(reason="OPEN_ONLY_SELFTEST"):
    return dict(newDispatchAdmission="OPEN", allowExistingActiveSessions=True, reason=reason, updatedAtUtc="before")


class ParentTransitionTests(unittest.TestCase):
    def test_phase_gate_selected_before_n5(self):
        self.assertEqual(choose_next(catalog(), {"N4.11.H"}), "GATE-N4")

    def test_n5_blocked_without_gate(self):
        c = catalog("GATE-N4")
        with self.assertRaisesRegex(ValueError, "CURRENT_PARENT_NOT_CERTIFIED"):
            choose_next(c, {"N4.11.H"})

    def test_n5_selected_after_gate(self):
        self.assertEqual(choose_next(catalog("GATE-N4"), {"N4.11.H", "GATE-N4"}), "N5.1.A")

    def test_all_gate_dependencies_required(self):
        c = catalog()
        c["roadmap"]["nodes"][1]["dependencies"].append("N4.6.H")
        with self.assertRaisesRegex(ValueError, "DEPENDENCIES_NOT_CLOSED:N4.6.H"):
            choose_next(c, {"N4.11.H"})

    def test_numeric_boundary_not_compared_as_text(self):
        c = catalog("N4.9.H")
        c["roadmap"]["nodes"] = [
            dict(id="N4.9.H", order=9, type="MICROTAREA", dependencies=[], phaseGate=""),
            dict(id="N4.10.A", order=10, type="MICROTAREA", dependencies=["N4.9.H"], phaseGate="")]
        self.assertEqual(choose_next(c, {"N4.9.H"}), "N4.10.A")

    def test_later_task_cannot_skip_blocked_gate(self):
        c = catalog()
        c["roadmap"]["nodes"][1]["dependencies"].append("N4.6.H")
        c["roadmap"]["nodes"][2]["dependencies"] = ["N4.11.H"]
        with self.assertRaises(ValueError):
            choose_next(c, {"N4.11.H"})

    def test_missing_roadmap_fails_closed(self):
        with self.assertRaisesRegex(ValueError, "CANONICAL_ROADMAP_MISSING"):
            choose_next({"currentParent": "N4.11.H", "lanes": {}}, {"N4.11.H"})

    def test_unknown_current_fails_closed(self):
        with self.assertRaisesRegex(ValueError, "CURRENT_PARENT_NOT_IN_ROADMAP"):
            choose_next(catalog("UNKNOWN"), {"UNKNOWN"})

    def test_duplicate_id_rejected(self):
        c = catalog()
        c["roadmap"]["nodes"].append(copy.deepcopy(c["roadmap"]["nodes"][0]))
        with self.assertRaises(ValueError):
            roadmap_nodes(c)

    def test_duplicate_order_rejected(self):
        c = catalog()
        c["roadmap"]["nodes"][1]["order"] = 50
        with self.assertRaises(ValueError):
            roadmap_nodes(c)

    def test_forward_dependency_rejected(self):
        c = catalog()
        c["roadmap"]["nodes"][0]["dependencies"] = ["N5.1.B"]
        with self.assertRaisesRegex(ValueError, "DEPENDENCY_ORDER_OR_CYCLE"):
            roadmap_nodes(c)

    def test_bool_order_rejected(self):
        c = catalog()
        c["roadmap"]["nodes"][0]["order"] = True
        with self.assertRaises(ValueError):
            roadmap_nodes(c)

    def test_missing_phase_gate_field_rejected(self):
        c = catalog()
        del c["roadmap"]["nodes"][2]["phaseGate"]
        with self.assertRaisesRegex(ValueError, "PHASE_GATE_REQUIRED"):
            roadmap_nodes(c)

    def test_unclosed_phase_gate_blocks_even_if_not_in_dependencies(self):
        c = catalog("N5.1.A")
        with self.assertRaisesRegex(ValueError, "DEPENDENCIES_NOT_CLOSED:GATE-N4"):
            choose_next(c, {"N5.1.A"})

    def test_valid_receipt(self):
        self.assertTrue(valid_closure(receipt("P"), "P"))

    def test_receipt_rejects_false_certification(self):
        for change in [dict(p0Open=1), dict(p1Open=True), dict(review="PENDING"),
                       dict(combinedStatus="FAILURE"), dict(productionTouched=True),
                       dict(functionalHead="short"), dict(authority="other"), dict(mergePerformed=True)]:
            with self.subTest(change=change):
                self.assertFalse(valid_closure(receipt("P", **change), "P"))

    def test_read_closures_ignores_bad_json(self):
        with tempfile.TemporaryDirectory() as root:
            Path(root, "P_LISTO_REAL_1.json").write_text(json.dumps(receipt("P")))
            Path(root, "Q_LISTO_REAL_1.json").write_text("invalid")
            Path(root, "R_LISTO_REAL_1.json").write_text(json.dumps(receipt("R", p0Open=1)))
            self.assertEqual(read_closures(root), {"P"})

    def test_gate_promotion_keeps_global_open_but_never_dispatches_jules(self):
        c, a = transition(catalog(), access(), {"N4.11.H"}, "r.json", "a"*40, "now", True)
        self.assertEqual(c["currentParent"], "GATE-N4")
        self.assertEqual(a["newDispatchAdmission"], "OPEN")
        self.assertTrue(a["allowExistingActiveSessions"])
        self.assertFalse(c["lanes"]["JULES_A"][0]["dispatchEligible"])
        self.assertIn("OPEN_PHASE_GATE_REQUIRED", a["reason"])
        self.assertEqual(c["throughputPlan"]["currentParent"], "GATE-N4")

    def test_promotion_after_hardening_and_material_work(self):
        c, a = transition(catalog("GATE-N4"), access(), {"GATE-N4"}, "r.json", "a"*40, "now", True)
        self.assertEqual(c["currentParent"], "N5.1.A")
        self.assertEqual(a["newDispatchAdmission"], "OPEN")
        self.assertTrue(c["lanes"]["JULES_A"][0]["dispatchEligible"])
        self.assertEqual(c["lanes"]["JULES_A"][0]["reason"], "CURRENT_PARENT__DEPENDENCIES_CLOSED__MATERIAL_SCOPE")
        self.assertEqual(c["throughputPlan"]["currentParent"], "N5.1.A")
        self.assertEqual(c["throughputPlan"]["currentParentMaterialScopeCount"], 1)
        self.assertEqual(c["throughputPlan"]["nextParent"], "N5.1.B")
        self.assertIn("N5.1.A is current", c["roadmap"]["note"])

    def test_hardening_not_passed_keeps_open_and_disables_task(self):
        c, a = transition(catalog("GATE-N4"), access(), {"GATE-N4"}, "r.json", "a"*40, "now")
        self.assertEqual(a["newDispatchAdmission"], "OPEN")
        self.assertFalse(c["lanes"]["JULES_A"][0]["dispatchEligible"])
        self.assertIn("OPEN_HARDENING_REQUIRED", a["reason"])

    def test_non_open_global_states_are_rejected(self):
        for invalid_state in ("FROZEN", "CLOSED", "UNKNOWN"):
            a = access()
            a["newDispatchAdmission"] = invalid_state
            with self.subTest(state=invalid_state), self.assertRaisesRegex(ValueError, "INVALID_ADMISSION_CONTRACT"):
                transition(catalog(), a, {"N4.11.H"}, "r.json", "a"*40, "now", True)

    def test_allow_existing_active_sessions_must_be_true(self):
        a = access()
        a["allowExistingActiveSessions"] = False
        with self.assertRaisesRegex(ValueError, "INVALID_ADMISSION_CONTRACT"):
            transition(catalog(), a, {"N4.11.H"}, "r.json", "a"*40, "now", True)

    def test_no_material_work_keeps_open_without_fabricating_work(self):
        c = catalog("GATE-N4")
        c["lanes"]["JULES_A"][0]["readyForDispatch"] = False
        c2, a = transition(c, access(), {"GATE-N4"}, "r.json", "a"*40, "now", True)
        self.assertEqual(a["newDispatchAdmission"], "OPEN")
        self.assertFalse(c2["lanes"]["JULES_A"][0]["dispatchEligible"])
        self.assertIn("OPEN_NO_SAFE_MATERIAL", a["reason"])

    def test_closure_retained_when_no_successor_and_admission_stays_open(self):
        c = catalog("N5.1.B")
        c2, a = transition(c, access(), {"N5.1.B"}, "real-receipt.json", "a"*40, "now", True)
        self.assertEqual(c2["lastClosedParent"], "N5.1.B")
        self.assertEqual(c2["closureReceipts"]["N5.1.B"], "real-receipt.json")
        self.assertEqual(a["newDispatchAdmission"], "OPEN")
        self.assertEqual(c2["throughputPlan"]["currentParent"], "N5.1.B")
        self.assertEqual(c2["throughputPlan"]["currentParentMaterialScopeCount"], 0)
        self.assertIsNone(c2["throughputPlan"]["nextParent"])
        self.assertIn("OPEN_NO_SUCCESSOR", a["reason"])

    def test_open_input_remains_open_when_hardening_blocks(self):
        c2, a2 = transition(catalog("GATE-N4"), access("already-open"), {"GATE-N4"}, "r.json", "a"*40, "now", False)
        self.assertEqual(a2["newDispatchAdmission"], "OPEN")
        self.assertFalse(c2["lanes"]["JULES_A"][0]["dispatchEligible"])

    def test_inputs_are_not_mutated(self):
        c, a = catalog(), access()
        before = copy.deepcopy((c, a))
        transition(c, a, {"N4.11.H"}, "r.json", "a"*40, "now", True)
        self.assertEqual((c, a), before)


if __name__ == "__main__":
    unittest.main()
