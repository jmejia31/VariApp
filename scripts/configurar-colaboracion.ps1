$ErrorActionPreference = "Stop"

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Falló el comando: git $($Arguments -join ' ')"
    }
}

try {
    Invoke-Git @("rev-parse", "--is-inside-work-tree")

    $pending = (& git status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo consultar el estado del repositorio."
    }

    if ($pending) {
        throw "Hay cambios locales pendientes. Confírmalos o guárdalos antes de configurar la colaboración. No se descartó ningún archivo."
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

    Write-Host ""
    Write-Host "Colaboración configurada correctamente." -ForegroundColor Green
    Write-Host "Rama activa: Desarrollo"
    Write-Host "Cada commit en Desarrollo intentará publicarse automáticamente en GitHub."
    Write-Host "Main permanece sin cambios y no se fusionará automáticamente."
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
