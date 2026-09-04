#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Remove,
    [switch]$SkipAuthProbe,
    [string]$TaskName = "VariApp-AntiG-Reviewer"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Native {
    param(
        [string]$File,
        [string[]]$Arguments,
        [switch]$AllowFailure
    )
    $output = & $File @Arguments 2>&1
    $code = $LASTEXITCODE
    $text = ($output | Out-String).Trim()
    if (-not $AllowFailure -and $code -ne 0) {
        throw "$File $($Arguments -join ' ') failed exit=$code" + [Environment]::NewLine + $text
    }
    [pscustomobject]@{ ExitCode=$code; Text=$text }
}

function Add-UniqueValues($Object,[string]$PropertyName,[string[]]$Values) {
    if (-not ($Object.PSObject.Properties.Name -contains $PropertyName)) {
        $Object | Add-Member -NotePropertyName $PropertyName -NotePropertyValue @()
    }
    $current = @($Object.$PropertyName)
    foreach ($v in $Values) {
        if ($v -notin $current) { $current += $v }
    }
    $Object.$PropertyName = $current
}

if ($env:OS -ne "Windows_NT") {
    throw "This installer targets the authorized Windows VariApp workstation."
}

if ($Remove) {
    Invoke-Native schtasks.exe @("/Delete","/TN",$TaskName,"/F") -AllowFailure | Out-Null
    Write-Host "Scheduled task '$TaskName' removed. Global Antigravity permissions were preserved." -ForegroundColor Yellow
    exit 0
}

foreach ($cmd in @("git","gh","agy","schtasks.exe")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        throw "Missing required command '$cmd'."
    }
}

$repoRoot = (Invoke-Native git @("rev-parse","--show-toplevel")).Text
Set-Location $repoRoot

$origin = (Invoke-Native git @("remote","get-url","origin")).Text
if ($origin -notmatch 'jmejia31/VariApp(\.git)?$') {
    throw "PROJECT GUARD: wrong origin '$origin'."
}

$branch = (Invoke-Native git @("branch","--show-current")).Text
if ($branch -ne "Desarrollo") {
    throw "PROJECT GUARD: run installer on Desarrollo."
}

$env:GIT_TERMINAL_PROMPT = "0"
$env:GCM_INTERACTIVE = "Never"
Invoke-Native git @("fetch","origin","--prune","--quiet") | Out-Null

$status = (Invoke-Native git @("status","--porcelain")).Text
if (-not [string]::IsNullOrWhiteSpace($status)) {
    throw "Working tree must be clean before installation."
}

$div = (Invoke-Native git @("rev-list","--left-right","--count","HEAD...origin/Desarrollo")).Text -split "\s+"
if ([int]$div[0] -ne 0 -or [int]$div[1] -ne 0) {
    throw "Checkout must equal origin/Desarrollo before installation."
}

Invoke-Native gh @("auth","status") | Out-Null

$agents = Invoke-Native agy @("agents")
if ($agents.Text -notmatch 'variapp-reviewer') {
    throw "Antigravity CLI did not discover workspace agent 'variapp-reviewer'. Reopen from repo root and retry."
}

$settingsDir = Join-Path $HOME ".gemini\antigravity-cli"
$settingsPath = Join-Path $settingsDir "settings.json"
[IO.Directory]::CreateDirectory($settingsDir) | Out-Null
$settingsExisted = Test-Path -LiteralPath $settingsPath
$settingsBackup = if ($settingsExisted) { [IO.File]::ReadAllBytes($settingsPath) } else { $null }

if ($settingsExisted) {
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
}
else {
    $settings = [pscustomobject]@{}
}

if (-not ($settings.PSObject.Properties.Name -contains "permissions")) {
    $settings | Add-Member -NotePropertyName permissions -NotePropertyValue ([pscustomobject]@{})
}

if (-not ($settings.permissions.PSObject.Properties.Name -contains "ask")) {
    $settings.permissions | Add-Member -NotePropertyName ask -NotePropertyValue @()
}

if (@($settings.permissions.ask) -contains "command(*)") {
    throw "Antigravity settings contain ask=command(*), which overrides scoped allow rules. Resolve that policy explicitly before enabling headless automation."
}

$allow = @(
    "command(git status)",
    "command(git diff)",
    "command(git show)",
    "command(git log)",
    "command(git rev-parse)",
    "command(git ls-files)",
    "command(git apply)",
    "command(npm run lint)",
    "command(npm run build)",
    "command(npm run test)",
    "command(npm.cmd run lint)",
    "command(npm.cmd run build)",
    "command(npm.cmd run test)",
    "command(dotnet build)",
    "command(dotnet test)",
    "command(node --check)"
)

$deny = @(
    "command(git add)",
    "command(git commit)",
    "command(git push)",
    "command(git merge)",
    "command(git rebase)",
    "command(git reset)",
    "command(git checkout)",
    "command(git switch)",
    "command(vercel)",
    "command(mysql)",
    "command(mysqldump)"
)

