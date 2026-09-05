import fs from 'node:fs';
import process from 'node:process';

const FIELDS = ['dispatches', 'sessions', 'attempts', 'preDispatchRejects', 'staleRefreshes', 'materialConflicts', 'changelogConflicts', 'repairExports', 'durationAHSeconds', 'ciRuns', 'usefulWorkerTimeSeconds', 'docOnlyCommits', 'bundleSuccesses', 'bundleFailures'];
function main() {
  const file = process.argv[process.argv.indexOf('--events') + 1];
  if (!file) { console.error('usage: node scripts/vaep/metrics.mjs --events <file>'); process.exitCode = 2; return; }
  const events = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (!Array.isArray(events)) { console.error('events must be an array'); process.exitCode = 2; return; }
  const totals = Object.fromEntries(FIELDS.map((field) => [field, 0]));
  for (const event of events) for (const field of FIELDS) if (Number.isFinite(event[field])) totals[field] += event[field];
  console.log(JSON.stringify({ schemaVersion: '1.0', measuredEvents: events.length, totals }, null, 2));
}
main();
