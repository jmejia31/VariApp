import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = process.cwd();
const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'variapp-vaep-preflight-'));
const head = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim();
const previous = execFileSync('git', ['rev-parse', 'HEAD^'], { cwd: root, encoding: 'utf8' }).trim();
const script = path.join(root, 'scripts', 'vaep', 'dispatch-preflight.mjs');
const common = { schemaVersion: '1.0', projectId: 'VARIAPP', repository: 'jmejia31/VariApp', branch: 'Desarrollo', dispatchId: 'SELFTEST-N46-ADMISSION-01', taskId: 'N4.7.A.PREFLIGHT', parentId: 'N4.7.A', phase: 'PRE', stage: 'A', primaryBaseHead: head, fileScopeHint: ['vaep/**'], worker: 'CODEX', attempt: 1, attemptConsumed: false, dependencies: [{ taskId: 'N4.6.H', status: 'SATISFIED' }], acceptanceCriteria: ['validator rejects invalid admission'], tracks: ['VAEP'], session: { sessionId: 'self-test', workerId: 'CODEX', correlationId: 'self-test' }, ownership: { owner: 'CODEX', status: 'AVAILABLE', scopes: ['vaep/**'] }, timestamps: { createdAt: '2026-09-04T00:00:00Z' } };
const write = (name, value) => { const file = path.join(temp, name); fs.writeFileSync(file, typeof value === 'string' ? value : JSON.stringify(value)); return file; };
const run = (manifest, extra = []) => {
  const file = write(`${Math.random().toString(16).slice(2)}.json`, manifest);
  try { return JSON.parse(execFileSync(process.execPath, [script, '--manifest', file, ...extra], { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] })); } catch (error) { return JSON.parse(error.stdout); }
};
const cases = [
  ['valid', run(common).outcome === 'ADMITTED'],
  ['malformed JSON', run('{').outcome === 'PRE_DISPATCH_INVALID'],
  ['wrong branch', run({ ...common, branch: 'main' }).outcome === 'PRE_DISPATCH_INVALID'],
  ['missing field', run({ ...common, taskId: undefined }).outcome === 'PRE_DISPATCH_INVALID'],
  ['unknown field', run({ ...common, unexpected: true }).outcome === 'PRE_DISPATCH_INVALID'],
  ['dependency blocked', run({ ...common, dependencies: [{ taskId: 'N4.6.H', status: 'BLOCKED' }] }).outcome === 'PRE_DISPATCH_INVALID'],
  ['duplicate dispatch', run(common, ['--registry', write('registry.json', [common.dispatchId])]).outcome === 'PRE_DISPATCH_INVALID'],
  ['invalid file scope', run({ ...common, fileScopeHint: ['../main'] }).outcome === 'PRE_DISPATCH_INVALID'],
  ['stale but refreshable', run({ ...common, dispatchId: 'SELFTEST-N46-ADMISSION-02', primaryBaseHead: previous, fileScopeHint: ['docs/non-overlap/**'] }).outcome === 'REFRESHABLE'],
  ['stale conflicting', run({ ...common, dispatchId: 'SELFTEST-N46-ADMISSION-03', primaryBaseHead: previous, fileScopeHint: ['CHANGELOG_AI.md'] }).outcome === 'FAIL_CLOSED']
];
fs.rmSync(temp, { recursive: true, force: true });
const failed = cases.filter(([, ok]) => !ok);
for (const [name, ok] of cases) console.log(`${ok ? 'PASS' : 'FAIL'} ${name}`);
if (failed.length) process.exitCode = 1;
