#requires -Version 5.1
[CmdletBinding()]
param([switch]$StaticOnly)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

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
foreach ($marker in @("name: variapp-reviewer","Never declare or write LISTO_REAL","Never run git add, commit, push, merge, rebase, reset, checkout or switch")) {
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
if ($worker -match 'dangerously-skip-permissions') { throw "Unsafe Antigravity permission bypass detected." }
foreach ($marker in @('origin/$Branch moved during AntiG review',"SCOPE_LEAK","READY_FOR_VAEP","LISTO_REAL=no")) {
    if ($worker -notlike "*$marker*") { throw "Worker guard missing: $marker" }
}

Write-Host "ANTIG_STATIC_SELF_TEST=PASS" -ForegroundColor Green
