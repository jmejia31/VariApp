import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const REQUIRED = ['taskId', 'parentId', 'worker', 'dispatchId', 'baseHead', 'resultHead', 'status', 'evidence', 'tests', 'workflows', 'artifacts', 'p0', 'p1', 'timestamp', 'blockers', 'attempt', 'fileScope', 'notes'];
const PASS = new Set(['SUCCESS', 'PASS', 'LISTO_REAL']);

function options(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i += 1) {
    if (!argv[i].startsWith('--')) continue;
    const key = argv[i].slice(2);
    const next = argv[i + 1];
    out[key] = next === undefined || next.startsWith('--') ? true : next;
    if (out[key] !== true) i += 1;
  }
  return out;
}

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, 'utf8'));
}

function stable(value) {
  if (Array.isArray(value)) return value.map(stable);
  if (value && typeof value === 'object') return Object.fromEntries(Object.keys(value).sort().map((key) => [key, stable(value[key])]));
  return value;
}

function validate(fragment, file) {
  const errors = [];
  for (const field of REQUIRED) if (!(field in fragment)) errors.push(`${file}: missing ${field}`);
  if (errors.length) return errors;
  if (!/^N\d+\.\d+\.[A-H]$/.test(fragment.parentId)) errors.push(`${file}: invalid parentId`);
  if (!/^[0-9a-f]{40}$/.test(fragment.baseHead) || !/^[0-9a-f]{40}$/.test(fragment.resultHead)) errors.push(`${file}: invalid commit SHA`);
  if (!PASS.has(fragment.status) && !['FAILURE', 'BLOCKED', 'PENDING'].includes(fragment.status)) errors.push(`${file}: invalid status`);
  if (!Number.isInteger(fragment.p0) || fragment.p0 < 0 || !Number.isInteger(fragment.p1) || fragment.p1 < 0) errors.push(`${file}: p0/p1 must be non-negative integers`);
  if (Number.isNaN(Date.parse(fragment.timestamp))) errors.push(`${file}: invalid timestamp`);
  if (!Number.isInteger(fragment.attempt) || fragment.attempt < 1 || fragment.attempt > 2) errors.push(`${file}: invalid attempt`);
  for (const field of ['evidence', 'tests', 'workflows', 'artifacts', 'blockers', 'fileScope']) if (!Array.isArray(fragment[field])) errors.push(`${file}: ${field} must be an array`);
  return errors;
}

function findFragments(directory) {
  if (!fs.existsSync(directory)) return [];
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const target = path.join(directory, entry.name);
    if (entry.isDirectory()) return findFragments(target);
    return entry.name.endsWith('.json') ? [target] : [];
  });
}

function build(parentId, fragments) {
  const ordered = [...fragments].sort((a, b) => a.dispatchId.localeCompare(b.dispatchId));
  const digest = crypto.createHash('sha256').update(JSON.stringify(stable(ordered))).digest('hex');
  const lines = [
    `<!-- VAEP_AGGREGATION parent=${parentId} digest=${digest} -->`,
    `### VAEP evidence aggregation — ${parentId}`,
    '',
    `- Fragments: ${ordered.length}`,
    `- Result heads: ${[...new Set(ordered.map((item) => item.resultHead))].join(', ')}`,
    '- Controller decision: REQUIRED; this block does not set LISTO_REAL automatically.',
    '- P0/P1: all fragments report zero.',
    ''
  ];
  for (const item of ordered) lines.push(`- ${item.dispatchId} (${item.worker}): ${item.status}; tests=${item.tests.join('; ') || 'none'}; workflows=${item.workflows.join('; ') || 'none'}`);
  lines.push('', `<!-- /VAEP_AGGREGATION parent=${parentId} -->`, '');
  return { digest, block: lines.join('\n') };
}

function main() {
  const opts = options(process.argv.slice(2));
  const mode = opts.apply ? 'apply' : opts['dry-run'] ? 'dry-run' : 'check';
  if (!opts.parent || !opts.fragments || !opts.changelog) { console.error('usage: --parent N --fragments DIR --changelog FILE [--check|--dry-run|--apply]'); process.exitCode = 2; return; }
  const files = findFragments(path.resolve(opts.fragments));
  const fragments = [];
  const errors = [];
  for (const file of files) {
    try {
      const fragment = readJson(file);
      errors.push(...validate(fragment, file));
      if (fragment.parentId !== opts.parent) errors.push(`${file}: parentId does not match ${opts.parent}`);
      fragments.push(fragment);
    } catch (error) { errors.push(`${file}: invalid JSON (${error.message})`); }
  }
  const dispatches = new Set();
  for (const item of fragments) {
    if (dispatches.has(item.dispatchId)) errors.push(`duplicate dispatchId: ${item.dispatchId}`);
    dispatches.add(item.dispatchId);
    if (!PASS.has(item.status)) errors.push(`${item.dispatchId}: status ${item.status} is not terminal success`);
    if (item.p0 !== 0 || item.p1 !== 0) errors.push(`${item.dispatchId}: P0/P1 must both be zero`);
    if (!item.evidence.length) errors.push(`${item.dispatchId}: evidence is empty`);
  }
  if (!fragments.length) errors.push('no evidence fragments found');
  if (errors.length) { console.log(JSON.stringify({ outcome: 'ABORTED', errors }, null, 2)); process.exitCode = 2; return; }
  const result = build(opts.parent, fragments);
  const target = path.resolve(opts.changelog);
  const existing = fs.existsSync(target) ? fs.readFileSync(target, 'utf8') : '';
  const marker = `VAEP_AGGREGATION parent=${opts.parent}`;
  const same = existing.includes(`VAEP_AGGREGATION parent=${opts.parent} digest=${result.digest}`);
  if (existing.includes(marker) && !same) { console.log(JSON.stringify({ outcome: 'ABORTED', errors: [`existing aggregation for ${opts.parent} has a different digest`] }, null, 2)); process.exitCode = 2; return; }
  const next = same ? existing : `${existing}${existing.endsWith('\n') || !existing ? '' : '\n'}${result.block}`;
  if (mode === 'apply' && !same) {
    const temp = `${target}.tmp-${process.pid}-${Date.now()}`;
    try { fs.writeFileSync(temp, next, 'utf8'); fs.renameSync(temp, target); } finally { if (fs.existsSync(temp)) fs.rmSync(temp); }
  }
  console.log(JSON.stringify({ outcome: same ? 'ALREADY_AGGREGATED' : mode === 'apply' ? 'APPLIED' : 'READY', digest: result.digest, fragments: fragments.length, changed: !same }, null, 2));
}

main();
