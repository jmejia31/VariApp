import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { execFileSync } from 'node:child_process';

const ROOT = process.cwd();
const schemaPath = path.join(ROOT, 'vaep', 'schemas', 'jules-dispatch.schema.json');
const REQUIRED = ['schemaVersion', 'projectId', 'repository', 'branch', 'dispatchId', 'taskId', 'parentId', 'phase', 'stage', 'primaryBaseHead', 'fileScopeHint', 'worker', 'attempt', 'attemptConsumed', 'dependencies', 'acceptanceCriteria', 'tracks', 'session', 'ownership', 'timestamps'];

function args(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i += 1) {
    if (!argv[i].startsWith('--')) continue;
    const key = argv[i].slice(2);
    out[key] = argv[i + 1]?.startsWith('--') ? true : argv[i + 1];
    if (out[key] !== true) i += 1;
  }
  return out;
}

function git(...values) {
  return execFileSync('git', values, { cwd: ROOT, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
}

function fail(errors, code = 'PRE_DISPATCH_INVALID') {
  return { outcome: code, attemptStarts: false, attemptConsumed: false, errors };
}

function globToRegExp(glob) {
  const escaped = glob.split('*').map((part) => part.replace(/[.+?^${}()|[\]\\]/g, '\\$&')).join('.*');
  return new RegExp(`^${escaped}$`);
}

function matches(scope, file) {
  return globToRegExp(scope).test(file);
}

function validateShape(manifest) {
  const errors = [];
  if (!manifest || typeof manifest !== 'object' || Array.isArray(manifest)) return ['manifest must be an object'];
  for (const key of REQUIRED) if (!(key in manifest)) errors.push(`missing required field: ${key}`);
  if (errors.length) return errors;
  if (manifest.schemaVersion !== '1.0') errors.push('schemaVersion must be 1.0');
  if (manifest.projectId !== 'VARIAPP') errors.push('projectId must be VARIAPP');
  if (manifest.repository !== 'jmejia31/VariApp') errors.push('repository must be jmejia31/VariApp');
  if (manifest.branch !== 'Desarrollo') errors.push('branch must be Desarrollo');
  if (!/^[A-Za-z0-9][A-Za-z0-9._:-]{7,127}$/.test(manifest.dispatchId)) errors.push('dispatchId is invalid');
  if (!/^N\d+\.\d+\.[A-H](\.[A-Za-z0-9._-]+)*$/.test(manifest.taskId)) errors.push('taskId is invalid');
  if (!/^N\d+\.\d+\.[A-H]$/.test(manifest.parentId)) errors.push('parentId is invalid');
  if (!/^[0-9a-f]{40}$/.test(manifest.primaryBaseHead)) errors.push('primaryBaseHead must be a full lowercase SHA');
  if (!Array.isArray(manifest.fileScopeHint) || manifest.fileScopeHint.length === 0) errors.push('fileScopeHint must be a non-empty array');
  for (const scope of manifest.fileScopeHint ?? []) {
    if (typeof scope !== 'string' || scope.startsWith('/') || scope.includes('..') || /(^|\/)(main|Produccion)(\/|$)/i.test(scope)) errors.push(`invalid protected file scope: ${scope}`);
  }
  if (!Number.isInteger(manifest.attempt) || manifest.attempt < 1 || manifest.attempt > 2) errors.push('attempt must be 1 or 2');
  if (manifest.attemptConsumed !== false) errors.push('attemptConsumed must be false before admission');
  if (!Array.isArray(manifest.acceptanceCriteria) || manifest.acceptanceCriteria.length === 0) errors.push('acceptanceCriteria must be non-empty');
  if (!Array.isArray(manifest.tracks) || manifest.tracks.length === 0) errors.push('tracks must be non-empty');
  if (!manifest.session?.sessionId || !manifest.session?.workerId || !manifest.session?.correlationId) errors.push('session metadata is incomplete');
  if (!['AVAILABLE', 'RELEASED'].includes(manifest.ownership?.status)) errors.push('ownership must be AVAILABLE or RELEASED');
  if (!manifest.ownership?.owner || !Array.isArray(manifest.ownership?.scopes) || !manifest.ownership.scopes.length) errors.push('ownership metadata is incomplete');
  if (!manifest.timestamps?.createdAt || Number.isNaN(Date.parse(manifest.timestamps.createdAt))) errors.push('timestamps.createdAt must be ISO date-time');
  for (const dependency of manifest.dependencies ?? []) if (dependency.status !== 'SATISFIED') errors.push(`dependency blocked: ${dependency.taskId}`);
  return errors;
}

function readJson(file, label) {
  try { return JSON.parse(fs.readFileSync(file, 'utf8')); } catch (error) { throw new Error(`${label}: ${error.message}`); }
}

function main() {
  const options = args(process.argv.slice(2));
  if (!options.manifest) { console.error('usage: node scripts/vaep/dispatch-preflight.mjs --manifest <file> [--head <sha>] [--registry <file>] [--ownership-file <file>]'); process.exitCode = 2; return; }
  let manifest;
  try { manifest = readJson(path.resolve(options.manifest), 'manifest JSON invalid'); } catch (error) { console.log(JSON.stringify(fail([error.message]))); process.exitCode = 2; return; }
  const shapeErrors = validateShape(manifest);
  if (shapeErrors.length) { console.log(JSON.stringify(fail(shapeErrors))); process.exitCode = 2; return; }
  if (!fs.existsSync(schemaPath)) { console.log(JSON.stringify(fail(['formal schema not found']))); process.exitCode = 2; return; }
  const head = options.head || git('rev-parse', 'HEAD');
  const errors = [];
  try { git('cat-file', '-e', `${manifest.primaryBaseHead}^{commit}`); } catch { errors.push(`primaryBaseHead does not exist: ${manifest.primaryBaseHead}`); }
  let stale = false;
  let changed = [];
  if (!errors.length) {
    try { git('merge-base', '--is-ancestor', manifest.primaryBaseHead, head); } catch { errors.push('primaryBaseHead is not an ancestor of current HEAD'); }
    stale = manifest.primaryBaseHead !== head;
    if (stale) {
      try { changed = git('diff', '--name-only', `${manifest.primaryBaseHead}..${head}`).split(/\r?\n/).filter(Boolean); } catch (error) { errors.push(`cannot inspect ancestry delta: ${error.message}`); }
    }
  }
  if (options.registry) {
    try {
      const registry = readJson(path.resolve(options.registry), 'dispatch registry JSON invalid');
      const ids = Array.isArray(registry) ? registry : registry.dispatchIds;
      if (ids?.includes(manifest.dispatchId)) errors.push(`duplicate dispatchId: ${manifest.dispatchId}`);
    } catch (error) { errors.push(error.message); }
  }
  if (options['ownership-file']) {
    try {
      const owners = readJson(path.resolve(options['ownership-file']), 'ownership JSON invalid');
      for (const owner of (Array.isArray(owners) ? owners : owners.owners ?? [])) {
        if (owner.status !== 'ACTIVE' || owner.owner === manifest.worker) continue;
        const overlap = (owner.scopes ?? []).filter((owned) => (manifest.fileScopeHint ?? []).some((scope) => matches(scope, owned) || matches(owned, scope)));
        if (overlap.length) errors.push(`active ownership conflict by ${owner.owner}: ${overlap.join(', ')}`);
      }
    } catch (error) { errors.push(error.message); }
  }
  if (errors.length) { console.log(JSON.stringify(fail(errors, stale && errors.every((e) => e.startsWith('primaryBaseHead')) ? 'FAIL_CLOSED' : 'PRE_DISPATCH_INVALID'))); process.exitCode = 2; return; }
  const conflicts = changed.filter((file) => manifest.fileScopeHint.some((scope) => matches(scope, file)));
  if (stale && conflicts.length) { console.log(JSON.stringify(fail([`stale base has material scope conflicts: ${conflicts.join(', ')}`], 'FAIL_CLOSED'))); process.exitCode = 2; return; }
  if (stale) { console.log(JSON.stringify({ outcome: 'REFRESHABLE', attemptStarts: false, attemptConsumed: false, refreshRequired: true, changedFiles: changed })); return; }
  console.log(JSON.stringify({ outcome: 'ADMITTED', attemptStarts: true, attemptConsumed: false, refreshRequired: false, changedFiles: [] }));
}

main();
