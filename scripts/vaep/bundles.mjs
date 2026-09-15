import fs from 'node:fs';
import process from 'node:process';

const BUNDLES = [
  { id: 'CORE', gates: ['A', 'B', 'C', 'D'] },
  { id: 'UI_RBAC', gates: ['E', 'F'] },
  { id: 'E2E_CERT', gates: ['G', 'H'] }
];
const OK = new Set(['SUCCESS', 'PASS', 'LISTO_REAL']);

function args(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i += 1) { if (!argv[i].startsWith('--')) continue; const key = argv[i].slice(2); const next = argv[i + 1]; out[key] = next === undefined || next.startsWith('--') ? true : next; if (out[key] !== true) i += 1; }
  return out;
}

export function plan(gates, mode = process.env.VAEP_EXECUTION_MODE || 'legacy') {
  if (mode === 'legacy') return { mode, bundles: BUNDLES.map((bundle) => ({ ...bundle, status: 'LEGACY_GATE_BY_GATE' })) };
  if (mode !== 'bundled') throw new Error(`unsupported VAEP_EXECUTION_MODE: ${mode}`);
  const byId = new Map(gates.map((gate) => [gate.id, gate.status]));
  return { mode, bundles: BUNDLES.map((bundle) => {
    let blocked = false;
    const gatesInBundle = bundle.gates.map((id) => {
      const status = byId.get(id) || 'PENDING';
      const effective = blocked ? 'HELD_DEPENDENCY' : status;
      if (!OK.has(status)) blocked = true;
      return { id, status: effective };
    });
    return { id: bundle.id, gates: gatesInBundle, status: gatesInBundle.every((gate) => OK.has(gate.status)) ? 'READY' : 'HELD' };
  }) };
}

function main() {
  const opts = args(process.argv.slice(2));
  if (!opts.gates) { console.error('usage: node scripts/vaep/bundles.mjs --gates <json> [--mode legacy|bundled]'); process.exitCode = 2; return; }
  try { console.log(JSON.stringify(plan(JSON.parse(fs.readFileSync(opts.gates, 'utf8')), opts.mode), null, 2)); } catch (error) { console.error(error.message); process.exitCode = 2; }
}

if (process.argv[1]?.endsWith('bundles.mjs')) main();
