import fs from 'node:fs';
import process from 'node:process';

function parseArgs(argv) { const result = {}; for (let i = 0; i < argv.length; i += 1) { if (!argv[i].startsWith('--')) continue; const key = argv[i].slice(2); const next = argv[i + 1]; result[key] = next === undefined || next.startsWith('--') ? true : next; if (result[key] !== true) i += 1; } return result; }
function main() {
  const opts = parseArgs(process.argv.slice(2));
  if (!opts.input) { console.error('usage: node scripts/vaep/reconcile-status.mjs --input <file>'); process.exitCode = 2; return; }
  const input = JSON.parse(fs.readFileSync(opts.input, 'utf8'));
  const reasons = [];
  const required = ['parentId', 'currentHead', 'gates', 'p0', 'p1', 'documentationPresent', 'ownershipConflict', 'dependenciesSatisfied'];
  for (const field of required) if (!(field in input)) reasons.push(`missing ${field}`);
  const allGatesGreen = Array.isArray(input.gates) && input.gates.length > 0 && input.gates.every((gate) => ['SUCCESS', 'PASS', 'LISTO_REAL'].includes(gate.status) && gate.head === input.currentHead);
  if (!allGatesGreen) reasons.push('required gates are not terminal SUCCESS at exact current head');
  if (input.p0 !== 0 || input.p1 !== 0) reasons.push('P0/P1 are not zero');
  if (input.documentationPresent !== true) reasons.push('required documentation is absent');
  if (input.ownershipConflict !== false) reasons.push('ownership conflict exists');
  if (input.dependenciesSatisfied !== true) reasons.push('dependency is not satisfied');
  console.log(JSON.stringify({ outcome: reasons.length ? 'PROMOTION_HELD' : 'ELIGIBLE_FOR_CONTROLLER_REVIEW', autoPromote: false, controllerMustDecide: true, reasons }, null, 2));
  if (reasons.length) process.exitCode = 1;
}
main();
