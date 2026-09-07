import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { plan } from './bundles.mjs';

const root = process.cwd();
const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'variapp-vaep-block2-'));
const fragments = path.join(temp, 'fragments');
fs.mkdirSync(fragments, { recursive: true });
const sha = '0123456789abcdef0123456789abcdef01234567';
const fragment = (dispatchId, stage) => ({ taskId: `N4.7.${stage}.TEST`, parentId: 'N4.7.A', worker: 'CODEX', dispatchId, baseHead: sha, resultHead: sha, status: 'SUCCESS', evidence: ['self-test evidence'], tests: ['self-test'], workflows: ['none'], artifacts: [], p0: 0, p1: 0, timestamp: '2026-09-04T00:00:00Z', blockers: [], attempt: 1, fileScope: ['vaep/**'], notes: 'test' });
fs.writeFileSync(path.join(fragments, 'one.json'), JSON.stringify(fragment('SELFTEST-DISPATCH-01', 'A')));
fs.writeFileSync(path.join(fragments, 'two.json'), JSON.stringify(fragment('SELFTEST-DISPATCH-02', 'B')));
const changelog = path.join(temp, 'CHANGELOG_AI.md');
fs.writeFileSync(changelog, 'history\n');
const aggregator = path.join(root, 'scripts', 'vaep', 'aggregate-evidence.mjs');
const run = (flag) => JSON.parse(execFileSync(process.execPath, [aggregator, '--parent', 'N4.7.A', '--fragments', fragments, '--changelog', changelog, flag], { cwd: root, encoding: 'utf8' }));
const first = run('--dry-run');
const applied = run('--apply');
const before = fs.readFileSync(changelog, 'utf8');
const second = run('--apply');
const after = fs.readFileSync(changelog, 'utf8');
const bundleAll = plan([{ id: 'A', status: 'SUCCESS' }, { id: 'B', status: 'SUCCESS' }, { id: 'C', status: 'SUCCESS' }, { id: 'D', status: 'SUCCESS' }, { id: 'E', status: 'SUCCESS' }, { id: 'F', status: 'SUCCESS' }, { id: 'G', status: 'SUCCESS' }, { id: 'H', status: 'SUCCESS' }], 'bundled');
const bundleBlocked = plan([{ id: 'A', status: 'SUCCESS' }, { id: 'B', status: 'FAILURE' }, { id: 'C', status: 'PENDING' }, { id: 'D', status: 'PENDING' }, { id: 'E', status: 'SUCCESS' }, { id: 'F', status: 'SUCCESS' }, { id: 'G', status: 'SUCCESS' }, { id: 'H', status: 'SUCCESS' }], 'bundled');
const checks = [
  ['aggregator dry-run', first.outcome === 'READY' && first.changed === true],
  ['aggregator apply', applied.outcome === 'APPLIED'],
  ['aggregator idempotent', second.outcome === 'ALREADY_AGGREGATED' && before === after],
  ['bundled has three bundles', bundleAll.bundles.length === 3],
  ['B failure holds C/D', bundleBlocked.bundles[0].gates[2].status === 'HELD_DEPENDENCY' && bundleBlocked.bundles[0].gates[3].status === 'HELD_DEPENDENCY'],
  ['legacy is available', plan([], 'legacy').bundles.every((bundle) => bundle.status === 'LEGACY_GATE_BY_GATE')]
];
fs.rmSync(temp, { recursive: true, force: true });
for (const [name, ok] of checks) console.log(`${ok ? 'PASS' : 'FAIL'} ${name}`);
if (checks.some(([, ok]) => !ok)) process.exitCode = 1;
