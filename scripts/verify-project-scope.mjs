import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const root = process.cwd();
const expectedRepo = 'solqaryn/Solqaryn';
const expectedProjectId = 'SOLQARYN';
const requiredMarker = 'PROJECT_SCOPE_LOCK=STRICT';
const onlyLocalSkill = '.agents/skills/solqaryn-project-governance/SKILL.md';
const registryPath = 'docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md';
const allowlistPath = 'docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md';

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
  allowlistPath,
  registryPath,
  onlyLocalSkill,
];

const expectedExternalSources = [
  ['agentskills/agentskills', '69ef37e9424c0a7ea9dd2293b559e43ec8176379'],
  ['Skill Creator oficial de ChatGPT / OpenAI', 'Integrado en el entorno'],
  ['pbakaus/impeccable', '2149fcce39a90bb409df5f16515f316a76dc6199'],
  ['emilkowalski/skills', 'd23d7f88a2e21c9e4b1418c7abe420f5c1052ba7'],
  ['Leonxlnx/taste-skill', 'ccbc15639c97057cbfcf32ecebc38ef716e4bb37'],
  ['blader/humanizer', '9862685f575c65a8247f90369951df1b3416e3d6'],
  ['blader/napkin', '27fa60a895de4383b26a539136bc983155cb979c'],
  ['alexgreensh/token-optimizer', '37a9546b9fecba2c4e9a02ef4e90855d449bf08f'],
  ['JuliusBrussee/caveman', '15581d14007fd01fb3f132016741962f34936ca2'],
];

const errors = [];

function read(rel) {
  return readFileSync(join(root, rel), 'utf8');
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

for (const rel of mandatory) {
  if (!existsSync(join(root, rel))) {
    errors.push(`missing required SOLQARYN file: ${rel}`);
  }
}

const lockRequired = mandatory.filter(rel =>
  ![registryPath].includes(rel) && existsSync(join(root, rel))
);
for (const rel of lockRequired) {
  const content = read(rel);
  if (!content.includes(requiredMarker)) {
    errors.push(`${rel} missing ${requiredMarker}`);
  }
}

const skillFiles = walk(join(root, '.agents', 'skills'))
  .filter(path => path.endsWith('SKILL.md'))
  .map(path => relative(root, path).split(sep).join('/'))
  .sort();

if (skillFiles.length !== 1) {
  errors.push(`SOLQARYN must contain exactly one local SKILL.md; found ${skillFiles.length}`);
}
if (skillFiles[0] !== onlyLocalSkill) {
  errors.push(`the only local skill must be ${onlyLocalSkill}`);
}

if (existsSync(join(root, onlyLocalSkill))) {
  const content = read(onlyLocalSkill);
  const nameMatch = content.match(/^name:\s*([^\n]+)$/m);
  const name = nameMatch?.[1]?.trim().replace(/^["']|["']$/g, '') ?? '';
  if (name !== 'solqaryn-project-governance') errors.push('local skill name must be solqaryn-project-governance');
  if (!content.includes(`PROJECT_ID=${expectedProjectId}`)) errors.push('local skill missing canonical PROJECT_ID');
  if (!content.includes(`REPOSITORY=${expectedRepo}`)) errors.push('local skill missing canonical REPOSITORY');
  if (!content.includes(requiredMarker)) errors.push('local skill missing strict scope lock');

  const agentPath = join(root, '.agents/skills/solqaryn-project-governance/agents/openai.yaml');
  if (!existsSync(agentPath)) {
    errors.push('local skill missing agents/openai.yaml');
  } else {
    const agent = readFileSync(agentPath, 'utf8');
    if (!/display_name:\s*["']?SOLQARYN\b/.test(agent)) {
      errors.push('local skill display_name must start with SOLQARYN');
    }
  }
}

if (existsSync(join(root, registryPath))) {
  const registry = read(registryPath);
  if (!registry.includes('LOCAL_SKILL_COUNT=1')) errors.push('registry must declare LOCAL_SKILL_COUNT=1');
  if (!registry.includes('EXTERNAL_SKILL_SOURCES=9')) errors.push('registry must declare EXTERNAL_SKILL_SOURCES=9');
  for (const [source, pin] of expectedExternalSources) {
    if (!registry.includes(source)) errors.push(`registry missing authorized source: ${source}`);
    if (!registry.includes(pin)) errors.push(`registry missing authorized pin/resolution for: ${source}`);
  }
}

if (existsSync(join(root, allowlistPath))) {
  const allowlist = read(allowlistPath);
  if (!allowlist.includes('AUTHORIZED_ORIGINAL_SKILL_SOURCES=9')) {
    errors.push('allowlist must declare nine authorized original skill sources');
  }
  for (const [source, pin] of expectedExternalSources) {
    if (!allowlist.includes(source)) errors.push(`allowlist missing authorized source: ${source}`);
    if (!allowlist.includes(pin)) errors.push(`allowlist missing pin/resolution for: ${source}`);
  }
}

const identityFiles = mandatory.filter(rel =>
  existsSync(join(root, rel)) &&
  ![registryPath, allowlistPath].includes(rel)
);
for (const rel of identityFiles) {
  const content = read(rel);
  for (const match of content.matchAll(/PROJECT_ID\s*[:=]\s*[`"']?([A-Za-z0-9_-]+)/g)) {
    if (match[1].toUpperCase() !== expectedProjectId) {
      errors.push(`${rel} declares a non-canonical PROJECT_ID`);
    }
  }
  for (const match of content.matchAll(/REPOSITORY\s*[:=]\s*[`"']?([A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+)/g)) {
    if (match[1] !== expectedRepo) {
      errors.push(`${rel} declares a non-canonical REPOSITORY`);
    }
  }
  if (/skills:\/\//i.test(content)) {
    errors.push(`${rel} contains a direct external skill URI; resolve external references through the SOLQARYN registry`);
  }
}


const retiredIdentityToken = ['vari', 'app'].join('');
const retiredIdentityPattern = new RegExp(retiredIdentityToken, 'i');

function looksBinary(buffer) {
  return buffer.includes(0);
}

for (const abs of walk(root)) {
  const rel = relative(root, abs).split(sep).join('/');
  if (rel.startsWith('.git/')) continue;
  if (retiredIdentityPattern.test(rel)) {
    errors.push(`retired project identity remains in path: ${rel}`);
  }

  const raw = readFileSync(abs);
  if (looksBinary(raw)) continue;

  let content;
  try {
    content = raw.toString('utf8');
  } catch {
    continue;
  }

  if (retiredIdentityPattern.test(content)) {
    errors.push(`retired project identity remains in content: ${rel}`);
  }
}

if (errors.length) {
  console.error('SOLQARYN PROJECT SCOPE GATE FAILED');
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log('SOLQARYN PROJECT SCOPE GATE OK: canonical SOLQARYN identity + one local skill + nine pinned original references');
