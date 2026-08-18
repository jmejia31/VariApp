[CmdletBinding()]
param(
    [string]$Repository = "jmejia31/VariApp",
    [string]$Branch = "Desarrollo"
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
$secretNames = gh secret list --repo $Repository --json name --jq '.[].name'
if ($LASTEXITCODE -ne 0 -or ($secretNames -notcontains "JULES_API_KEY")) {
    throw "GitHub no reporta el secret JULES_API_KEY."
}

Write-Host "`nPRE-FLIGHT JULES VAEP COMPLETADO." -ForegroundColor Green
Write-Host "Repo: $Repository" -ForegroundColor Green
Write-Host "Rama: $Branch" -ForegroundColor Green
Write-Host "Secret: JULES_API_KEY (valor oculto)" -ForegroundColor Green
Write-Host "Siguiente gate: smoke test del workflow VAEP Jules y activacion de CONFIG.JULES_ENABLED=TRUE." -ForegroundColor Green
