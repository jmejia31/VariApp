[CmdletBinding()]
param(
    [string]$Repository = "jmejia31/VariApp",
    [string]$Branch = "Desarrollo",
    [switch]$SkipSmokeTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Require-Command([string]$Name, [string]$InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "No se encontro '$Name'. $InstallHint"
    }
}

function Invoke-GhJson([string[]]$Arguments) {
    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub CLI fallo: gh $($Arguments -join ' ')"
    }
    return ($output | Out-String).Trim()
}

Write-Step "Validando identidad VariApp"
if ($Repository -ne "jmejia31/VariApp") {
    throw "Repositorio no autorizado para este script: $Repository"
}
if ($Branch -ne "Desarrollo") {
    throw "Rama no autorizada para este script: $Branch"
}

Require-Command "node" "Instala Node.js antes de continuar."
Require-Command "npm" "Instala npm antes de continuar."
Require-Command "git" "Instala Git antes de continuar."
Require-Command "gh" "Instala GitHub CLI y autentica tu cuenta antes de continuar."

Write-Step "Validando GitHub CLI"
gh auth status
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI no esta autenticado. Ejecuta 'gh auth login' y vuelve a correr este script."
}

Write-Step "Instalando/actualizando Jules Tools"
if (-not (Get-Command "jules" -ErrorAction SilentlyContinue)) {
    npm install -g @google/jules
    if ($LASTEXITCODE -ne 0) {
        throw "No fue posible instalar @google/jules."
    }
}

jules version

Write-Step "Autenticando Jules en esta PC"
Write-Host "Se abrira el flujo de autenticacion de Google si Jules aun no tiene una sesion valida."
jules login
if ($LASTEXITCODE -ne 0) {
    throw "Jules login no finalizo correctamente."
}

Write-Step "Comprobando repositorios conectados a Jules"
jules remote list --repo
if ($LASTEXITCODE -ne 0) {
    throw "No fue posible listar los repositorios de Jules."
}

Write-Host "`nAntes del siguiente paso, jmejia31/VariApp debe estar conectado en Jules mediante su GitHub App y la rama Desarrollo debe ser visible." -ForegroundColor Yellow
Write-Host "Si aun no lo esta, completa la conexion en Jules y vuelve a esta consola." -ForegroundColor Yellow
Start-Process "https://jules.google.com/settings"

