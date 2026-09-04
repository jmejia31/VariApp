#requires -Version 5.1
[CmdletBinding()]
param([switch]$StaticOnly)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-TestGit {
    param([string]$WorkingDirectory,[string[]]$Arguments)
    $output = & git -C $WorkingDirectory @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git -C $WorkingDirectory $($Arguments -join ' ') failed: " + (($output | Out-String).Trim())
    }
    return (($output | Out-String).Trim())
}

$repoRoot = (& git rev-parse --show-toplevel 2>$null | Out-String).Trim()
if ([string]::IsNullOrWhiteSpace($repoRoot)) { throw "Not inside a Git repository." }

$required = @(
    ".agents/agents/variapp-reviewer/agent.md",
    "scripts/antig/antig-review-worker.ps1",
    "scripts/antig/install-antig-automation.ps1",
    "vaep/schemas/antig-review-result.schema.json",
    "docs/ANTIGRAVITY_AUTOMATION.md"
)
foreach ($rel in $required) {
    $path = Join-Path $repoRoot $rel
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing $rel" }
}

$schemaPath = Join-Path $repoRoot "vaep/schemas/antig-review-result.schema.json"
$schema = Get-Content -LiteralPath $schemaPath -Raw | ConvertFrom-Json
if ($schema.properties.decision.enum -notcontains "READY_FOR_VAEP") { throw "Schema missing READY_FOR_VAEP." }
if ($schema.properties.decision.enum -contains "LISTO_REAL") { throw "AntiG schema must not allow LISTO_REAL." }

$agent = Get-Content -LiteralPath (Join-Path $repoRoot ".agents/agents/variapp-reviewer/agent.md") -Raw
foreach ($marker in @(
    "name: variapp-reviewer",
    "Never declare or write LISTO_REAL",
    "Never run git add, commit, push, merge, rebase, reset, checkout or switch"
)) {
    if ($agent -notlike "*$marker*") { throw "Agent guard missing: $marker" }
}

$workerPath = Join-Path $repoRoot "scripts/antig/antig-review-worker.ps1"
$installerPath = Join-Path $repoRoot "scripts/antig/install-antig-automation.ps1"
foreach ($path in @($workerPath,$installerPath,$PSCommandPath)) {
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors) | Out-Null
    if ($errors.Count -gt 0) { throw "PowerShell syntax error in $path : $($errors[0].Message)" }
}

$worker = Get-Content -LiteralPath $workerPath -Raw
$installer = Get-Content -LiteralPath $installerPath -Raw
foreach ($marker in @("settingsBackup","stateBackup","AntiG installation rolled back transactionally","/Query")) {
    if ($installer -notlike "*$marker*") { throw "Installer transactional guard missing: $marker" }
}

if ($worker -match 'dangerously-skip-permissions') { throw "Unsafe Antigravity permission bypass detected." }
if ($worker -match 'git\s+@\("add","--all"\)' -or $worker -match '"add","--all"') { throw "Unsafe git add --all detected." }
if ($worker -match '"restore","--staged","--worktree","--","\."') { throw "Unsafe whole-tree restore detected." }

foreach ($marker in @(
    'origin/$Branch moved during AntiG review',
    "worktree",
    "--detach",
    "Assert-DispatchContract",
    "Assert-ResultContract",
    "Test-ProtectedPath",
    "frontend/vercel.json",
    "frontend/scripts/vercel-ignore-build.mjs",
    "Select-CausalArtifact",
    "Assert-GitPatchContract",
    "Read-JsonOrQuarantine",
    "Assert-RemoteAt",
    "COMMENT_PENDING",
    "QUARANTINED_INVALID_HANDOFF",
    "staged paths differ from authorized paths",
    "READY_FOR_VAEP",
    "LISTO_REAL=no"
)) {
    if ($worker -notlike "*$marker*") { throw "Worker guard missing: $marker" }
}

# Functional contract tests execute the worker's real validation helpers without gh/agy/network.
$psExe = (Get-Process -Id $PID).Path
& $psExe -NoProfile -ExecutionPolicy Bypass -File $workerPath -ContractSelfTest
if ($LASTEXITCODE -ne 0) { throw "AntiG contract self-test failed with exit=$LASTEXITCODE." }

$installerSelfTest = Join-Path $repoRoot "scripts/antig/install-antig-automation.ps1"
& $psExe -NoProfile -ExecutionPolicy Bypass -File $installerSelfTest -SelfTest
if ($LASTEXITCODE -ne 0) { throw "AntiG installer transaction self-test failed with exit=$LASTEXITCODE." }

# Functional concurrency/isolation test: a concurrent file in the primary checkout must
# survive disposable review worktree creation, review edits and teardown unchanged.
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("antig-selftest-" + [Guid]::NewGuid().ToString("N"))
$primary = Join-Path $tempRoot "primary"
$review = Join-Path $tempRoot "review"
try {
    [IO.Directory]::CreateDirectory($primary) | Out-Null
    Invoke-TestGit $primary @("init") | Out-Null
    Invoke-TestGit $primary @("config","user.email","antig-selftest@localhost") | Out-Null
    Invoke-TestGit $primary @("config","user.name","AntiG Self Test") | Out-Null

    [IO.File]::WriteAllText((Join-Path $primary "tracked.txt"),"BASE",[Text.UTF8Encoding]::new($false))
    Invoke-TestGit $primary @("add","--","tracked.txt") | Out-Null
    Invoke-TestGit $primary @("commit","-m","base") | Out-Null
    $base = Invoke-TestGit $primary @("rev-parse","HEAD")

    Invoke-TestGit $primary @("worktree","add","--detach",$review,$base) | Out-Null
    [IO.File]::WriteAllText((Join-Path $primary "concurrent.txt"),"DO_NOT_DELETE",[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $review "tracked.txt"),"REVIEW_CHANGE",[Text.UTF8Encoding]::new($false))

    $reviewDelta = Invoke-TestGit $review @("diff","--name-only")
    if ($reviewDelta -ne "tracked.txt") { throw "Isolation test: unexpected review delta '$reviewDelta'." }

    Invoke-TestGit $primary @("worktree","remove","--force",$review) | Out-Null

    if (-not (Test-Path -LiteralPath (Join-Path $primary "concurrent.txt"))) {
        throw "Isolation test: concurrent primary-checkout file was removed."
    }
    $concurrent = Get-Content -LiteralPath (Join-Path $primary "concurrent.txt") -Raw
    if ($concurrent -ne "DO_NOT_DELETE") { throw "Isolation test: concurrent file content changed." }
    $tracked = Get-Content -LiteralPath (Join-Path $primary "tracked.txt") -Raw
    if ($tracked -ne "BASE") { throw "Isolation test: primary tracked file changed." }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host "ANTIG_HARDENING_REVISION=P1_CLOSED_V2" -ForegroundColor Green
Write-Host "ANTIG_STATIC_SELF_TEST=PASS" -ForegroundColor Green
Write-Host "ANTIG_FUNCTIONAL_ISOLATION_TEST=PASS" -ForegroundColor Green
