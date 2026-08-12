$ErrorActionPreference = "Stop"

$ExpectedRepo = "jmejia31/VariApp"
$ExpectedOrigins = @(
    "https://github.com/jmejia31/VariApp",
    "https://github.com/jmejia31/VariApp.git",
    "git@github.com:jmejia31/VariApp.git",
    "ssh://git@github.com/jmejia31/VariApp.git"
)

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Falló el comando: git $($Arguments -join ' ')"
    }
}

function Invoke-GitCapture {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Falló el comando: git $($Arguments -join ' ')"
    }
    return (($output | Out-String).Trim())
}

try {
    Invoke-Git @("rev-parse", "--is-inside-work-tree")

    $originUrl = Invoke-GitCapture @("remote", "get-url", "origin")
    if ($ExpectedOrigins -notcontains $originUrl) {
        throw "PROJECT GUARD: origin '$originUrl' no corresponde a '$ExpectedRepo'. Se abortó sin modificar configuración."
    }

    $pending = (& git status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo consultar el estado del repositorio."
    }

    if ($pending) {
        throw "Hay cambios locales pendientes. Confírmalos o resuélvelos antes de configurar la colaboración. No se descartó ningún archivo."
    }

    Invoke-Git @("fetch", "origin", "--prune")

    & git show-ref --verify --quiet refs/heads/Desarrollo
    $localBranchExists = $LASTEXITCODE -eq 0

    if ($localBranchExists) {
        Invoke-Git @("switch", "Desarrollo")
    }
    else {
        Invoke-Git @("switch", "--create", "Desarrollo", "--track", "origin/Desarrollo")
    }

    Invoke-Git @("pull", "--rebase", "origin", "Desarrollo")
    Invoke-Git @("config", "core.hooksPath", ".githooks")
    Invoke-Git @("config", "pull.rebase", "true")
    Invoke-Git @("config", "fetch.prune", "true")
    Invoke-Git @("config", "push.autoSetupRemote", "true")

    $sessionScript = Join-Path $PSScriptRoot "iniciar-sesion-ia.ps1"
    if (Test-Path -LiteralPath $sessionScript -PathType Leaf) {
        & $sessionScript -NoFetch
        if ($LASTEXITCODE -ne 0) {
            throw "El gate de sesión detectó una inconsistencia después de configurar colaboración."
        }
    }

    Write-Host ""
    Write-Host "Colaboración configurada correctamente." -ForegroundColor Green
    Write-Host "Proyecto confirmado: VARIAPP / $ExpectedRepo"
    Write-Host "Rama activa: Desarrollo"
    Write-Host "Hooks activos: pre-commit (identidad + evidencia) y post-commit (push seguro)."
    Write-Host "Main permanece congelada y no se fusionará automáticamente."
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
