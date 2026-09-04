#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Remove,
    [switch]$SkipAuthProbe,
    [switch]$SelfTest,
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

function Get-TaskRollbackMode([bool]$Existed) {
    if ($Existed) { return "RESTORE_EXISTING_XML" }
    return "DELETE_NEW_TASK"
}

function Test-TaskNotFound($QueryResult) {
    if ($QueryResult.ExitCode -eq 0) { return $false }
    $text = [string]$QueryResult.Text
    return $text -match '(?i)(cannot find|could not find|does not exist|not found|no existe|no se encuentra|no se pudo encontrar|nombre de tarea.*inv[aá]lido)'
}

function Resolve-AgyCommand {
    $command = Get-Command agy -ErrorAction SilentlyContinue
    if ($null -ne $command) { return [string]$command.Source }
    $candidate = Join-Path $env:LOCALAPPDATA "agy\bin\agy.exe"
    if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    throw "Missing required command 'agy' and official fallback '$candidate' was not found."
}

if ($SelfTest) {
    if ((Get-TaskRollbackMode $false) -ne "DELETE_NEW_TASK") { throw "Installer self-test: absent task rollback plan invalid." }
    if ((Get-TaskRollbackMode $true) -ne "RESTORE_EXISTING_XML") { throw "Installer self-test: existing task rollback plan invalid." }
    $missingTask = [pscustomobject]@{ ExitCode = 1; Text = "ERROR: The system cannot find the file specified." }
    $unknownTask = [pscustomobject]@{ ExitCode = 1; Text = "ERROR: Access is denied." }
    if (-not (Test-TaskNotFound $missingTask)) { throw "Installer self-test: missing task was not classified as absent." }
    if (Test-TaskNotFound $unknownTask) { throw "Installer self-test: indeterminate task query was treated as absent." }
    Write-Host "ANTIG_INSTALLER_TRANSACTION_SELF_TEST=PASS" -ForegroundColor Green
    exit 0
}

if ($env:OS -ne "Windows_NT") {
    throw "This installer targets the authorized Windows VariApp workstation."
}

if ($Remove) {
    Invoke-Native schtasks.exe @("/Delete","/TN",$TaskName,"/F") -AllowFailure | Out-Null
    Write-Host "Scheduled task '$TaskName' removed. Global Antigravity permissions were preserved." -ForegroundColor Yellow
    exit 0
}

foreach ($cmd in @("git","gh","schtasks.exe")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        throw "Missing required command '$cmd'."
    }
}
$agyCommand = Resolve-AgyCommand

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

$agents = Invoke-Native -File $agyCommand -Arguments @("agents")
$agentFile = Join-Path $repoRoot ".agents\agents\variapp-reviewer\agent.md"
if ($agents.Text -notmatch 'variapp-reviewer' -and -not (Test-Path -LiteralPath $agentFile -PathType Leaf)) {
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
    $probe = Invoke-Native -File $agyCommand -Arguments @(
        "-p","Return exactly READY. Do not use tools.",
        "--agent","variapp-reviewer",
        "--add-dir",$repoRoot,
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

$taskQuery = Invoke-Native schtasks.exe @("/Query","/TN",$TaskName) -AllowFailure
if ($taskQuery.ExitCode -eq 0) {
    $taskExisted = $true
}
elseif (Test-TaskNotFound $taskQuery) {
    $taskExisted = $false
}
else {
    throw "Unable to determine whether Scheduled Task '$TaskName' exists safely; installation aborted without replacing it. $($taskQuery.Text)"
}
$taskBackupPath = Join-Path ([IO.Path]::GetTempPath()) ("VariApp-AntiG-task-" + [Guid]::NewGuid().ToString("N") + ".xml")
if ($taskExisted) {
    $taskXml = Invoke-Native schtasks.exe @("/Query","/TN",$TaskName,"/XML")
    [IO.File]::WriteAllText($taskBackupPath,$taskXml.Text + [Environment]::NewLine,[Text.UTF8Encoding]::new($false))
}

$workerPath = Join-Path $repoRoot "scripts\antig\antig-review-worker.ps1"
$quotedWorker = '"' + $workerPath + '"'
$taskAction = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File $quotedWorker -Once"
$taskReplaced = $false
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
    $taskReplaced = $true

    Invoke-Native schtasks.exe @("/Query","/TN",$TaskName) | Out-Null
}
catch {
    $originalError = $_.Exception.Message

    if ($taskReplaced) {
        if ($taskExisted) {
            Invoke-Native schtasks.exe @("/Create","/TN",$TaskName,"/XML",$taskBackupPath,"/F") -AllowFailure | Out-Null
        }
        else {
            Invoke-Native schtasks.exe @("/Delete","/TN",$TaskName,"/F") -AllowFailure | Out-Null
        }
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

    if (Test-Path -LiteralPath $taskBackupPath) { Remove-Item -LiteralPath $taskBackupPath -Force -ErrorAction SilentlyContinue }
    throw "AntiG installation rolled back transactionally: $originalError"
}

if (Test-Path -LiteralPath $taskBackupPath) { Remove-Item -LiteralPath $taskBackupPath -Force -ErrorAction SilentlyContinue }

Write-Host ""
Write-Host "ANTIG_AUTOMATION=INSTALLED" -ForegroundColor Green
Write-Host "TASK=$TaskName" -ForegroundColor Green
Write-Host "CADENCE=1 minute" -ForegroundColor Green
Write-Host "WATERMARK=$watermark (historical Jules issues will not be replayed)" -ForegroundColor Green
Write-Host "AGENT=variapp-reviewer" -ForegroundColor Green
Write-Host "LISTO_REAL_AUTHORITY=VAEP_CONTROLLER_ONLY" -ForegroundColor Green
