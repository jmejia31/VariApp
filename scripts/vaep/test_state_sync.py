import unittest
from state_sync import build_telemetry_clocks, verify


class StateSyncTests(unittest.TestCase):
    def setUp(self):
        self.snapshot = dict(currentParent='B', lastClosedParent='A', sourceHead='a'*40, nextParent='C')
        self.readback = dict(config=dict(CURRENT_PARENT='B', LAST_CLOSED_PARENT='A', CURRENT_HEAD='a'*40,
                                         NEXT_PARENT='C'), cola={'A': 'LISTO', 'B': 'VALIDANDO'})

    def test_consistent_observation(self):
        self.assertEqual(verify(self.snapshot, self.readback, 'a'*40), [])

    def test_concurrent_git_advance_requires_rebuild(self):
        self.assertIn('HEAD_CHANGED_REBUILD', verify(self.snapshot, self.readback, 'b'*40))

    def test_stale_queue_cannot_reopen_closure(self):
        self.readback['cola']['A'] = 'EN_PROGRESO'
        self.assertIn('CLOSED_PARENT_REGRESSION', verify(self.snapshot, self.readback, 'a'*40))

    def test_partial_sheet_write_is_not_sync(self):
        self.readback['config']['CURRENT_PARENT'] = 'A'
        self.assertIn('CONFIG_MISMATCH:CURRENT_PARENT', verify(self.snapshot, self.readback, 'a'*40))

    def test_telemetry_clocks_never_infer_scheduler_from_sync(self):
        clocks = build_telemetry_clocks(
            {'certifiedAtUtc': '2026-09-11T12:24:00Z'},
            '2026-09-11T13:00:00Z',
        )
        self.assertIsNone(clocks['lastSchedulerTriggerUtc'])
        self.assertEqual(clocks['lastMaterialActionUtc'], '2026-09-11T12:24:00Z')
        self.assertEqual(clocks['lastTelemetrySyncUtc'], '2026-09-11T13:00:00Z')
        self.assertEqual(clocks['provenance']['lastSchedulerTriggerUtc'], 'UNAVAILABLE_REPO_LOCAL')
        self.assertTrue(clocks['invariants']['telemetrySyncIsNotMaterialAction'])

    def test_explicit_runtime_clocks_stay_independent(self):
        clocks = build_telemetry_clocks(
            {'certifiedAtUtc': '2026-09-11T12:24:00Z'},
            '2026-09-11T13:00:00Z',
            scheduler_trigger_utc='2026-09-11T12:53:02Z',
            material_action_utc='2026-09-11T12:58:40Z',
        )
        self.assertEqual(clocks['lastSchedulerTriggerUtc'], '2026-09-11T12:53:02Z')
        self.assertEqual(clocks['lastMaterialActionUtc'], '2026-09-11T12:58:40Z')
        self.assertEqual(clocks['provenance']['lastMaterialActionUtc'], 'VERIFIED_RUNTIME_EXPLICIT')

    def test_optional_sheet_clock_readback_is_verified_independently(self):
        self.snapshot['telemetryClocks'] = build_telemetry_clocks(
            {'certifiedAtUtc': '2026-09-11T12:24:00Z'},
            '2026-09-11T13:00:00Z',
            scheduler_trigger_utc='2026-09-11T12:53:02Z',
        )
        self.readback['config']['LAST_SCHEDULER_TRIGGER'] = '2026-09-11T12:53:02Z'
        self.readback['config']['LAST_MATERIAL_ACTION'] = '2026-09-11T12:24:00Z'
        self.readback['config']['LAST_TELEMETRY_SYNC'] = '2026-09-11T12:59:00Z'
        errors = verify(self.snapshot, self.readback, 'a'*40)
        self.assertIn('CONFIG_MISMATCH:LAST_TELEMETRY_SYNC', errors)
        self.assertNotIn('CONFIG_MISMATCH:LAST_SCHEDULER_TRIGGER', errors)
        self.assertNotIn('CONFIG_MISMATCH:LAST_MATERIAL_ACTION', errors)


if __name__ == '__main__':
    unittest.main()
