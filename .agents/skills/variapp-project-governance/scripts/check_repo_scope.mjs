import { readFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

const expectedRepo = 'solqaryn/VariApp';
const expectedBranch = 'Desarrollo';

function git(...args) {
  return execFileSync('git', args, { encoding: 'utf8' }).trim();
}

const branch = git('branch', '--show-current');
if (branch !== expectedBranch) {
  throw new Error(`Scope gate: branch ${branch} != ${expectedBranch}`);
}

const origin = git('remote', 'get-url', 'origin');
const allowedOrigins = new Set([
  'https://github.com/solqaryn/VariApp',
  'https://github.com/solqaryn/VariApp.git',
  'git@github.com:solqaryn/VariApp.git',
  'ssh://git@github.com/solqaryn/VariApp.git',
]);
if (!allowedOrigins.has(origin)) {
  throw new Error(`Scope gate: origin ${origin} no corresponde a ${expectedRepo}`);
}

for (const path of ['AGENTS.md', 'docs/PROJECT_SCOPE_LOCK.md']) {
  if (!existsSync(path)) throw new Error(`Scope gate: falta ${path}`);
  const content = readFileSync(path, 'utf8');
  if (!content.includes('PROJECT_SCOPE_LOCK=STRICT')) {
    throw new Error(`Scope gate: ${path} no declara PROJECT_SCOPE_LOCK=STRICT`);
  }
}

console.log(`Scope OK: ${expectedRepo} / ${expectedBranch}`);
