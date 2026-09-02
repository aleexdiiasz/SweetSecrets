[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$BackupPath,
    [ValidateSet('Full', 'Tenant')] [string]$Mode = 'Full',
    [string]$TenantDatabase = '',
    [string]$ComposeFile = (Join-Path $PSScriptRoot '..\compose.production.yml'),
    [string]$EnvFile = (Join-Path $PSScriptRoot '..\.env'),
    [string]$ProjectName = '',
    [string]$MasterTargetDatabase = 'sweetsecrets_restore_master',
    [string]$TenantTargetPrefix = 'sweetsecrets_restore_',
    [string]$DataProtectionTargetPath = ''
)
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
Assert-DockerAvailable
$root = [System.IO.Path]::GetFullPath($BackupPath)
try {
    $manifest = Test-BackupManifest $root
    if ($Mode -eq 'Full' -and [string]::IsNullOrWhiteSpace($DataProtectionTargetPath)) { throw 'Restore Full requiere DataProtectionTargetPath hacia una ubicación aislada y vacía.' }
    if ($Mode -eq 'Tenant' -and [string]::IsNullOrWhiteSpace($TenantDatabase)) { throw 'Restore Tenant requiere TenantDatabase.' }
    $postgresId = Get-ServiceContainerId $ComposeFile $EnvFile $ProjectName 'postgres'
    $postgresUser = Invoke-DockerText @('exec', $postgresId, 'printenv', 'POSTGRES_USER') 'la lectura del usuario PostgreSQL'
    function Restore-Database([string]$SourceFile, [string]$TargetDatabase, [string[]]$RequiredTables) {
        Assert-DatabaseName $TargetDatabase
        $literal = ConvertTo-SqlLiteral $TargetDatabase
        $exists = Invoke-DockerText @('exec', $postgresId, 'psql', '-X', '-U', $postgresUser, '-d', 'postgres', '-At', '-v', 'ON_ERROR_STOP=1', '-c', "SELECT 1 FROM pg_database WHERE datname = '$literal';") "la validación del destino '$TargetDatabase'"
        if ($exists -eq '1') { throw "El destino '$TargetDatabase' ya existe; no se sobrescribirá." }
        $temp = "/tmp/sweetsecrets-restore-$([Guid]::NewGuid().ToString('N')).dump"
        try {
            [void](Invoke-DockerText @('cp', $SourceFile, "${postgresId}:$temp") "la preparación de '$TargetDatabase'")
            [void](Invoke-DockerText @('exec', $postgresId, 'createdb', '-U', $postgresUser, $TargetDatabase) "la creación de '$TargetDatabase'")
            [void](Invoke-DockerText @('exec', $postgresId, 'pg_restore', '-U', $postgresUser, '-d', $TargetDatabase, '--exit-on-error', '--no-owner', '--no-privileges', $temp) "el restore de '$TargetDatabase'")
            foreach ($table in $RequiredTables) {
                $tableLiteral = ConvertTo-SqlLiteral $table
                $present = Invoke-DockerText @('exec', $postgresId, 'psql', '-X', '-U', $postgresUser, '-d', $TargetDatabase, '-At', '-v', 'ON_ERROR_STOP=1', '-c', "SELECT to_regclass('public.$tableLiteral') IS NOT NULL;") "la validación de schema en '$TargetDatabase'"
                if ($present -ne 't') { throw "La tabla requerida '$table' no existe después del restore de '$TargetDatabase'." }
            }
            $migrationCount = Invoke-DockerText @('exec', $postgresId, 'psql', '-X', '-U', $postgresUser, '-d', $TargetDatabase, '-At', '-v', 'ON_ERROR_STOP=1', '-c', 'SELECT COUNT(*) FROM "__EFMigrationsHistory";') "la validación de migrations en '$TargetDatabase'"
            if ([int]$migrationCount -lt 1) { throw "'$TargetDatabase' no contiene historial de migrations." }
            Write-Host "Restore validado: $TargetDatabase (migrations: $migrationCount)"
        } finally { & docker exec $postgresId rm -f $temp 2>$null | Out-Null }
    }
    $tenantTables = @('products', 'recipes', 'recipe_items', 'units', 'settings', 'product_price_history', 'recipe_cost_history')
    if ($Mode -eq 'Full') {
        Restore-Database (Get-ContainedPath $root "master/$($manifest.MasterDatabase).dump") $MasterTargetDatabase @('tenants', 'platform_users', 'platform_roles', 'user_sessions', 'platform_audit_logs')
        foreach ($tenant in $manifest.Tenants) { Restore-Database (Get-ContainedPath $root $tenant.File) "$TenantTargetPrefix$($tenant.DatabaseName)" $tenantTables }
        $targetPath = [System.IO.Path]::GetFullPath($DataProtectionTargetPath)
        if (Test-Path -LiteralPath $targetPath) { if (Get-ChildItem -LiteralPath $targetPath -Force | Select-Object -First 1) { throw 'DataProtectionTargetPath debe estar vacío.' } } else { New-Item -ItemType Directory -Path $targetPath -Force | Out-Null }
        Get-ChildItem -LiteralPath (Join-Path $root 'dataprotection') -Force | Copy-Item -Destination $targetPath -Recurse
        Write-Host 'Data Protection fue restaurado en la ubicación aislada indicada.'
    } else {
        $tenant = @($manifest.Tenants | Where-Object DatabaseName -eq $TenantDatabase)
        if ($tenant.Count -ne 1) { throw "El tenant '$TenantDatabase' no existe de forma única en el manifest." }
        Restore-Database (Get-ContainedPath $root $tenant[0].File) "$TenantTargetPrefix$TenantDatabase" $tenantTables
    }
} catch { Write-Error $_.Exception.Message; exit 1 }
