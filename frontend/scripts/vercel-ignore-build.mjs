import { execFileSync } from 'node:child_process';

const previous = process.env.VERCEL_GIT_PREVIOUS_SHA;
const current = process.env.VERCEL_GIT_COMMIT_SHA || 'HEAD';

// Fail open when Vercel cannot provide a trustworthy comparison. An unknown
// change must build; only an explicitly non-runtime-only diff may be skipped.
if (!previous) {
  process.exit(1);
}

let changedFiles;
try {
  changedFiles = execFileSync(
    'git',
    ['diff', '--name-only', '--no-renames', previous, current],
    { encoding: 'utf8' }
  )
    .split(/\r?\n/)
    .map(file => file.trim().replaceAll('\\', '/'))
    .filter(Boolean);
} catch {
  process.exit(1);
}

const explicitlyNonRuntime = file =>
  [
    'AGENTS.md',
    'PROJECT_CONTEXT.md',
    'PROJECT_INDEX.md',
    'ARCHITECTURE.md',
    'ARCHITECTURE_CHANGELOG.md',
    'TASKS.md',
    'CHANGELOG_AI.md',
    'PLAN_EJECUCION_AUTONOMA.md',
    'implementation_plan.md',
    'README.md'
  ].includes(file) ||
  file.startsWith('docs/') ||
  file.startsWith('.github/') ||
  file.startsWith('vaep/');

if (changedFiles.length > 0 && changedFiles.every(explicitlyNonRuntime)) {
  // Exit 0 tells Vercel that no build is required for this diff.
  process.exit(0);
}

// Any frontend or otherwise unclassified change must build.
process.exit(1);
