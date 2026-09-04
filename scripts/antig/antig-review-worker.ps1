#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Once,
    [switch]$SelfTest,
    [switch]$ContractSelfTest,
    [int]$PollSeconds = 60,
    [string]$Repository = "jmejia31/VariApp",
    [string]$Branch = "Desarrollo"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-Native {
    param(
        [Parameter(Mandatory=$true)][string]$File,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [switch]$AllowFailure
    )
    $output = & $File @Arguments 2>&1
    $code = $LASTEXITCODE
    $text = ($output | Out-String).Trim()
    if (-not $AllowFailure -and $code -ne 0) {
        throw "$File $($Arguments -join ' ') failed with exit=$code" + [Environment]::NewLine + $text
    }
    [pscustomobject]@{ ExitCode=$code; Text=$text }
}

function Write-JsonNoBom([string]$Path, $Value) {
    $json = $Value | ConvertTo-Json -Depth 30
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Get-RepoRoot {
    $candidate = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
    return (Invoke-Native git @("-C",$candidate,"rev-parse","--show-toplevel")).Text
}

function Sync-And-AssertClean([string]$RepoRoot) {
    Set-Location $RepoRoot
    $origin = (Invoke-Native git @("remote","get-url","origin")).Text
    $allowed = @(
        "https://github.com/jmejia31/VariApp",
        "https://github.com/jmejia31/VariApp.git",
        "git@github.com:jmejia31/VariApp.git",
        "ssh://git@github.com/jmejia31/VariApp.git"
    )
    if ($origin -notin $allowed) { throw "PROJECT GUARD: wrong origin '$origin'." }

    $current = (Invoke-Native git @("branch","--show-current")).Text
    if ($current -ne $Branch) { throw "PROJECT GUARD: branch '$current' != '$Branch'." }

    $env:GIT_TERMINAL_PROMPT = "0"
    $env:GCM_INTERACTIVE = "Never"
    Invoke-Native git @("fetch","origin","--prune","--quiet") | Out-Null

    $status = (Invoke-Native git @("status","--porcelain")).Text
    if (-not [string]::IsNullOrWhiteSpace($status)) { throw "FAIL_CLOSED: primary working tree is not clean." }

    $div = (Invoke-Native git @("rev-list","--left-right","--count","HEAD...origin/$Branch")).Text -split "\s+"
    if ([int]$div[0] -ne 0 -or [int]$div[1] -ne 0) {
        throw "FAIL_CLOSED: checkout diverged from origin/$Branch (ahead=$($div[0]) behind=$($div[1]))."
    }

    return (Invoke-Native git @("rev-parse","HEAD")).Text
}

function Get-State([string]$StatePath) {
    if (-not (Test-Path -LiteralPath $StatePath)) {
        return [pscustomobject]@{ lastSeenIssue = 0; updatedAt = [DateTime]::UtcNow.ToString("o") }
    }
    return (Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json)
}

function Save-State([string]$StatePath, [int]$IssueNumber) {
    Write-JsonNoBom $StatePath ([ordered]@{
        lastSeenIssue = $IssueNumber
        updatedAt = [DateTime]::UtcNow.ToString("o")
    })
}

function Save-Quarantine([string]$StateRoot, $Issue, [string]$Reason) {
    $dir = Join-Path $StateRoot "quarantine"
    [IO.Directory]::CreateDirectory($dir) | Out-Null
    $path = Join-Path $dir ("issue-" + [int]$Issue.number + ".json")
    Write-JsonNoBom $path ([ordered]@{
        issueNumber = [int]$Issue.number
        issueUrl = [string]$Issue.url
        title = [string]$Issue.title
        reason = $Reason
        quarantinedAt = [DateTime]::UtcNow.ToString("o")
        retryAutomatically = $false
        listoReal = $false
    })
    $body = "[AntiG] QUARANTINED_INVALID_HANDOFF: $Reason" +
        [Environment]::NewLine + [Environment]::NewLine +
        "The lane watermark will advance so this malformed handoff cannot block later Jules results. LISTO_REAL=no; controller review required."
    Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body",$body) -AllowFailure | Out-Null
}

function Get-TerminalIssues([int]$AfterIssue) {
    $raw = Invoke-Native gh @("issue","list","--repo",$Repository,"--state","all","--limit","100","--json","number,title,body,createdAt,url")
    $items = @($raw.Text | ConvertFrom-Json)
    return @($items | Where-Object {
        $_.number -gt $AfterIssue -and $_.title -match '^\[VAEP-JULES(?:-[BCD])?\] .+ result$'
    } | Sort-Object number)
}

function Match-One([string]$Text, [string]$Pattern, [string]$Name) {
    $m = [regex]::Match($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $m.Success) { throw "QUARANTINE: invalid Jules evidence; missing $Name." }
    return $m.Groups[1].Value.Trim()
}

function Normalize-Scopes($ScopeValue) {
    $items = @()
    if ($ScopeValue -is [System.Array]) {
        $items = @($ScopeValue | ForEach-Object { [string]$_ })
    }
    elseif ($null -ne $ScopeValue) {
        $items = @(([string]$ScopeValue) -split ';')
    }
    return @(
        $items |
        ForEach-Object { ($_ -replace '\\','/').Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
    )
}

function Test-ScopeMatch([string]$Path, [string[]]$Scopes) {
    $candidate = ($Path -replace '\\','/').TrimStart('/')
    foreach ($scope0 in $Scopes) {
        $scope = ($scope0 -replace '\\','/').Trim().TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($scope)) { continue }

        if ($scope.IndexOfAny([char[]]"*?[") -ge 0) {
            $wc = [System.Management.Automation.WildcardPattern]::new(
                $scope,
                [System.Management.Automation.WildcardOptions]::IgnoreCase
            )
            if ($wc.IsMatch($candidate)) { return $true }
        }
        elseif (
            $candidate.Equals($scope,[StringComparison]::OrdinalIgnoreCase) -or
            $candidate.StartsWith($scope.TrimEnd('/') + "/",[StringComparison]::OrdinalIgnoreCase)
        ) {
            return $true
        }
    }
    return $false
}

function Test-ProtectedPath([string]$Path) {
    $p = ($Path -replace '\\','/').TrimStart('/')
    $exact = @("AGENTS.md",".env",".env.local",".env.production")
    if ($p -in $exact) { return $true }
    foreach ($prefix in @(
        ".git/",".github/workflows/",".agents/","scripts/antig/","vaep/schemas/",
        "Produccion/","Production/","production/","secrets/","certificates/"
    )) {
        if ($p.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    if ($p -match '(^|/)\.env(\.|$)') { return $true }
    if ($p -match '(?i)(secret|credential|private[-_]?key|certificate)') { return $true }
    return $false
}

function Get-ChangedPaths([string]$RepoRoot) {
    $changedText = (Invoke-Native git @("-C",$RepoRoot,"diff","--name-only")).Text
    $untrackedText = (Invoke-Native git @("-C",$RepoRoot,"ls-files","--others","--exclude-standard")).Text
    $all = @()
    foreach ($text in @($changedText,$untrackedText)) {
        if (-not [string]::IsNullOrWhiteSpace($text)) { $all += @($text -split "\r?\n") }
    }
    return @(
        $all |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { ($_ -replace '\\','/').Trim() } |
        Sort-Object -Unique
    )
}

function Get-PatchPaths([string]$PatchPath) {
    $paths = @()
    foreach ($line in (Get-Content -LiteralPath $PatchPath)) {
        $m = [regex]::Match($line, '^diff --git a/(.+?) b/(.+)$')
        if ($m.Success) {
            foreach ($value in @($m.Groups[1].Value,$m.Groups[2].Value)) {
                if ($value -ne "/dev/null") { $paths += ($value -replace '\\','/') }
            }
        }
    }
    return @($paths | Sort-Object -Unique)
}

function Backup-WorktreeChanges([string]$WorktreeRoot, [string]$RecoveryDir, [string]$Reason) {
    [IO.Directory]::CreateDirectory($RecoveryDir) | Out-Null
    $patch = (Invoke-Native git @("-C",$WorktreeRoot,"diff","--binary")).Text
    if (-not [string]::IsNullOrEmpty($patch)) {
        [IO.File]::WriteAllText((Join-Path $RecoveryDir "changes.patch"), $patch + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    }
    foreach ($rel in (Get-ChangedPaths $WorktreeRoot)) {
        $src = Join-Path $WorktreeRoot $rel
        if (Test-Path -LiteralPath $src -PathType Leaf) {
            $dst = Join-Path $RecoveryDir ("files\" + $rel)
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dst)) | Out-Null
            Copy-Item -LiteralPath $src -Destination $dst -Force
        }
    }
    [IO.File]::WriteAllText((Join-Path $RecoveryDir "reason.txt"), $Reason + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Assert-RemoteStillAt([string]$RepoRoot, [string]$ExpectedHead) {
    $env:GIT_TERMINAL_PROMPT = "0"
    $env:GCM_INTERACTIVE = "Never"
    Invoke-Native git @("-C",$RepoRoot,"fetch","origin","--prune","--quiet") | Out-Null
    $originHead = (Invoke-Native git @("-C",$RepoRoot,"rev-parse","origin/$Branch")).Text
    if ($originHead -ne $ExpectedHead) { throw "FAIL_CLOSED: origin/$Branch moved during AntiG review." }
    return $originHead
}

function Assert-BaseIsAncestor([string]$RepoRoot,[string]$BaseHead,[string]$StartHead) {
    if ($BaseHead -notmatch '^[0-9a-fA-F]{40}$') { throw "QUARANTINE: invalid primary base SHA." }
    $r = Invoke-Native git @("-C",$RepoRoot,"merge-base","--is-ancestor",$BaseHead,$StartHead) -AllowFailure
    if ($r.ExitCode -ne 0) { throw "QUARANTINE: Jules base is not an ancestor of current Desarrollo; stale/conflicting evidence." }
}

function Assert-DispatchContract($Dispatch,[string]$DispatchId,[string]$TaskId,[int]$Attempt,[string]$Session,[string]$RepoRoot,[string]$StartHead) {
    foreach ($required in @("dispatchId","taskId","primaryBaseHead","fileScopeHint")) {
        if (-not ($Dispatch.PSObject.Properties.Name -contains $required)) { throw "QUARANTINE: dispatch missing '$required'." }
    }
    if ([string]$Dispatch.dispatchId -ne $DispatchId) { throw "QUARANTINE: dispatchId mismatch." }
    if ([string]$Dispatch.taskId -ne $TaskId) { throw "QUARANTINE: taskId mismatch." }

    $dispatchAttempt = $null
    if ($Dispatch.PSObject.Properties.Name -contains "attempt") { $dispatchAttempt = [int]$Dispatch.attempt }
    elseif ($Dispatch.PSObject.Properties.Name -contains "taskAttempt") { $dispatchAttempt = [int]$Dispatch.taskAttempt }
    else { throw "QUARANTINE: dispatch missing attempt metadata." }
    if ($dispatchAttempt -ne $Attempt -or $dispatchAttempt -notin @(1,2)) { throw "QUARANTINE: dispatch attempt mismatch/out of range." }

    $strict = $Dispatch.PSObject.Properties.Name -contains "schemaVersion"
    if ($strict) {
        if ([string]$Dispatch.schemaVersion -ne "1.0") { throw "QUARANTINE: unsupported dispatch schemaVersion." }
        if ([string]$Dispatch.projectId -ne "VARIAPP") { throw "QUARANTINE: dispatch projectId mismatch." }
        if ([string]$Dispatch.repository -ne $Repository) { throw "QUARANTINE: dispatch repository mismatch." }
        if ([string]$Dispatch.branch -ne $Branch) { throw "QUARANTINE: dispatch branch mismatch." }
        if ([string]$Dispatch.parentId -notmatch '^N[0-9]+\.[0-9]+\.[A-H]$') { throw "QUARANTINE: invalid parentId." }
        if ($Dispatch.PSObject.Properties.Name -contains "attemptConsumed" -and [bool]$Dispatch.attemptConsumed) { throw "QUARANTINE: dispatch attempt already consumed." }
        foreach ($dep in @($Dispatch.dependencies)) {
            if ([string]$dep.status -ne "SATISFIED") { throw "QUARANTINE: dispatch dependency '$($dep.taskId)' is not SATISFIED." }
        }
        if ($null -eq $Dispatch.ownership -or [string]::IsNullOrWhiteSpace([string]$Dispatch.ownership.owner)) { throw "QUARANTINE: dispatch ownership missing." }
        if ([string]$Dispatch.ownership.status -notin @("AVAILABLE","RELEASED")) { throw "QUARANTINE: dispatch ownership status invalid." }
        if (@($Dispatch.ownership.scopes).Count -eq 0) { throw "QUARANTINE: dispatch ownership scopes missing." }
        if ($Dispatch.PSObject.Properties.Name -contains "session" -and $null -ne $Dispatch.session) {
            if ([string]$Dispatch.session.sessionId -ne $Session -and [string]$Dispatch.session.sessionId -ne ($Session -replace '^sessions/','')) {
                throw "QUARANTINE: dispatch session identity mismatch."
            }
        }
    }
    else {
        foreach ($required in @("workerId","expectedBranch","prompt")) {
            if (-not ($Dispatch.PSObject.Properties.Name -contains $required)) { throw "QUARANTINE: legacy v3.25 dispatch missing '$required'." }
        }
        if ([string]$Dispatch.workerId -notin @("JULES_A","JULES_B","JULES_C","JULES_D")) { throw "QUARANTINE: invalid Jules workerId." }
        if ([string]$Dispatch.expectedBranch -ne $Branch) { throw "QUARANTINE: legacy dispatch branch mismatch." }
        if ([string]::IsNullOrWhiteSpace([string]$Dispatch.prompt)) { throw "QUARANTINE: legacy dispatch prompt is empty." }
    }

    Assert-BaseIsAncestor $RepoRoot ([string]$Dispatch.primaryBaseHead) $StartHead
    $scopes = @(Normalize-Scopes $Dispatch.fileScopeHint)
    if ($scopes.Count -eq 0) { throw "QUARANTINE: dispatch has no usable file scope." }
    return $scopes
}

function Assert-ResultContract($Result,$Dispatch,[string]$DispatchId,[string]$TaskId,[int]$Attempt,[string]$Session,[string]$ActualPatchBase) {
    foreach ($required in @("dispatchId","taskId","taskAttempt","session","state","requestedBase","actualPatchBase","patchPresent","workerId")) {
        if (-not ($Result.PSObject.Properties.Name -contains $required)) { throw "QUARANTINE: result missing '$required'." }
    }
    if ([string]$Result.dispatchId -ne $DispatchId -or [string]$Result.taskId -ne $TaskId) { throw "QUARANTINE: result identity mismatch." }
    if ([int]$Result.taskAttempt -ne $Attempt) { throw "QUARANTINE: result attempt mismatch." }
    if ([string]$Result.session -ne $Session) { throw "QUARANTINE: result session mismatch." }
    if ([string]$Result.state -ne "COMPLETED" -or -not [bool]$Result.patchPresent) { throw "QUARANTINE: result is not a completed patch handoff." }
    if ([string]$Result.requestedBase -ne [string]$Dispatch.primaryBaseHead) { throw "QUARANTINE: requestedBase does not match dispatch primaryBaseHead." }
    if ([string]$Result.actualPatchBase -ne $ActualPatchBase) { throw "QUARANTINE: result actualPatchBase mismatch." }
    if ($ActualPatchBase -notmatch '^[0-9a-fA-F]{40}$') { throw "QUARANTINE: invalid actualPatchBase." }
    if ([string]$Result.workerId -notin @("JULES_A","JULES_B","JULES_C","JULES_D")) { throw "QUARANTINE: invalid result workerId." }
    if ($Dispatch.PSObject.Properties.Name -contains "workerId" -and [string]$Result.workerId -ne [string]$Dispatch.workerId) {
        throw "QUARANTINE: worker identity mismatch between dispatch and result."
    }
}

function Assert-PathsAuthorized([string[]]$Paths,[string[]]$Scopes,[string]$Context) {
    foreach ($path in $Paths) {
        if (Test-ProtectedPath $path) { throw "QUARANTINE: protected path '$path' detected in $Context." }
        if (-not (Test-ScopeMatch $path $Scopes)) { throw "QUARANTINE: SCOPE_LEAK '$path' detected in $Context." }
    }
}

function Invoke-InternalContractSelfTests([string]$RepoRoot) {
    $head = (Invoke-Native git @("-C",$RepoRoot,"rev-parse","HEAD")).Text
    $dispatch = [pscustomobject]@{
        dispatchId="TEST-DISPATCH-0001"; taskId="N9.9.A.TEST"; workerId="JULES_A"; expectedBranch="Desarrollo"
        primaryBaseHead=$head; taskAttempt=1; fileScopeHint="backend/src/**; backend/tests/**"; prompt="test"
    }
    $scopes = Assert-DispatchContract $dispatch "TEST-DISPATCH-0001" "N9.9.A.TEST" 1 "sessions/test" $RepoRoot $head
    if ($scopes.Count -lt 1) { throw "Contract self-test failed: scopes." }

    $result = [pscustomobject]@{
        protocol="v3.25"; workerId="JULES_A"; dispatchId="TEST-DISPATCH-0001"; taskId="N9.9.A.TEST"; taskAttempt=1
        session="sessions/test"; state="COMPLETED"; requestedBase=$head; actualPatchBase=$head; patchPresent=$true
    }
    Assert-ResultContract $result $dispatch "TEST-DISPATCH-0001" "N9.9.A.TEST" 1 "sessions/test" $head

    $bad = $dispatch.PSObject.Copy()
    $bad.expectedBranch = "main"
    $caught = $false
    try { Assert-DispatchContract $bad "TEST-DISPATCH-0001" "N9.9.A.TEST" 1 "sessions/test" $RepoRoot $head | Out-Null }
    catch { $caught = $_.Exception.Message -like "QUARANTINE:*" }
    if (-not $caught) { throw "Contract self-test failed: invalid branch was not quarantined." }

    if (-not (Test-ProtectedPath ".github/workflows/unsafe.yml")) { throw "Contract self-test failed: protected workflow path." }
    if (Test-ProtectedPath "backend/src/safe.cs") { throw "Contract self-test failed: safe path false-positive." }
    Write-Host "ANTIG_CONTRACT_SELF_TEST=PASS" -ForegroundColor Green
}

function Process-Issue($Issue, [string]$RepoRoot, [string]$StateRoot, [string]$SchemaPath) {
    $body = [string]$Issue.body
    $dispatchId = Match-One $body '- Dispatch:\s+[^A-Za-z0-9]*([A-Za-z0-9_.:-]+)' "dispatch"
    $taskId = Match-One $body '- Task:\s+[^A-Za-z0-9]*([A-Za-z0-9_.-]+)' "task"
    $attempt = [int](Match-One $body '- Task attempt:\s+[^0-9]*([12])/2' "taskAttempt")
    $session = Match-One $body '- Jules session:\s+[^A-Za-z0-9]*([A-Za-z0-9/]+)' "session"
    $terminal = Match-One $body '- Terminal state:\s+[^A-Z]*([A-Z_]+)' "terminal state"
    $patchPresent = Match-One $body '- Patch present:\s+[^a-z]*(true|false)' "patch present"
    $runUrl = Match-One $body '- Workflow run:\s+(https://github\.com/[^\s]+)' "workflow run"

    $expectedRunPrefix = "https://github.com/$Repository/actions/runs/"
    if (-not $runUrl.StartsWith($expectedRunPrefix,[StringComparison]::OrdinalIgnoreCase)) {
        throw "QUARANTINE: workflow run URL is outside the authorized repository."
    }

    if ($terminal -ne "COMPLETED" -or $patchPresent -ne "true") {
        $comment = "[AntiG] REVIEW_NOT_STARTED: terminal=$terminal patchPresent=$patchPresent. No LISTO_REAL; controller review required."
        Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body",$comment) | Out-Null
        return "PROCESSED"
    }

    $startHead = Sync-And-AssertClean $RepoRoot
    $runId = Match-One $runUrl '/actions/runs/(\d+)' "run id"
    $run = (Invoke-Native gh @("api","repos/$Repository/actions/runs/$runId")).Text | ConvertFrom-Json
    if ([string]$run.head_branch -ne $Branch) { throw "QUARANTINE: causal workflow run is not on Desarrollo." }

    $jobRoot = Join-Path $StateRoot ("jobs\" + $Issue.number)
    $artifactDir = Join-Path $jobRoot "artifact"
    if (Test-Path -LiteralPath $jobRoot) { Remove-Item -LiteralPath $jobRoot -Recurse -Force }
    [IO.Directory]::CreateDirectory($artifactDir) | Out-Null

    $artifactEnvelope = (Invoke-Native gh @("api","repos/$Repository/actions/runs/$runId/artifacts")).Text | ConvertFrom-Json
    $matches = @(
        $artifactEnvelope.artifacts |
        Where-Object { -not $_.expired -and ([string]$_.name).IndexOf($dispatchId,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
    )
    if ($matches.Count -ne 1) { throw "QUARANTINE: expected exactly one causal artifact for dispatch=$dispatchId run=$runId; found $($matches.Count)." }
    $artifact = $matches[0]

    Invoke-Native gh @("run","download",$runId,"--repo",$Repository,"--name",[string]$artifact.name,"--dir",$artifactDir) | Out-Null

    $dispatchPath = Join-Path $artifactDir "dispatch.json"
    $resultPath = Join-Path $artifactDir "result.json"
    $patchPath = Join-Path $artifactDir "changes.patch"
    $gitPatchPath = Join-Path $artifactDir "gitpatch.json"
    foreach ($p in @($dispatchPath,$resultPath,$patchPath,$gitPatchPath)) {
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { throw "QUARANTINE: causal artifact missing '$([IO.Path]::GetFileName($p))'." }
    }

    $dispatch = Get-Content -LiteralPath $dispatchPath -Raw | ConvertFrom-Json
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    $gitPatch = Get-Content -LiteralPath $gitPatchPath -Raw | ConvertFrom-Json
    if ($null -eq $gitPatch -or [string]::IsNullOrWhiteSpace([string]$gitPatch.baseCommitId)) {
        throw "QUARANTINE: gitpatch.json missing baseCommitId."
    }

    $scopes = @(Assert-DispatchContract $dispatch $dispatchId $taskId $attempt $session $RepoRoot $startHead)
    Assert-ResultContract $result $dispatch $dispatchId $taskId $attempt $session ([string]$gitPatch.baseCommitId)

    $parentId = if ($dispatch.PSObject.Properties.Name -contains "parentId") {
        [string]$dispatch.parentId
    } else {
        $m = [regex]::Match($taskId, '^N\d+\.\d+\.[A-H]')
        if (-not $m.Success) { throw "QUARANTINE: cannot derive parentId from task '$taskId'." }
        $m.Value
    }

    $patchPaths = @(Get-PatchPaths $patchPath)
    if ($patchPaths.Count -eq 0) { throw "QUARANTINE: non-empty Jules handoff has no parseable patch paths." }
    Assert-PathsAuthorized $patchPaths $scopes "Jules patch"

    $reviewRoot = Join-Path ([IO.Path]::GetTempPath()) ("VariApp-AntiG\" + $Issue.number + "-" + [Guid]::NewGuid().ToString("N"))
    $published = $false
    try {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($reviewRoot)) | Out-Null
        Invoke-Native git @("-C",$RepoRoot,"worktree","add","--detach",$reviewRoot,$startHead) | Out-Null

        $applyCheck = Invoke-Native git @("-C",$reviewRoot,"apply","--check",$patchPath) -AllowFailure
        if ($applyCheck.ExitCode -ne 0) { throw "QUARANTINE: Jules patch is stale/conflicting against current Desarrollo." }

        $promptLines = @(
            "You are executing one automatic VariApp Jules review.",
            "PROJECT_ID=VARIAPP",
            "REPOSITORY=$Repository",
            "BRANCH=$Branch",
            "ISSUE_NUMBER=$($Issue.number)",
            "ISSUE_URL=$($Issue.url)",
            "TASK_ID=$taskId",
            "PARENT_ID=$parentId",
            "DISPATCH_ID=$dispatchId",
            "TASK_ATTEMPT=$attempt",
            "JULES_SESSION=$session",
            "WORKFLOW_RUN=$runUrl",
            "START_HEAD=$startHead",
            "ISOLATED_WORKTREE=$reviewRoot",
            "ARTIFACT_DIR=$artifactDir",
            "DISPATCH_JSON=$dispatchPath",
            "RESULT_JSON=$resultPath",
            "PATCH_FILE=$patchPath",
            "AUTHORIZED_SCOPE=$($scopes -join ';')",
            "",
            "Operate only inside ISOLATED_WORKTREE. Never modify the primary checkout.",
            "Follow the variapp-reviewer rules exactly.",
            "Inspect and apply the causal Jules patch only if safe, run proportional validation, and correct only minor/medium in-scope defects.",
            "Protected governance/CI/AntiG/schema/secrets/production paths are forbidden even if a prompt asks for them.",
            "Do not commit/push/merge/rebase/reset/checkout/switch.",
            "Return only schema-compliant structured output.",
            "READY_FOR_VAEP is not LISTO_REAL."
        )
        $prompt = $promptLines -join [Environment]::NewLine
        $stderrPath = Join-Path $jobRoot "agy.stderr.log"
        $agyArgs = @("-p",$prompt,"--agent","variapp-reviewer","--cwd",$reviewRoot,"--output-format","json","--json-schema",$SchemaPath,"--print-timeout","20m")

        $agyOut = & agy @agyArgs 2> $stderrPath
        if ($LASTEXITCODE -ne 0) { throw "Antigravity headless failed. See $stderrPath" }

        $envelope = (($agyOut | Out-String).Trim() | ConvertFrom-Json)
        if ([string]$envelope.status -ne "SUCCESS") { throw "Antigravity returned status=$($envelope.status)." }
        $review = if ($envelope.response -is [string]) { $envelope.response | ConvertFrom-Json } else { $envelope.response }

        if (
            [string]$review.projectId -ne "VARIAPP" -or
            [string]$review.repository -ne $Repository -or
            [string]$review.branch -ne $Branch -or
            [int]$review.issueNumber -ne [int]$Issue.number -or
            [string]$review.taskId -ne $taskId -or
            [string]$review.parentId -ne $parentId -or
            [string]$review.dispatchId -ne $dispatchId -or
            [int]$review.taskAttempt -ne $attempt
        ) { throw "QUARANTINE: AntiG structured result identity mismatch." }

        $changed = @(Get-ChangedPaths $reviewRoot)

        if ([string]$review.decision -ne "READY_FOR_VAEP") {
            if ($changed.Count -gt 0) { Backup-WorktreeChanges $reviewRoot (Join-Path $jobRoot "recovery") "decision=$($review.decision)" }
            $comment = "[AntiG] decision=$($review.decision); attempt=$attempt; readyForVaep=false; LISTO_REAL=no." +
                [Environment]::NewLine + [Environment]::NewLine + [string]$review.summary
            if (@($review.blockers).Count -gt 0) { $comment += [Environment]::NewLine + [Environment]::NewLine + "Blockers: " + (@($review.blockers) -join "; ") }
            Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body",$comment) | Out-Null
            return "PROCESSED"
        }

        if (
            -not [bool]$review.readyForVaep -or
            [int]$review.p0 -ne 0 -or
            [int]$review.p1 -ne 0 -or
            @($review.blockers).Count -ne 0 -or
            [string]$review.scopeAssessment -ne "IN_SCOPE"
        ) { throw "QUARANTINE: READY_FOR_VAEP invariants failed." }

        if ($changed.Count -eq 0) {
            Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body","[AntiG] NO_ACTION: READY_FOR_VAEP produced no workspace delta; publication skipped. LISTO_REAL=no.") | Out-Null
            return "PROCESSED"
        }

        Assert-PathsAuthorized $changed $scopes "AntiG workspace delta"
        $declared = @($review.filesChanged | ForEach-Object { ([string]$_ -replace '\\','/').Trim() } | Sort-Object -Unique)
        if (($declared -join [Environment]::NewLine) -ne ($changed -join [Environment]::NewLine)) { throw "QUARANTINE: AntiG filesChanged does not equal the actual isolated-worktree delta." }

        $failed = @($review.validations | Where-Object { $_.status -eq "FAIL" })
        if ($failed.Count -gt 0) { throw "QUARANTINE: AntiG returned failed validations while READY_FOR_VAEP." }

        Invoke-Native git @("-C",$reviewRoot,"diff","--check") | Out-Null
        Assert-RemoteStillAt $RepoRoot $startHead | Out-Null
        if ((Invoke-Native git @("-C",$reviewRoot,"rev-parse","HEAD")).Text -ne $startHead) { throw "FAIL_CLOSED: AntiG changed commit history." }

        $beforeStage = @(Get-ChangedPaths $reviewRoot)
        if (($beforeStage -join [Environment]::NewLine) -ne ($changed -join [Environment]::NewLine)) { throw "FAIL_CLOSED: isolated-worktree delta changed after authorization check." }

        $addArgs = @("-C",$reviewRoot,"add","--") + $changed
        Invoke-Native -File git -Arguments $addArgs | Out-Null
        $stagedText = (Invoke-Native git @("-C",$reviewRoot,"diff","--cached","--name-only")).Text
        $staged = @()
        if (-not [string]::IsNullOrWhiteSpace($stagedText)) { $staged = @($stagedText -split "\r?\n" | Sort-Object -Unique) }
        if (($staged -join [Environment]::NewLine) -ne ($changed -join [Environment]::NewLine)) { throw "FAIL_CLOSED: staged paths differ from authorized paths." }
        Invoke-Native git @("-C",$reviewRoot,"diff","--cached","--check") | Out-Null

        Invoke-Native git @("-C",$reviewRoot,"commit","-m","fix($taskId): integrate Jules review issue $($Issue.number) [AntiG]") | Out-Null
        $codeHead = (Invoke-Native git @("-C",$reviewRoot,"rev-parse","HEAD")).Text

        $fragmentDir = Join-Path $reviewRoot ("vaep\evidence\fragments\" + $parentId)
        [IO.Directory]::CreateDirectory($fragmentDir) | Out-Null
        $safeDispatch = ($dispatchId -replace '[^A-Za-z0-9._-]','_')
        $fragmentPath = Join-Path $fragmentDir ($safeDispatch + "-antig.json")
        $fragmentRel = ("vaep/evidence/fragments/" + $parentId + "/" + $safeDispatch + "-antig.json")
        $tests = @($review.validations | ForEach-Object { "$($_.name):$($_.status):$($_.command)" })

        $fragment = [ordered]@{
            taskId=$taskId; parentId=$parentId; worker="ANTIGRAVITY"; dispatchId="$dispatchId-ANTIG"
            baseHead=$startHead; resultHead=$codeHead; status="PASS"
            evidence=@("AntiG automated review issue $($Issue.url)","decision=READY_FOR_VAEP",[string]$review.summary)
            tests=$tests; workflows=@(); artifacts=@($runUrl); p0=0; p1=0
            timestamp=[DateTime]::UtcNow.ToString("o"); blockers=@(); attempt=$attempt; fileScope=$changed
            notes="READY_FOR_VAEP only; autoPromote=false; LISTO_REAL requires separate VAEP/controller certification."
        }
        Write-JsonNoBom $fragmentPath $fragment
        Invoke-Native git @("-C",$reviewRoot,"add","--",$fragmentRel) | Out-Null
        Invoke-Native git @("-C",$reviewRoot,"commit","-m","chore(vaep): record AntiG review $taskId [AntiG]") | Out-Null
        $evidenceHead = (Invoke-Native git @("-C",$reviewRoot,"rev-parse","HEAD")).Text

        Assert-RemoteStillAt $RepoRoot $startHead | Out-Null
        Invoke-Native git @("-C",$reviewRoot,"push","origin","HEAD:$Branch") | Out-Null
        $published = $true

        $comment = "[AntiG] READY_FOR_VAEP. codeHead=$codeHead evidenceHead=$evidenceHead P0=0 P1=0. LISTO_REAL=no; VAEP/controller certification remains mandatory."
        Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body",$comment) | Out-Null
        return "PROCESSED"
    }
    finally {
        if (Test-Path -LiteralPath $reviewRoot) {
            if (-not $published) {
                $recoveryDir = Join-Path $jobRoot "recovery"
                $delta = @(Get-ChangedPaths $reviewRoot)
                if ($delta.Count -gt 0) { Backup-WorktreeChanges $reviewRoot $recoveryDir "cleanup before publication" }
                $headNow = (Invoke-Native git @("-C",$reviewRoot,"rev-parse","HEAD") -AllowFailure).Text
                if (-not [string]::IsNullOrWhiteSpace($headNow) -and $headNow -ne $startHead) {
                    [IO.Directory]::CreateDirectory($recoveryDir) | Out-Null
                    $commitPatch = (Invoke-Native git @("-C",$reviewRoot,"diff","--binary","$startHead..$headNow") -AllowFailure).Text
                    if (-not [string]::IsNullOrWhiteSpace($commitPatch)) {
                        [IO.File]::WriteAllText((Join-Path $recoveryDir "committed.patch"),$commitPatch + [Environment]::NewLine,[Text.UTF8Encoding]::new($false))
                    }
                    [IO.File]::WriteAllText((Join-Path $recoveryDir "local-head.txt"),$headNow + [Environment]::NewLine,[Text.UTF8Encoding]::new($false))
                }
            }
            Invoke-Native git @("-C",$RepoRoot,"worktree","remove","--force",$reviewRoot) -AllowFailure | Out-Null
            if (Test-Path -LiteralPath $reviewRoot) { Remove-Item -LiteralPath $reviewRoot -Recurse -Force -ErrorAction SilentlyContinue }
        }
    }
}

$original = Get-Location
$lockStream = $null

try {
    if ($Repository -ne "jmejia31/VariApp" -or $Branch -ne "Desarrollo") { throw "Unauthorized repository/branch." }
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "Missing required command 'git'." }

    $repoRoot = Get-RepoRoot
    Set-Location $repoRoot

    if ($ContractSelfTest) {
        Invoke-InternalContractSelfTests $repoRoot
        exit 0
    }

    foreach ($cmd in @("gh","agy")) {
        if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) { throw "Missing required command '$cmd'." }
    }

    $gitDir = (Invoke-Native git @("rev-parse","--git-dir")).Text
    if (-not [IO.Path]::IsPathRooted($gitDir)) { $gitDir = Join-Path $repoRoot $gitDir }
    $stateRoot = Join-Path $gitDir "vaep-antig"
    [IO.Directory]::CreateDirectory($stateRoot) | Out-Null

    $lockPath = Join-Path $stateRoot "worker.lock"
    try {
        $lockStream = [IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    }
    catch {
        Write-Host "AntiG worker already active; exiting." -ForegroundColor Yellow
        exit 0
    }

    $schemaPath = Join-Path $repoRoot "vaep\schemas\antig-review-result.schema.json"
    if (-not (Test-Path -LiteralPath $schemaPath -PathType Leaf)) { throw "Missing AntiG result schema." }

    if ($SelfTest) {
        $head = Sync-And-AssertClean $repoRoot
        Write-Host "ANTIG_WORKER_SELF_TEST=PASS HEAD=$head" -ForegroundColor Green
        exit 0
    }

    do {
        Sync-And-AssertClean $repoRoot | Out-Null
        $statePath = Join-Path $stateRoot "state.json"
        $state = Get-State $statePath
        $issues = @(Get-TerminalIssues ([int]$state.lastSeenIssue))

        if ($issues.Count -gt 0) {
            $issue = $issues[0]
            try {
                $result = Process-Issue $issue $repoRoot $stateRoot $schemaPath
                if ($result -eq "PROCESSED") { Save-State $statePath ([int]$issue.number) }
            }
            catch {
                $message = $_.Exception.Message
                if ($message -like "QUARANTINE:*") {
                    Save-Quarantine $stateRoot $issue ($message.Substring("QUARANTINE:".Length).Trim())
                    Save-State $statePath ([int]$issue.number)
                    Write-Warning "Issue $($issue.number) quarantined; lane advanced."
                }
                else { throw }
            }
        }

        if ($Once) { break }
        Start-Sleep -Seconds $PollSeconds
    } while ($true)
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    Set-Location $original
}
