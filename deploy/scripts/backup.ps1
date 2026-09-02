[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$OutputRoot,
    [string]$ComposeFile = (Join-Path $PSScriptRoot '..\compose.production.yml'),
    [string]$EnvFile = (Join-Path $PSScriptRoot '..\.env'),
    [string]$ProjectName = '',
    [string]$MasterDatabaseName = 'sweetsecrets_master',
    [string]$DataProtectionSourcePath = '',
    [string]$DataProtectionContainerPath = '/home/app/.aspnet/DataProtection-Keys'
)
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
Assert-DockerAvailable
Assert-DatabaseName $MasterDatabaseName
$timestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHHmmssZ')
$backupPath = Join-Path ([System.IO.Path]::GetFullPath($OutputRoot)) $timestamp
if (Test-Path -LiteralPath $backupPath) { throw "El directorio de backup ya existe: $backupPath" }
$manifest = [ordered]@{ BackupVersion = 1; Status = 'InProgress'; TimestampUtc = [DateTime]::UtcNow.ToString('o'); CompletedAtUtc = $null; PostgreSqlVersion = $null; SweetSecretsVersion = 'TEN-032'; MasterDatabase = $MasterDatabaseName; TenantCount = 0; Tenants = @(); Files = @(); Errors = @() }
New-Item -ItemType Directory -Path (Join-Path $backupPath 'master'), (Join-Path $backupPath 'tenants'), (Join-Path $backupPath 'dataprotection') -Force | Out-Null
try {
    $postgresId = Get-ServiceContainerId $ComposeFile $EnvFile $ProjectName 'postgres'
    $postgresUser = Invoke-DockerText @('exec', $postgresId, 'printenv', 'POSTGRES_USER') 'la lectura del usuario PostgreSQL'
    if ([string]::IsNullOrWhiteSpace($postgresUser)) { throw 'El usuario PostgreSQL no está configurado en el contenedor.' }
    $manifest.PostgreSqlVersion = Invoke-DockerText @('exec', $postgresId, 'pg_dump', '--version') 'la lectura de versión PostgreSQL'
    function Invoke-DatabaseDump([string]$DatabaseName, [string]$Destination) {
        Assert-DatabaseName $DatabaseName
        $literal = ConvertTo-SqlLiteral $DatabaseName
        $exists = Invoke-DockerText @('exec', $postgresId, 'psql', '-X', '-U', $postgresUser, '-d', 'postgres', '-At', '-v', 'ON_ERROR_STOP=1', '-c', "SELECT 1 FROM pg_database WHERE datname = '$literal';") "la validación de '$DatabaseName'"
        if ($exists -ne '1') { throw "La base requerida '$DatabaseName' no existe." }
        $temp = "/tmp/sweetsecrets-backup-$([Guid]::NewGuid().ToString('N')).dump"
        try {
            [void](Invoke-DockerText @('exec', $postgresId, 'pg_dump', '-U', $postgresUser, '-d', $DatabaseName, '-Fc', '--no-owner', '--file', $temp) "el backup de '$DatabaseName'")
            [void](Invoke-DockerText @('cp', "${postgresId}:$temp", $Destination) "la copia del backup de '$DatabaseName'")
        } finally { & docker exec $postgresId rm -f $temp 2>$null | Out-Null }
    }
    Invoke-DatabaseDump $MasterDatabaseName (Join-Path $backupPath "master\$MasterDatabaseName.dump")
    $tenantRows = Invoke-DockerText @('exec', $postgresId, 'psql', '-X', '-U', $postgresUser, '-d', $MasterDatabaseName, '-At', '-F', '|', '-v', 'ON_ERROR_STOP=1', '-c', 'SELECT "DatabaseName", "Status" FROM tenants ORDER BY "DatabaseName";') 'el descubrimiento de tenants desde MASTER'
    $tenants = @()
    if (-not [string]::IsNullOrWhiteSpace($tenantRows)) {
        foreach ($row in ($tenantRows -split "`r?`n")) {
            $parts = $row.Split('|')
            if ($parts.Count -ne 2) { throw 'MASTER devolvió metadata tenant no válida.' }
            $databaseName = $parts[0]
            if ($databaseName -notmatch '^sweetsecrets_tenant_\d{6}$') { throw "MASTER contiene un DatabaseName tenant no válido: '$databaseName'." }
            $status = switch ([int]$parts[1]) { 1 {'Provisioning'} 2 {'Active'} 3 {'Suspended'} 4 {'Disabled'} 5 {'Failed'} default { throw "MASTER contiene un estado tenant desconocido para '$databaseName'." } }
            $tenants += [ordered]@{ DatabaseName = $databaseName; Status = $status; File = "tenants/$databaseName.dump" }
        }
    }
    $manifest.Tenants = $tenants; $manifest.TenantCount = $tenants.Count
    foreach ($tenant in $tenants) { Invoke-DatabaseDump $tenant.DatabaseName (Join-Path $backupPath "tenants\$($tenant.DatabaseName).dump") }
    $dpDestination = Join-Path $backupPath 'dataprotection'
    if ($DataProtectionSourcePath) {
        if (-not (Test-Path -LiteralPath $DataProtectionSourcePath -PathType Container)) { throw 'La ruta origen de Data Protection no existe.' }
        Get-ChildItem -LiteralPath $DataProtectionSourcePath -Force | Copy-Item -Destination $dpDestination -Recurse
    } else {
        $apiId = Get-ServiceContainerId $ComposeFile $EnvFile $ProjectName 'api'
        [void](Invoke-DockerText @('cp', "${apiId}:$DataProtectionContainerPath/.", $dpDestination) 'la copia de Data Protection')
    }
    if (-not (Get-ChildItem -LiteralPath $dpDestination -File -Recurse | Select-Object -First 1)) { throw 'No se encontraron archivos de Data Protection para respaldar.' }
    $manifest.Files = Get-BackupFileMetadata $backupPath; $manifest.Status = 'Completed'; $manifest.CompletedAtUtc = [DateTime]::UtcNow.ToString('o')
} catch {
    $manifest.Status = 'Failed'; $manifest.CompletedAtUtc = [DateTime]::UtcNow.ToString('o'); $manifest.Errors = @($_.Exception.Message); $manifest.Files = Get-BackupFileMetadata $backupPath
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backupPath 'manifest.json') -Encoding utf8
    Write-Error $_.Exception.Message; exit 1
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backupPath 'manifest.json') -Encoding utf8
Write-Host "Backup completo validable creado en: $backupPath"
Write-Host "Bases tenant incluidas: $($manifest.TenantCount)"
