import unittest
from state_sync import verify


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


if __name__ == '__main__':
    unittest.main()
