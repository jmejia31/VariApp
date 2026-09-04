#requires -Version 5.1
[CmdletBinding()]
param([switch]$StaticOnly)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (& git rev-parse --show-toplevel 2>$null | Out-String).Trim()
if ([string]::IsNullOrWhiteSpace($repoRoot)) { throw "Not inside a Git repository." }

$required = @(
    ".agents/agents/variapp-reviewer/agent.md",
    "docs/ANTIGRAVITY_AUTOMATION.md",
    "scripts/antig/antig-review-worker.ps1",
    "scripts/antig/antig-self-test.ps1",
    "scripts/antig/install-antig-automation.ps1",
    "vaep/schemas/antig-review-result.schema.json",
    "docs/VAEP_AUTHORITY.md"
)
foreach ($rel in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $rel) -PathType Leaf)) {
        throw "Missing preserved AntiG component: $rel"
    }
}

$schema = Get-Content -LiteralPath (Join-Path $repoRoot "vaep/schemas/antig-review-result.schema.json") -Raw | ConvertFrom-Json
if ($schema.properties.decision.enum -contains "LISTO_REAL") {
    throw "Dormant AntiG schema must not allow LISTO_REAL."
}

$master = Get-Content -LiteralPath (Join-Path $repoRoot "docs/VAEP_AUTHORITY.md") -Raw
foreach ($marker in @(
    "ANTIG_STATUS=RESERVED_INACTIVE",
    "ANTIG_OPERATIONAL_NOW=FALSE",
    "ANTIG_SCHEDULER=DISABLED",
    "ANTIG_HANDOFF_PROCESSING=DISABLED",
    "ANTIG_AUTHORITY=MASTER",
    "ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE",
    "ANTIG_FUTURE_REINCORPORATION=EXPLICIT_AUTHORIZATION_REQUIRED"
)) {
    if ($master -notlike "*$marker*") { throw "MASTER AntiG state missing: $marker" }
}

$agentPath = Join-Path $repoRoot ".agents/agents/variapp-reviewer/agent.md"
$workerPath = Join-Path $repoRoot "scripts/antig/antig-review-worker.ps1"
$installerPath = Join-Path $repoRoot "scripts/antig/install-antig-automation.ps1"
$runbookPath = Join-Path $repoRoot "docs/ANTIGRAVITY_AUTOMATION.md"

$agent = Get-Content -LiteralPath $agentPath -Raw
$worker = Get-Content -LiteralPath $workerPath -Raw
$installer = Get-Content -LiteralPath $installerPath -Raw
$runbook = Get-Content -LiteralPath $runbookPath -Raw

foreach ($path in @($workerPath,$installerPath,$PSCommandPath)) {
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors) | Out-Null
    if ($errors.Count -gt 0) { throw "PowerShell syntax error in $path : $($errors[0].Message)" }
}

foreach ($marker in @("RESERVED_INACTIVE","mainAgent: false","subagent: false","ANTIG_HANDOFF_PROCESSING=DISABLED","ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE")) {
    if ($agent -notlike "*$marker*") { throw "Inactive agent guard missing: $marker" }
}
foreach ($marker in @("RESERVED_INACTIVE","ANTIG_HANDOFF_PROCESSING=DISABLED","ANTIG_NO_ACTION=RESERVED_INACTIVE")) {
    if ($worker -notlike "*$marker*") { throw "Inactive worker guard missing: $marker" }
}
foreach ($marker in @("RESERVED_INACTIVE","ANTIG_SCHEDULER=DISABLED","ANTIG_INSTALLATION_ALLOWED=FALSE","/Delete")) {
    if ($installer -notlike "*$marker*") { throw "Inactive installer guard missing: $marker" }
}
foreach ($marker in @("RESERVED_INACTIVE","ANTIG_SCHEDULER=DISABLED","ANTIG_HANDOFF_PROCESSING=DISABLED","EXPLICIT_AUTHORIZATION_REQUIRED")) {
    if ($runbook -notlike "*$marker*") { throw "Inactive runbook guard missing: $marker" }
}

if ($installer -match '(?i)/Create|Register-ScheduledTask|New-ScheduledTask') {
    throw "Scheduler creation path detected while AntiG is RESERVED_INACTIVE."
}

$activeAntiGText = $agent + [Environment]::NewLine + $worker + [Environment]::NewLine + $installer + [Environment]::NewLine + $runbook
if ($activeAntiGText -match '(?i)\bv[0-9]+\.[0-9]+\b') {
    throw "Numeric AntiG protocol/version label remains in active AntiG surfaces."
}

$psExe = (Get-Process -Id $PID).Path
& $psExe -NoProfile -ExecutionPolicy Bypass -File $workerPath -SelfTest
if ($LASTEXITCODE -ne 0) { throw "Reserved AntiG worker self-test failed." }

& $psExe -NoProfile -ExecutionPolicy Bypass -File $installerPath -SelfTest
if ($LASTEXITCODE -ne 0) { throw "Reserved AntiG installer self-test failed." }

Write-Host "ANTIG_COMPONENTS_PRESERVED=PASS"
Write-Host "ANTIG_STATUS=RESERVED_INACTIVE"
Write-Host "ANTIG_OPERATIONAL_NOW=FALSE"
Write-Host "ANTIG_SCHEDULER=DISABLED"
Write-Host "ANTIG_HANDOFF_PROCESSING=DISABLED"
Write-Host "ANTIG_CAN_CERTIFY_LISTO_REAL=FALSE"
Write-Host "ANTIG_ACTIVE_NUMERIC_PROTOCOL_LABELS=0"
Write-Host "ANTIG_RESERVED_INACTIVE_SELF_TEST=PASS"