Add-UniqueValues $settings.permissions "allow" $allow
Add-UniqueValues $settings.permissions "deny" $deny

$json = $settings | ConvertTo-Json -Depth 20

if (-not $SkipAuthProbe) {
    $probe = Invoke-Native agy @(
        "-p","Return exactly READY. Do not use tools.",
        "--agent","variapp-reviewer",
        "--cwd",$repoRoot,
        "--output-format","json",
        "--print-timeout","2m"
    )
    $probeJson = $probe.Text | ConvertFrom-Json
    if ([string]$probeJson.status -ne "SUCCESS") {
        throw "Antigravity authentication probe failed."
    }
}

& (Join-Path $repoRoot "scripts\antig\antig-self-test.ps1") -StaticOnly
if ($LASTEXITCODE -ne 0) {
    throw "AntiG static/functional self-test failed."
}

Invoke-Native powershell.exe @(
    "-NoProfile",
    "-ExecutionPolicy","Bypass",
    "-File",(Join-Path $repoRoot "scripts\antig\antig-review-worker.ps1"),
    "-SelfTest",
    "-Once"
) | Out-Null

$gitDir = (Invoke-Native git @("rev-parse","--git-dir")).Text
if (-not [IO.Path]::IsPathRooted($gitDir)) {
    $gitDir = Join-Path $repoRoot $gitDir
}

$stateRoot = Join-Path $gitDir "vaep-antig"
[IO.Directory]::CreateDirectory($stateRoot) | Out-Null

$issuesRaw = Invoke-Native gh @(
    "issue","list",
    "--repo","jmejia31/VariApp",
    "--state","all",
    "--limit","100",
    "--json","number,title"
)

$issues = @(
    $issuesRaw.Text |
    ConvertFrom-Json |
    Where-Object { $_.title -match '^\[VAEP-JULES(?:-[BCD])?\] .+ result$' }
)

$watermark = 0
if ($issues.Count -gt 0) {
    $watermark = [int](($issues | Measure-Object number -Maximum).Maximum)
}

$state = [ordered]@{
    lastSeenIssue = $watermark
    updatedAt = [DateTime]::UtcNow.ToString("o")
    installedFromHead = (Invoke-Native git @("rev-parse","HEAD")).Text
}

$statePath = Join-Path $stateRoot "state.json"
$stateExisted = Test-Path -LiteralPath $statePath
$stateBackup = if ($stateExisted) { [IO.File]::ReadAllBytes($statePath) } else { $null }

$workerPath = Join-Path $repoRoot "scripts\antig\antig-review-worker.ps1"
$quotedWorker = '"' + $workerPath + '"'
$taskAction = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File $quotedWorker -Once"
$taskCreated = $false
$settingsWritten = $false
$stateWritten = $false

try {
    [IO.File]::WriteAllText(
        $settingsPath,
        $json + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false)
    )
    $settingsWritten = $true

    [IO.File]::WriteAllText(
        $statePath,
        ($state | ConvertTo-Json -Depth 5) + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false)
    )
    $stateWritten = $true

    Invoke-Native schtasks.exe @(
        "/Create",
        "/TN",$TaskName,
        "/TR",$taskAction,
        "/SC","MINUTE",
        "/MO","1",
        "/RL","LIMITED",
        "/F"
    ) | Out-Null
    $taskCreated = $true

    Invoke-Native schtasks.exe @("/Query","/TN",$TaskName) | Out-Null
}
catch {
    $originalError = $_.Exception.Message

    if ($taskCreated) {
        Invoke-Native schtasks.exe @("/Delete","/TN",$TaskName,"/F") -AllowFailure | Out-Null
    }

    if ($stateWritten) {
        if ($stateExisted) {
            [IO.File]::WriteAllBytes($statePath,$stateBackup)
        }
        elseif (Test-Path -LiteralPath $statePath) {
            Remove-Item -LiteralPath $statePath -Force
        }
    }

    if ($settingsWritten) {
        if ($settingsExisted) {
            [IO.File]::WriteAllBytes($settingsPath,$settingsBackup)
        }
        elseif (Test-Path -LiteralPath $settingsPath) {
            Remove-Item -LiteralPath $settingsPath -Force
        }
    }

    throw "AntiG installation rolled back transactionally: $originalError"
}

Write-Host ""
Write-Host "ANTIG_AUTOMATION=INSTALLED" -ForegroundColor Green
Write-Host "TASK=$TaskName" -ForegroundColor Green
Write-Host "CADENCE=1 minute" -ForegroundColor Green
Write-Host "WATERMARK=$watermark (historical Jules issues will not be replayed)" -ForegroundColor Green
Write-Host "AGENT=variapp-reviewer" -ForegroundColor Green
Write-Host "LISTO_REAL_AUTHORITY=VAEP_CONTROLLER_ONLY" -ForegroundColor Green
