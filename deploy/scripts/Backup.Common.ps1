Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-DockerAvailable {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Docker no está disponible.' }
}

function Invoke-ComposeText {
    param([string]$ComposeFile, [string]$EnvFile, [string]$ProjectName, [string[]]$Arguments)
    $base = @('compose')
    if ($ProjectName) { $base += @('-p', $ProjectName) }
    if ($EnvFile) { $base += @('--env-file', $EnvFile) }
    $base += @('-f', $ComposeFile)
    $output = & docker @base @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "La operación Docker Compose falló para '$($Arguments -join ' ')'." }
    return ($output | Out-String).Trim()
}

function Invoke-DockerText {
    param([string[]]$Arguments, [string]$Operation)
    $output = & docker @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Docker falló durante $Operation." }
    return ($output | Out-String).Trim()
}

function Get-ServiceContainerId {
    param([string]$ComposeFile, [string]$EnvFile, [string]$ProjectName, [string]$Service)
    $id = Invoke-ComposeText $ComposeFile $EnvFile $ProjectName @('ps', '-q', $Service)
    if ([string]::IsNullOrWhiteSpace($id)) { throw "El servicio '$Service' no está creado o ejecutándose." }
    return $id.Trim()
}

function Assert-DatabaseName {
    param([string]$DatabaseName)
    if ($DatabaseName -notmatch '^[a-z][a-z0-9_]{0,62}$') { throw "El nombre de base de datos no es seguro: '$DatabaseName'." }
}

function ConvertTo-SqlLiteral { param([string]$Value) return $Value.Replace("'", "''") }

function Get-ContainedPath {
    param([string]$Root, [string]$RelativePath)
    if ([System.IO.Path]::IsPathRooted($RelativePath)) { throw 'El manifest contiene una ruta absoluta no permitida.' }
    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $Root $RelativePath))
    if (-not $candidate.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'El manifest contiene una ruta fuera del directorio de backup.' }
    return $candidate
}

function Get-BackupFileMetadata {
    param([string]$BackupPath)
    $root = [System.IO.Path]::GetFullPath($BackupPath)
    return @(Get-ChildItem -Path $root -File -Recurse | Where-Object { $_.Name -ne 'manifest.json' } | Sort-Object FullName | ForEach-Object {
        [ordered]@{ Path = [System.IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/'); SizeBytes = $_.Length; Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant() }
    })
}

function Test-BackupManifest {
    param([string]$BackupPath, [switch]$AllowFailed)
    $manifestPath = Join-Path $BackupPath 'manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'No existe manifest.json.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.BackupVersion -ne 1) { throw 'La versión del backup no es compatible.' }
    if (-not $AllowFailed -and $manifest.Status -ne 'Completed') { throw 'El backup no está marcado como completo.' }
    if ([int]$manifest.TenantCount -ne @($manifest.Tenants).Count) { throw 'TenantCount no coincide con la lista de tenants.' }
    $declaredPaths = @($manifest.Files | ForEach-Object Path)
    if (@($declaredPaths | Select-Object -Unique).Count -ne $declaredPaths.Count) { throw 'El manifest contiene archivos duplicados.' }
    $expectedMaster = "master/$($manifest.MasterDatabase).dump"
    if ($expectedMaster -notin $declaredPaths) { throw 'El dump MASTER no está declarado en Files.' }
    foreach ($tenant in $manifest.Tenants) {
        Assert-DatabaseName $tenant.DatabaseName
        if ($tenant.DatabaseName -notmatch '^sweetsecrets_tenant_\d{6}$') { throw "El manifest contiene un DatabaseName tenant no válido: '$($tenant.DatabaseName)'." }
        if ($tenant.File -notin $declaredPaths) { throw "El dump tenant no está declarado en Files: $($tenant.DatabaseName)." }
    }
    if (-not @($declaredPaths | Where-Object { $_ -like 'dataprotection/*' }).Count) { throw 'No hay archivos Data Protection declarados.' }
    $actualPaths = @(Get-BackupFileMetadata $BackupPath | ForEach-Object Path)
    if ($actualPaths.Count -ne $declaredPaths.Count -or @($actualPaths | Where-Object { $_ -notin $declaredPaths }).Count) { throw 'Los archivos presentes no coinciden con el manifest.' }
    foreach ($file in $manifest.Files) {
        $path = Get-ContainedPath $BackupPath $file.Path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Falta un archivo esperado: $($file.Path)." }
        $item = Get-Item -LiteralPath $path
        if ($item.Length -ne [long]$file.SizeBytes) { throw "El tamaño no coincide para: $($file.Path)." }
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash.ToLowerInvariant()
        if ($hash -ne $file.Sha256) { throw "El checksum no coincide para: $($file.Path)." }
    }
    return $manifest
}
