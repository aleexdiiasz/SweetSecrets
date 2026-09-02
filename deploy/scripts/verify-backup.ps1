[CmdletBinding()]
param([Parameter(Mandatory)] [string]$BackupPath)
. (Join-Path $PSScriptRoot 'Backup.Common.ps1')
try {
    $manifest = Test-BackupManifest ([System.IO.Path]::GetFullPath($BackupPath))
    Write-Host "Backup válido: $($manifest.TimestampUtc)"
    Write-Host "Tenants: $($manifest.TenantCount); archivos verificados: $($manifest.Files.Count)"
} catch { Write-Error $_.Exception.Message; exit 1 }
