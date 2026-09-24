import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, relative, sep } from 'node:path';

const root = process.cwd();
const expectedRepo = 'solqaryn/VariApp';
const expectedProjectId = 'VARIAPP';
const requiredMarker = 'PROJECT_SCOPE_LOCK=STRICT';
const skillPrefix = 'solqaryn-';
const displayPrefix = 'SOLQARYN';

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
  '.agents/skills/solqaryn-project-governance/SKILL.md',
];

const errors = [];

function read(rel) {
  return readFileSync(join(root, rel), 'utf8');
}

for (const rel of mandatory) {
  if (!existsSync(join(root, rel))) {
    errors.push(`missing required scope file: ${rel}`);
    continue;
  }
  const content = read(rel);
  if (!content.includes(requiredMarker)) errors.push(`${rel} missing ${requiredMarker}`);
  if (content.includes('.agents/skills/') && content.includes('project-governance') &&
      !content.includes('.agents/skills/solqaryn-project-governance/')) {
    errors.push(`${rel} contains a non-SOLQARYN project skill path`);
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

const skillsRoot = join(root, '.agents', 'skills');
for (const path of walk(skillsRoot).filter(p => p.endsWith('SKILL.md'))) {
  const rel = relative(root, path).split(sep).join('/');
  const parts = rel.split('/');
  const skillDir = parts[2] ?? '';
  const content = readFileSync(path, 'utf8');

  if (!skillDir.startsWith(skillPrefix)) errors.push(`${rel} skill directory must start with ${skillPrefix}`);

  const nameMatch = content.match(/^name:\s*([^\n]+)$/m);
  const name = nameMatch?.[1]?.trim().replace(/^["']|["']$/g, '') ?? '';
  if (!name.startsWith(skillPrefix)) errors.push(`${rel} frontmatter name must start with ${skillPrefix}`);

  if (!content.includes(`PROJECT_ID=${expectedProjectId}`)) errors.push(`${rel} missing PROJECT_ID=${expectedProjectId}`);
  if (!content.includes(`REPOSITORY=${expectedRepo}`)) errors.push(`${rel} missing REPOSITORY=${expectedRepo}`);
  if (!content.includes(requiredMarker)) errors.push(`${rel} missing ${requiredMarker}`);
  if (/skills:\/\//i.test(content)) errors.push(`${rel} may not depend on external skill URIs`);

  const agentPath = join(path, '..', 'agents', 'openai.yaml');
  if (!existsSync(agentPath)) {
    errors.push(`${rel} missing agents/openai.yaml`);
  } else {
    const agent = readFileSync(agentPath, 'utf8');
    const displayMatch = agent.match(/display_name:\s*["']?([^\n"']+)/);
    const displayName = displayMatch?.[1]?.trim() ?? '';
    if (!displayName.startsWith(displayPrefix)) errors.push(`${rel} display_name must start with ${displayPrefix}`);
  }
}

const governanceFiles = mandatory
  .filter(rel => existsSync(join(root, rel)))
  .map(rel => ({ rel, content: read(rel) }));

for (const { rel, content } of governanceFiles) {
  for (const match of content.matchAll(/PROJECT_ID\s*[:=]\s*[`"']?([A-Za-z0-9_-]+)/g)) {
    if (match[1].toUpperCase() !== expectedProjectId) {
      errors.push(`${rel} declares a non-canonical PROJECT_ID`);
    }
  }

  for (const match of content.matchAll(/REPOSITORY\s*[:=]\s*[`"']?([A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+)/g)) {
    if (match[1] !== expectedRepo) errors.push(`${rel} declares a non-canonical REPOSITORY`);
  }

  if (/skills:\/\//i.test(content)) errors.push(`${rel} contains an external skill URI`);
}

if (errors.length) {
  console.error('SOLQARYN PROJECT SCOPE GATE FAILED');
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`SOLQARYN PROJECT SCOPE GATE OK: ${expectedRepo} / ${requiredMarker}`);
