[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$CertificatePath,
    [Parameter(Mandatory)] [string]$CertificateRevocationListPath,
    [Parameter(Mandatory)] [string]$ProjectName,
    [Parameter(Mandatory)] [string]$EnvFile,
    [switch]$ConfirmIsolatedEnvironment
)

$ErrorActionPreference = 'Stop'
if (-not $ConfirmIsolatedEnvironment -or $ProjectName -notmatch '^sweetsecrets-ten033-[a-z0-9-]+$') {
    throw 'La instalación de CA solo se permite con confirmación y un proyecto aislado sweetsecrets-ten033-*.'
}

$certificate = [System.IO.Path]::GetFullPath($CertificatePath)
if (-not (Test-Path -LiteralPath $certificate -PathType Leaf)) { throw 'No existe la CA de prueba.' }
$crl = [System.IO.Path]::GetFullPath($CertificateRevocationListPath)
if (-not (Test-Path -LiteralPath $crl -PathType Leaf)) { throw 'No existe la CRL de prueba.' }
$hashPath = "$crl.hash"
if (-not (Test-Path -LiteralPath $hashPath -PathType Leaf)) { throw 'No existe el hash de la CRL de prueba.' }
$crlHash = (Get-Content -LiteralPath $hashPath -Raw).Trim()
if ($crlHash -notmatch '^[0-9a-f]{8}$') { throw 'El hash de la CRL no es válido.' }
$compose = Join-Path $PSScriptRoot '..\compose.production.yml'
$override = Join-Path $PSScriptRoot 'compose.validation.yml'
$containerId = (& docker compose -p $ProjectName --env-file $EnvFile -f $compose -f $override ps -q api).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($containerId)) { throw 'No se encontró el contenedor API aislado.' }
$actualProject = (& docker inspect --format '{{ index .Config.Labels "com.docker.compose.project" }}' $containerId).Trim()
if ($actualProject -ne $ProjectName) { throw 'El contenedor no pertenece al proyecto aislado confirmado.' }

& docker cp $certificate "${containerId}:/tmp/ten033-ca.crt" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No fue posible copiar la CA pública de prueba.' }
& docker cp $crl "${containerId}:/tmp/ten033-ca.crl" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No fue posible copiar la CRL pública de prueba.' }
& docker exec --user 0 $containerId sh -c "cp /tmp/ten033-ca.crt /usr/local/share/ca-certificates/ten033-ca.crt && update-ca-certificates >/dev/null && cp /tmp/ten033-ca.crl /etc/ssl/certs/$crlHash.r0 && rm -f /tmp/ten033-ca.crt /tmp/ten033-ca.crl"
if ($LASTEXITCODE -ne 0) { throw 'No fue posible actualizar el trust store aislado.' }
& docker restart $containerId | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No fue posible reiniciar la API aislada.' }
Write-Host 'CA pública efímera instalada en el contenedor API aislado.'
