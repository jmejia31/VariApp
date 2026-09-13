"""Execute the actual YAML guard offline; never dispatch closed certifications."""
import os
from pathlib import Path
import subprocess
import unittest
import yaml

ROOT = Path(__file__).resolve().parents[2]


class AuthorizationTests(unittest.TestCase):
    def test_both_workflows_fail_closed(self):
        for filename in ['fase8-validacion-completa.yml', 'm13-certificacion-final.yml']:
            workflow = yaml.load((ROOT / '.github/workflows' / filename).read_text(), Loader=yaml.BaseLoader)
            self.assertEqual(set(workflow['on']), {'workflow_dispatch'})
            self.assertEqual(workflow['on']['workflow_dispatch']['inputs']['authorization']['required'], 'true')
            jobs = workflow['jobs']
            for name, job in jobs.items():
                if name == 'authorize':
                    continue
                needs = job['needs']
                self.assertIn('authorize', needs if isinstance(needs, list) else [needs])
            if 'certify' in jobs:
                self.assertIn("needs.authorize.result == 'success'", jobs['certify']['if'])
            script = jobs['authorize']['steps'][0]['run']
            base = dict(EVENT_NAME='workflow_dispatch', REPOSITORY='jmejia31/VariApp', OWNER='jmejia31',
                        ACTOR='jmejia31', TRIGGERING_ACTOR='jmejia31', TARGET_REF='refs/heads/Desarrollo',
                        AUTHORIZATION='AUTORIZADO_REABRIR')
            for changes, allowed in [({}, True), ({'AUTHORIZATION': ''}, False),
                                     ({'AUTHORIZATION': 'AUTORIZADO_REABRIR\n'}, False),
                                     ({'ACTOR': 'collaborator'}, False),
                                     ({'TRIGGERING_ACTOR': 'collaborator'}, False),
                                     ({'TARGET_REF': 'refs/heads/main'}, False),
                                     ({'EVENT_NAME': 'push'}, False),
                                     ({'REPOSITORY': 'other/VariApp'}, False),
                                     ({'AUTHORIZATION': '$(exit 0)'}, False)]:
                with self.subTest(workflow=filename, changes=changes):
                    result = subprocess.run(['bash', '-c', script], env={**os.environ, **base, **changes},
                                            capture_output=True)
                    self.assertEqual(result.returncode == 0, allowed)


if __name__ == '__main__':
    unittest.main()
