import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const root = process.cwd();
const requiredMarker = 'PROJECT_SCOPE_LOCK=STRICT';
const expectedRepo = 'solqaryn/VariApp';

const mandatory = [
  'AGENTS.md',
  'PROJECT_CONTEXT.md',
  'PLAN_EJECUCION_AUTONOMA.md',
  'CONTRIBUTING.md',
  'docs/VAEP_AUTHORITY.md',
  'docs/COLABORATIVO.md',
  'docs/COLABORACION_IA.md',
  '.githooks/pre-commit',
  '.githooks/post-commit',
  'scripts/iniciar-sesion-ia.ps1',
  'scripts/configurar-jules-vaep.ps1',
  'scripts/configurar-colaboracion.ps1',
  'scripts/vaep/sync-bitacora.mjs',
  'docs/ROLLBACK_RUNBOOK.md',
  'docs/runbooks/GO_LIVE_MIGRATION_RUNBOOK.md',
  'docs/runbooks/HYPERCARE_RUNBOOK.md',
  'docs/runbooks/GO_LIVE_SMOKE_RUNBOOK.md',
  '.github/CODEOWNERS',
  'docs/PROJECT_SCOPE_LOCK.md',
  'docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md',
  '.agents/skills/variapp-project-governance/SKILL.md',
];

const errors = [];

for (const rel of mandatory) {
  const path = join(root, rel);
  if (!existsSync(path)) {
    errors.push(`missing required scope file: ${rel}`);
    continue;
  }
  const content = readFileSync(path, 'utf8');
  if (!content.includes(requiredMarker)) {
    errors.push(`${rel} missing ${requiredMarker}`);
  }
}

function walk(dir) {
  if (!existsSync(dir)) return [];
  const out = [];
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    if (statSync(path).isDirectory()) out.push(...walk(path));
    else out.push(path);
  }
  return out;
}

for (const path of walk(join(root, '.agents', 'skills')).filter(p => p.endsWith('SKILL.md'))) {
  const content = readFileSync(path, 'utf8');
  if (!content.includes('PROJECT_ID=VARIAPP')) errors.push(`${path} missing PROJECT_ID=VARIAPP`);
  if (!content.includes(`REPOSITORY=${expectedRepo}`)) errors.push(`${path} missing REPOSITORY=${expectedRepo}`);
  if (!content.includes(requiredMarker)) errors.push(`${path} missing ${requiredMarker}`);
  if (/skills:\/\//i.test(content)) errors.push(`${path} contains external skill URI; project skills may not chain external skills`);
}

const governanceFiles = mandatory
  .filter(rel => existsSync(join(root, rel)))
  .map(rel => ({ rel, content: readFileSync(join(root, rel), 'utf8') }));

const knownForeignProjectPatterns = [
  /cohpucp-engineering-governance/i,
  /jmejia31\/Cohpucp/i,
  /PROJECT_ID\s*=\s*COHPUCP/i,
];

for (const { rel, content } of governanceFiles) {
  for (const pattern of knownForeignProjectPatterns) {
    if (pattern.test(content)) errors.push(`${rel} contains forbidden foreign-project reference: ${pattern}`);
  }

  for (const match of content.matchAll(/PROJECT_ID\s*[:=]\s*([A-Za-z0-9_-]+)/g)) {
    if (match[1].toUpperCase() !== 'VARIAPP') {
      errors.push(`${rel} declares foreign PROJECT_ID=${match[1]}`);
    }
  }

  for (const match of content.matchAll(/REPOSITORY\s*[:=]\s*[`"']?([A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+)/g)) {
    if (match[1] !== expectedRepo) {
      errors.push(`${rel} declares foreign REPOSITORY=${match[1]}`);
    }
  }

  if (/skills:\/\//i.test(content)) {
    errors.push(`${rel} contains a skills:// URI; project governance must not depend on external skills`);
  }

  for (const match of content.matchAll(/github\.com\/([A-Za-z0-9_.-]+)\/([A-Za-z0-9_.-]+)/gi)) {
    const referencedRepo = `${match[1]}/${match[2].replace(/\.git$/, '')}`;
    if (referencedRepo !== expectedRepo) {
      errors.push(`${rel} references foreign GitHub repository ${referencedRepo}`);
    }
  }
}

if (errors.length) {
  console.error('PROJECT SCOPE GATE FAILED');
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`PROJECT SCOPE GATE OK: ${expectedRepo} / ${requiredMarker}`);