Write-Step "Registrando JULES_API_KEY como GitHub Actions secret"
Write-Host "Genera/copia la Jules API key desde Settings. La clave NO se mostrara ni se guardara en archivos." -ForegroundColor Yellow
$secureKey = Read-Host "JULES_API_KEY" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)
$plainKey = $null
try {
    $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    if ([string]::IsNullOrWhiteSpace($plainKey)) {
        throw "La API key esta vacia."
    }

    $headers = @{ "x-goog-api-key" = $plainKey }
    $sources = Invoke-RestMethod -Uri "https://jules.googleapis.com/v1alpha/sources?pageSize=100" -Headers $headers -Method Get
    $source = @($sources.sources) | Where-Object {
        $_.githubRepo.owner -eq "jmejia31" -and $_.githubRepo.repo -eq "VariApp"
    } | Select-Object -First 1

    if (-not $source) {
        throw "La API key funciona, pero VariApp aun no aparece como source de Jules. Conecta el repo en la web de Jules y repite el script."
    }

    $hasBranch = @($source.githubRepo.branches) | Where-Object { $_.displayName -eq $Branch }
    if (-not $hasBranch) {
        throw "Jules ve VariApp, pero no expone la rama '$Branch'. Sincroniza permisos/branches en Jules y repite el script."
    }

    Write-Host "Source confirmado: $($source.name) / rama $Branch" -ForegroundColor Green

    # gh secret set lee el valor por STDIN; la clave no queda en argumentos ni archivos.
    $plainKey | gh secret set JULES_API_KEY --repo $Repository
    if ($LASTEXITCODE -ne 0) {
        throw "No fue posible registrar JULES_API_KEY en GitHub Actions."
    }
}
finally {
    if ($null -ne $plainKey) {
        $plainKey = $null
    }
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

Write-Step "Verificando que GitHub registra el secret"
$secretNames = @(& gh secret list --repo $Repository --json name --jq '.[].name')
if ($LASTEXITCODE -ne 0 -or ($secretNames -notcontains "JULES_API_KEY")) {
    throw "GitHub no reporta el secret JULES_API_KEY."
}

if (-not $SkipSmokeTest) {
    Write-Step "Creando smoke test VAEP Jules"

    $refJson = Invoke-GhJson @("api", "repos/$Repository/git/ref/heads/$Branch")
    $baseHead = ($refJson | ConvertFrom-Json).object.sha
    if ($baseHead -notmatch '^[0-9a-fA-F]{40}$') {
        throw "No fue posible resolver HEAD de $Branch."
    }

    $utcStamp = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssZ")
    $dispatchId = "VAEP-JULES-SMOKE-$($baseHead.Substring(0,8))-$utcStamp"
    $dispatchPath = "vaep/jules/dispatch/$dispatchId.json"
    $smokeOutputPath = "vaep/jules/smoke/$dispatchId.txt"

    $manifest = [ordered]@{
        dispatchId      = $dispatchId
        taskId          = "VAEP-JULES-SMOKE"
        expectedBranch  = $Branch
        primaryBaseHead = $baseHead
        fileScopeHint   = $smokeOutputPath
        prompt          = "SMOKE TEST ONLY. Create exactly one new text file '$smokeOutputPath' containing exactly: VAEP Jules smoke test OK - $dispatchId. Do not modify any existing file. Run git diff --check. Return the ChangeSet/gitPatch only; do not create a branch, PR, merge or push."
        createdAt       = [DateTime]::UtcNow.ToString("o")
    } | ConvertTo-Json -Depth 5

    $encodedManifest = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($manifest))
    $requestBody = [ordered]@{
        message = "chore(vaep): dispatch Jules smoke test [Javier]"
        content = $encodedManifest
        branch  = $Branch
    } | ConvertTo-Json -Depth 5

    $tempPayload = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($tempPayload, $requestBody, [Text.UTF8Encoding]::new($false))
        $createJson = Invoke-GhJson @("api", "--method", "PUT", "repos/$Repository/contents/$dispatchPath", "--input", $tempPayload)
    }
    finally {
        Remove-Item $tempPayload -Force -ErrorAction SilentlyContinue
    }

    $dispatchCommit = ($createJson | ConvertFrom-Json).commit.sha
    if ($dispatchCommit -notmatch '^[0-9a-fA-F]{40}$') {
        throw "GitHub creo el manifest, pero no devolvio un commit SHA valido."
    }

    Write-Host "Dispatch: $dispatchId" -ForegroundColor Green
    Write-Host "Manifest commit: $dispatchCommit" -ForegroundColor Green
    Write-Host "Esperando a que GitHub Actions registre el worker..." -ForegroundColor Yellow

    $run = $null
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 4
        $runsJson = Invoke-GhJson @(
            "run", "list",
            "--repo", $Repository,
            "--workflow", "vaep-jules-secondary.yml",
            "--commit", $dispatchCommit,
            "--limit", "1",
            "--json", "databaseId,status,conclusion,url,headSha"
        )
        $runs = @($runsJson | ConvertFrom-Json)
        if ($runs.Count -gt 0) {
            $run = $runs[0]
            break
        }
    }

    if (-not $run) {
        throw "No aparecio un run de vaep-jules-secondary.yml para el dispatch. Revisa GitHub Actions."
    }

    Write-Host "Workflow: $($run.url)" -ForegroundColor Green
    Write-Host "El smoke test puede tardar mientras Jules procesa la sesion. La consola mostrara el progreso de GitHub Actions." -ForegroundColor Yellow

    & gh run watch $run.databaseId --repo $Repository --exit-status
    if ($LASTEXITCODE -ne 0) {
        throw "El smoke test Jules no finalizo correctamente. Revisa el run mostrado arriba; CONFIG.JULES_ENABLED debe permanecer PENDING_EXTERNAL_AUTH."
    }

    Write-Step "Verificando evidencia del smoke test"
    $issueJson = Invoke-GhJson @(
        "issue", "list",
        "--repo", $Repository,
        "--search", "[VAEP-JULES] $dispatchId result in:title",
        "--limit", "1",
        "--json", "number,title,url"
    )
    $issues = @($issueJson | ConvertFrom-Json)
    if ($issues.Count -eq 0) {
        throw "El workflow termino, pero no se encontro el Issue de evidencia [VAEP-JULES]."
    }

    Write-Host "Smoke result: $($issues[0].url)" -ForegroundColor Green
    Write-Host "Smoke dispatch ID: $dispatchId" -ForegroundColor Green
    Write-Host "No se aplico el patch de smoke a Desarrollo; el test valida exclusivamente el canal Jules -> artifact." -ForegroundColor Green
}

Write-Host "`nPRE-FLIGHT JULES VAEP COMPLETADO." -ForegroundColor Green
Write-Host "Repo: $Repository" -ForegroundColor Green
Write-Host "Rama: $Branch" -ForegroundColor Green
Write-Host "Secret: JULES_API_KEY (valor oculto)" -ForegroundColor Green
if ($SkipSmokeTest) {
    Write-Host "Smoke test: OMITIDO por -SkipSmokeTest" -ForegroundColor Yellow
    Write-Host "CONFIG.JULES_ENABLED debe permanecer PENDING_EXTERNAL_AUTH hasta certificar el smoke test." -ForegroundColor Yellow
}
else {
    Write-Host "Smoke test: COMPLETADO" -ForegroundColor Green
    Write-Host "Siguiente gate: reconciliar la evidencia en VAEP y cambiar CONFIG.JULES_ENABLED=TRUE." -ForegroundColor Green
}
