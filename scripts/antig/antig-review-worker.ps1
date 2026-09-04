#requires -Version 5.1
[CmdletBinding()]
param(
    [switch]$Once,
    [switch]$SelfTest,
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
    $json = $Value | ConvertTo-Json -Depth 20
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Get-RepoRoot {
    (Invoke-Native git @("rev-parse","--show-toplevel")).Text
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
    if (-not [string]::IsNullOrWhiteSpace($status)) { throw "FAIL_CLOSED: working tree is not clean." }

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

function Get-TerminalIssues([int]$AfterIssue) {
    $raw = Invoke-Native gh @("issue","list","--repo",$Repository,"--state","all","--limit","100","--json","number,title,body,createdAt,url")
    $items = @($raw.Text | ConvertFrom-Json)
    return @($items | Where-Object {
        $_.number -gt $AfterIssue -and $_.title -match '^\[VAEP-JULES(?:-[BCD])?\] .+ result$'
    } | Sort-Object number)
}

function Match-One([string]$Text, [string]$Pattern, [string]$Name) {
    $m = [regex]::Match($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    if (-not $m.Success) { throw "Invalid Jules evidence: missing $Name." }
    return $m.Groups[1].Value.Trim()
}

function Backup-And-RevertOwnedChanges([string]$RepoRoot, [string]$RecoveryDir, [string]$Reason) {
    [IO.Directory]::CreateDirectory($RecoveryDir) | Out-Null

    $patch = (Invoke-Native git @("diff","--binary")).Text
    if (-not [string]::IsNullOrEmpty($patch)) {
        [IO.File]::WriteAllText((Join-Path $RecoveryDir "changes.patch"), $patch + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    }

    $untrackedText = (Invoke-Native git @("ls-files","--others","--exclude-standard")).Text
    $untracked = @()
    if (-not [string]::IsNullOrWhiteSpace($untrackedText)) {
        $untracked = @($untrackedText -split "\r?\n")
    }

    foreach ($rel in $untracked) {
        $src = Join-Path $RepoRoot $rel
        if (Test-Path -LiteralPath $src -PathType Leaf) {
            $dst = Join-Path $RecoveryDir ("untracked\" + $rel)
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dst)) | Out-Null
            Copy-Item -LiteralPath $src -Destination $dst -Force
        }
    }

    Invoke-Native git @("restore","--staged","--worktree","--",".") | Out-Null

    foreach ($rel in $untracked) {
        $src = Join-Path $RepoRoot $rel
        if (Test-Path -LiteralPath $src) { Remove-Item -LiteralPath $src -Force -Recurse }
    }

    [IO.File]::WriteAllText((Join-Path $RecoveryDir "reason.txt"), $Reason + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function Test-ScopeMatch([string]$Path, [string[]]$Scopes) {
    foreach ($scope0 in $Scopes) {
        $scope = ($scope0 -replace '\\','/').Trim()
        if ([string]::IsNullOrWhiteSpace($scope)) { continue }

        $candidate = ($Path -replace '\\','/')
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

function Assert-RemoteStillAt([string]$ExpectedHead) {
    $env:GIT_TERMINAL_PROMPT = "0"
    $env:GCM_INTERACTIVE = "Never"
    Invoke-Native git @("fetch","origin","--prune","--quiet") | Out-Null
    $originHead = (Invoke-Native git @("rev-parse","origin/$Branch")).Text
    if ($originHead -ne $ExpectedHead) {
        throw "FAIL_CLOSED: origin/$Branch moved during AntiG review."
    }
    return $originHead
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

    if ($terminal -ne "COMPLETED" -or $patchPresent -ne "true") {
        $comment = "[AntiG] REVIEW_NOT_STARTED: terminal=$terminal patchPresent=$patchPresent. No LISTO_REAL; controller review required."
        Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body",$comment) | Out-Null
        return "PROCESSED"
    }

    $runId = Match-One $runUrl '/actions/runs/(\d+)' "run id"
    $jobRoot = Join-Path $StateRoot ("jobs\" + $Issue.number)
    $artifactDir = Join-Path $jobRoot "artifact"
    if (Test-Path -LiteralPath $jobRoot) { Remove-Item -LiteralPath $jobRoot -Recurse -Force }
    [IO.Directory]::CreateDirectory($artifactDir) | Out-Null

    $artifactRaw = Invoke-Native gh @("api","repos/$Repository/actions/runs/$runId/artifacts")
    $artifactEnvelope = $artifactRaw.Text | ConvertFrom-Json
    $artifact = @(
        $artifactEnvelope.artifacts |
        Where-Object { $_.name -like "*$dispatchId*" } |
        Sort-Object created_at -Descending |
        Select-Object -First 1
    )
    if ($artifact.Count -ne 1) {
        throw "Causal Jules artifact not found for dispatch=$dispatchId run=$runId."
    }

    Invoke-Native gh @(
        "run","download",$runId,
        "--repo",$Repository,
        "--name",[string]$artifact[0].name,
        "--dir",$artifactDir
    ) | Out-Null

    $dispatchPath = Join-Path $artifactDir "dispatch.json"
    $resultPath = Join-Path $artifactDir "result.json"
    $patchPath = Join-Path $artifactDir "changes.patch"
    foreach ($p in @($dispatchPath,$resultPath,$patchPath)) {
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
            throw "Causal artifact missing '$p'."
        }
    }

    $dispatch = Get-Content -LiteralPath $dispatchPath -Raw | ConvertFrom-Json

    $parentId = if ($dispatch.PSObject.Properties.Name -contains "parentId") {
        [string]$dispatch.parentId
    }
    else {
        $m = [regex]::Match($taskId, '^N\d+\.\d+\.[A-H]')
        if (-not $m.Success) { throw "Cannot derive parentId from task '$taskId'." }
        $m.Value
    }

    $scopes = @()
    if ($dispatch.PSObject.Properties.Name -contains "fileScopeHint") {
        $scopeValue = $dispatch.fileScopeHint
        if ($scopeValue -is [System.Array]) {
            $scopes = @($scopeValue | ForEach-Object { [string]$_ })
        }
        elseif ($null -ne $scopeValue) {
            $scopes = @([string]$scopeValue)
        }
    }
    if ($scopes.Count -eq 0) { throw "Dispatch has no usable file scope." }

    $startHead = Sync-And-AssertClean $RepoRoot

    $promptLines = @(
        "You are executing one automatic VariApp Jules review.",
        "ISSUE_NUMBER=$($Issue.number)",
        "ISSUE_URL=$($Issue.url)",
        "TASK_ID=$taskId",
        "PARENT_ID=$parentId",
        "DISPATCH_ID=$dispatchId",
        "TASK_ATTEMPT=$attempt",
        "JULES_SESSION=$session",
        "WORKFLOW_RUN=$runUrl",
        "START_HEAD=$startHead",
        "ARTIFACT_DIR=$artifactDir",
        "DISPATCH_JSON=$dispatchPath",
        "RESULT_JSON=$resultPath",
        "PATCH_FILE=$patchPath",
        "AUTHORIZED_SCOPE=$($scopes -join ';')",
        "",
        "Follow the variapp-reviewer rules exactly.",
        "Inspect the artifact and patch, apply only if safe, run proportional validation, and correct only minor/medium in-scope defects.",
        "Do not commit/push/merge/rebase/reset/checkout/switch.",
        "Return only schema-compliant structured output.",
        "READY_FOR_VAEP is not LISTO_REAL."
    )
    $prompt = $promptLines -join [Environment]::NewLine

    $stderrPath = Join-Path $jobRoot "agy.stderr.log"
    $agyArgs = @(
        "-p",$prompt,
        "--agent","variapp-reviewer",
        "--cwd",$RepoRoot,
        "--output-format","json",
        "--json-schema",$SchemaPath,
        "--print-timeout","20m"
    )

    $agyOut = & agy @agyArgs 2> $stderrPath
    $agyCode = $LASTEXITCODE
    if ($agyCode -ne 0) {
        throw "Antigravity headless failed exit=$agyCode. See $stderrPath"
    }

    $envelope = (($agyOut | Out-String).Trim() | ConvertFrom-Json)
    if ([string]$envelope.status -ne "SUCCESS") {
        throw "Antigravity returned status=$($envelope.status)."
    }

    $review = if ($envelope.response -is [string]) {
        $envelope.response | ConvertFrom-Json
    }
    else {
        $envelope.response
    }

    if (
        [int]$review.issueNumber -ne [int]$Issue.number -or
        [string]$review.taskId -ne $taskId -or
        [string]$review.dispatchId -ne $dispatchId -or
        [int]$review.taskAttempt -ne $attempt
    ) {
        throw "AntiG structured result identity mismatch."
    }

    $changedText = (Invoke-Native git @("diff","--name-only")).Text
    $untrackedText = (Invoke-Native git @("ls-files","--others","--exclude-standard")).Text
    $changed = @()
    foreach ($text in @($changedText,$untrackedText)) {
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            $changed += @($text -split "\r?\n")
        }
    }
    $changed = @(
        $changed |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
    )

    if ([string]$review.decision -ne "READY_FOR_VAEP") {
        if ($changed.Count -gt 0) {
            Backup-And-RevertOwnedChanges $RepoRoot (Join-Path $jobRoot "recovery") "decision=$($review.decision)"
        }

        $comment = "[AntiG] decision=$($review.decision); attempt=$attempt; readyForVaep=false; LISTO_REAL=no." +
            [Environment]::NewLine + [Environment]::NewLine + [string]$review.summary

        if (@($review.blockers).Count -gt 0) {
            $comment += [Environment]::NewLine + [Environment]::NewLine +
                "Blockers: " + (@($review.blockers) -join "; ")
        }

        Invoke-Native gh @("issue","comment",[string]$Issue.number,"--repo",$Repository,"--body",$comment) | Out-Null
        return "PROCESSED"
    }

    if (
        -not [bool]$review.readyForVaep -or
        [int]$review.p0 -ne 0 -or
        [int]$review.p1 -ne 0 -or
        @($review.blockers).Count -ne 0 -or
        [string]$review.scopeAssessment -ne "IN_SCOPE"
    ) {
        if ($changed.Count -gt 0) {
            Backup-And-RevertOwnedChanges $RepoRoot (Join-Path $jobRoot "recovery") "READY_FOR_VAEP invariant failed"
        }
        throw "READY_FOR_VAEP invariants failed."
    }

    if ($changed.Count -eq 0) {
        Invoke-Native gh @(
            "issue","comment",[string]$Issue.number,
            "--repo",$Repository,
            "--body","[AntiG] NO_ACTION: READY_FOR_VAEP produced no workspace delta; publication skipped. LISTO_REAL=no."
        ) | Out-Null
        return "PROCESSED"
    }

    foreach ($path in $changed) {
        if (-not (Test-ScopeMatch $path $scopes)) {
            Backup-And-RevertOwnedChanges $RepoRoot (Join-Path $jobRoot "recovery") "scope leak: $path"
            throw "SCOPE_LEAK: '$path' is outside dispatch scope."
        }
    }

    $failed = @($review.validations | Where-Object { $_.status -eq "FAIL" })
    if ($failed.Count -gt 0) {
        Backup-And-RevertOwnedChanges $RepoRoot (Join-Path $jobRoot "recovery") "validation failure"
        throw "AntiG returned failed validations while READY_FOR_VAEP."
    }

    Invoke-Native git @("diff","--check") | Out-Null
    Assert-RemoteStillAt $startHead | Out-Null

    if ((Invoke-Native git @("rev-parse","HEAD")).Text -ne $startHead) {
        throw "FAIL_CLOSED: AntiG changed commit history."
    }

    # Only the wrapper may publish, after all guards pass.
    Invoke-Native git @("add","--all") | Out-Null
    Invoke-Native git @(
        "commit","-m",
        "fix($taskId): integrate Jules review issue $($Issue.number) [AntiG]"
    ) | Out-Null
    $codeHead = (Invoke-Native git @("rev-parse","HEAD")).Text

    $fragmentDir = Join-Path $RepoRoot ("vaep\evidence\fragments\" + $parentId)
    [IO.Directory]::CreateDirectory($fragmentDir) | Out-Null
    $safeDispatch = ($dispatchId -replace '[^A-Za-z0-9._-]','_')
    $fragmentPath = Join-Path $fragmentDir ($safeDispatch + "-antig.json")
    $tests = @(
        $review.validations |
        ForEach-Object { "$($_.name):$($_.status):$($_.command)" }
    )

    $fragment = [ordered]@{
        taskId = $taskId
        parentId = $parentId
        worker = "ANTIGRAVITY"
        dispatchId = "$dispatchId-ANTIG"
        baseHead = $startHead
        resultHead = $codeHead
        status = "PASS"
        evidence = @(
            "AntiG automated review issue $($Issue.url)",
            "decision=READY_FOR_VAEP",
            [string]$review.summary
        )
        tests = $tests
        workflows = @()
        artifacts = @($runUrl)
        p0 = 0
        p1 = 0
        timestamp = [DateTime]::UtcNow.ToString("o")
        blockers = @()
        attempt = $attempt
        fileScope = $changed
        notes = "READY_FOR_VAEP only; autoPromote=false; LISTO_REAL requires separate VAEP/controller certification."
    }

    Write-JsonNoBom $fragmentPath $fragment
    Invoke-Native git @("add","--",$fragmentPath) | Out-Null
    Invoke-Native git @(
        "commit","-m",
        "chore(vaep): record AntiG review $taskId [AntiG]"
    ) | Out-Null
    $evidenceHead = (Invoke-Native git @("rev-parse","HEAD")).Text

    Assert-RemoteStillAt $startHead | Out-Null
    Invoke-Native git @("push","origin","HEAD:$Branch") | Out-Null

    $comment = "[AntiG] READY_FOR_VAEP. codeHead=$codeHead evidenceHead=$evidenceHead P0=0 P1=0. LISTO_REAL=no; VAEP/controller certification remains mandatory."
    Invoke-Native gh @(
        "issue","comment",[string]$Issue.number,
        "--repo",$Repository,
        "--body",$comment
    ) | Out-Null

    return "PROCESSED"
}

$original = Get-Location
$lockStream = $null

try {
    foreach ($cmd in @("git","gh","agy")) {
        if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
            throw "Missing required command '$cmd'."
        }
    }

    if ($Repository -ne "jmejia31/VariApp" -or $Branch -ne "Desarrollo") {
        throw "Unauthorized repository/branch."
    }

    $repoRoot = Get-RepoRoot
    Set-Location $repoRoot

    $gitDir = (Invoke-Native git @("rev-parse","--git-dir")).Text
    if (-not [IO.Path]::IsPathRooted($gitDir)) {
        $gitDir = Join-Path $repoRoot $gitDir
    }

    $stateRoot = Join-Path $gitDir "vaep-antig"
    [IO.Directory]::CreateDirectory($stateRoot) | Out-Null

    $lockPath = Join-Path $stateRoot "worker.lock"
    try {
        $lockStream = [IO.File]::Open(
            $lockPath,
            [IO.FileMode]::OpenOrCreate,
            [IO.FileAccess]::ReadWrite,
            [IO.FileShare]::None
        )
    }
    catch {
        Write-Host "AntiG worker already active; exiting." -ForegroundColor Yellow
        exit 0
    }

    $schemaPath = Join-Path $repoRoot "vaep\schemas\antig-review-result.schema.json"
    if (-not (Test-Path -LiteralPath $schemaPath -PathType Leaf)) {
        throw "Missing AntiG result schema."
    }

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
            $result = Process-Issue $issue $repoRoot $stateRoot $schemaPath
            if ($result -eq "PROCESSED") {
                Save-State $statePath ([int]$issue.number)
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
