import fs from 'node:fs';
import process from 'node:process';

const FIELDS = ['dispatches', 'sessions', 'attempts', 'preDispatchRejects', 'staleRefreshes', 'materialConflicts', 'changelogConflicts', 'repairExports', 'durationAHSeconds', 'ciRuns', 'usefulWorkerTimeSeconds', 'docOnlyCommits', 'bundleSuccesses', 'bundleFailures'];

export const TARGETS = {
  noOpRate: { op: 'lt', value: 0.01 },
  manifestAcceptedRate: { op: 'gt', value: 0.95 },
  sessionCreatedRate: { op: 'gt', value: 0.95 },
  timeoutRate: { op: 'lt', value: 0.05 },
  r2Rate: { op: 'lt', value: 0.10 },
  patchPresentRate: { op: 'gt', value: 0.90 },
  reviewFirstPassRate: { op: 'gt', value: 0.85 },
};

function n(event, key) {
  const value = event?.[key];
  if (value === true) return 1;
  if (value === false || value == null) return 0;
  return Number.isFinite(value) ? Number(value) : 0;
}

function ratio(num, den) {
  return den > 0 ? num / den : null;
}

function evaluate(name, value) {
  const target = TARGETS[name];
  if (value == null) return { status: 'NOT_MEASURED', value: null, ...target };
  const pass = target.op === 'lt' ? value < target.value : value > target.value;
  return { status: pass ? 'PASS' : 'BREACH', value, ...target };
}

export function computeOperational(events) {
  const c = {
    triggered: 0,
    noOp: 0,
    manifestAccepted: 0,
    sessionCreated: 0,
    sessionCompleted: 0,
    timeout: 0,
    r2: 0,
    patchPresent: 0,
    reviewAccepted: 0,
    reviewFirstPass: 0,
  };
  for (const e of events) for (const key of Object.keys(c)) c[key] += n(e, key);

  const values = {
    noOpRate: ratio(c.noOp, c.triggered),
    manifestAcceptedRate: ratio(c.manifestAccepted, c.triggered),
    sessionCreatedRate: ratio(c.sessionCreated, c.manifestAccepted),
    timeoutRate: ratio(c.timeout, c.sessionCreated),
    r2Rate: ratio(c.r2, c.sessionCreated),
    patchPresentRate: ratio(c.patchPresent, c.sessionCompleted),
    reviewFirstPassRate: ratio(c.reviewFirstPass, c.reviewAccepted),
  };
  const evaluation = Object.fromEntries(Object.entries(values).map(([k,v]) => [k, evaluate(k,v)]));
  const statuses = Object.values(evaluation).map(x => x.status);
  const status = statuses.includes('BREACH') ? 'BREACH' : (statuses.includes('NOT_MEASURED') ? 'PARTIAL' : 'PASS');
  return { counters: c, values, targets: TARGETS, evaluation, status };
}

function selfTest() {
  const good = [];
  for (let i=0;i<100;i++) {
    good.push({
      triggered: 1,
      manifestAccepted: 1,
      sessionCreated: 1,
      sessionCompleted: i < 95 ? 1 : 0,
      timeout: i === 0 ? 1 : 0,
      r2: i < 5 ? 1 : 0,
      patchPresent: i < 90 ? 1 : 0,
      reviewAccepted: i < 90 ? 1 : 0,
      reviewFirstPass: i < 80 ? 1 : 0,
      noOp: 0,
    });
  }
  const pass = computeOperational(good);
  if (pass.status !== 'PASS') throw new Error('expected PASS KPI fixture');

  const bad = Array.from({length:100}, (_,i) => ({
    triggered:1,
    manifestAccepted:i<80?1:0,
    sessionCreated:i<70?1:0,
    sessionCompleted:i<60?1:0,
    timeout:i<10?1:0,
    r2:i<20?1:0,
    patchPresent:i<40?1:0,
    reviewAccepted:i<50?1:0,
    reviewFirstPass:i<30?1:0,
    noOp:i<5?1:0,
  }));
  const breach = computeOperational(bad);
  if (breach.status !== 'BREACH') throw new Error('expected BREACH KPI fixture');
  console.log(JSON.stringify({status:'PASS',targetCount:Object.keys(TARGETS).length,passFixture:pass.values,breachFixture:breach.values},null,2));
}

function main() {
  if (process.argv.includes('--self-test')) {
    selfTest();
    return;
  }
  const idx = process.argv.indexOf('--events');
  const file = idx >= 0 ? process.argv[idx + 1] : null;
  if (!file) {
    console.error('usage: node scripts/vaep/metrics.mjs --events <file> | --self-test');
    process.exitCode = 2;
    return;
  }
  const events = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (!Array.isArray(events)) {
    console.error('events must be an array');
    process.exitCode = 2;
    return;
  }
  const totals = Object.fromEntries(FIELDS.map((field) => [field, 0]));
  for (const event of events) for (const field of FIELDS) if (Number.isFinite(event[field])) totals[field] += event[field];
  console.log(JSON.stringify({
    schemaVersion: '2.0',
    measuredEvents: events.length,
    totals,
    operational: computeOperational(events),
    productivityKpi: {
      authority: 'scripts/vaep/jules_integration_metrics.py',
      countedStage: 'INTEGRATED',
      legacyEventsCountAsProductivity: false,
      rule: 'Only validated integration commits with REVIEW_ACCEPTED and INTEGRATED receipts count as useful Jules throughput.',
    },
  }, null, 2));
}
main();
