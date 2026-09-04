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

$script:AntiGStatus = "RESERVED_INACTIVE"
$script:AntiGScheduler = "DISABLED"
$script:AntiGFutureReincorporation = "EXPLICIT_AUTHORIZATION_REQUIRED"

function Write-AntiGInstallerState {
    Write-Host "ANTIG_STATUS=$script:AntiGStatus"
    Write-Host "ANTIG_SCHEDULER=$script:AntiGScheduler"
    Write-Host "ANTIG_INSTALLATION_ALLOWED=FALSE"
    Write-Host "ANTIG_FUTURE_REINCORPORATION=$script:AntiGFutureReincorporation"
}

if ($SelfTest) {
    if ($script:AntiGStatus -ne "RESERVED_INACTIVE") { throw "Installer reserved-state self-test failed." }
    if ($script:AntiGScheduler -ne "DISABLED") { throw "Installer scheduler state must be DISABLED." }
    Write-AntiGInstallerState
    Write-Host "ANTIG_INSTALLER_RESERVED_INACTIVE_SELF_TEST=PASS"
    exit 0
}

if ($Remove) {
    Write-AntiGInstallerState
    if ($env:OS -ne "Windows_NT") {
        Write-Host "ANTIG_REMOVE=SKIPPED_NON_WINDOWS"
        exit 0
    }
    if ($null -eq (Get-Command schtasks.exe -ErrorAction SilentlyContinue)) {
        throw "schtasks.exe unavailable."
    }

    & schtasks.exe /Query /TN $TaskName 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        & schtasks.exe /Delete /TN $TaskName /F | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Failed to remove legacy AntiG Scheduled Task '$TaskName'." }
        Write-Host "ANTIG_LEGACY_TASK_REMOVED=$TaskName"
    }
    else {
        Write-Host "ANTIG_LEGACY_TASK_PRESENT=FALSE"
    }
    exit 0
}

Write-AntiGInstallerState
Write-Error "AntiG installation/activation is disabled by MASTER. Explicit future authorization and a repository changeset are required before scheduler creation can exist again."
exit 78
