#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the clean Windows x64 release archive from Build/.

.DESCRIPTION
    Copies the portable .bat launcher and memo into Build/, removes stale
    shortcuts, packs only the game files (no QA build, logs or screenshots) and
    prints the archive size and SHA256 for the GitHub release notes.
#>
param(
    [string]$BuildDir = (Join-Path $PSScriptRoot '..\Build'),
    [string]$OutputZip = (Join-Path $PSScriptRoot '..\CardboardDungeon_v1.5.0_win64.zip'),
    [string]$ExeName = 'CardboardDungeon.exe'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$BuildDir = (Resolve-Path $BuildDir).Path

if (-not (Test-Path -LiteralPath (Join-Path $BuildDir $ExeName))) {
    throw "Game executable not found in $BuildDir"
}

# --- launchers -------------------------------------------------------------
$launcherDir = Join-Path $root 'tools\launcher'
$launcherFiles = @(Get-ChildItem -LiteralPath $launcherDir -File |
    Where-Object { $_.Extension -in '.bat', '.txt' })
if ($launcherFiles.Count -eq 0) {
    throw "No launcher files found in $launcherDir"
}

foreach ($file in $launcherFiles) {
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $BuildDir $file.Name) -Force
    Write-Host "Staged launcher: $($file.Name)"
}

# Shortcuts are not portable between machines, so the release ships only
# the .bat launcher and the .exe. Remove stale links from earlier staging.
Get-ChildItem -LiteralPath $BuildDir -Filter '*.lnk' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

$launcherNames = @($launcherFiles.Name)

# --- collect release files -------------------------------------------------
$excludedDirs = @('qa', 'qa_shots')
$excludedExtensions = @('.log', '.lnk')
$excludedNames = @('desktop.ini', 'thumbs.db')

$files = Get-ChildItem -LiteralPath $BuildDir -Recurse -File | Where-Object {
    $relative = $_.FullName.Substring($BuildDir.Length + 1)
    $topDir = ($relative -split '[\\/]')[0]
    if ($excludedDirs -contains $topDir) { return $false }
    if ($excludedExtensions -contains $_.Extension.ToLowerInvariant()) { return $false }
    if ($excludedNames -contains $_.Name.ToLowerInvariant()) { return $false }
    if ($_.Name -like 'screenshot_*.png') { return $false }
    return $true
}

# Launchers first so they are easy to spot in the archive.
$files = @(
    $files | Where-Object { $launcherNames -contains $_.Name } | Sort-Object Name
) + @(
    $files | Where-Object { $launcherNames -notcontains $_.Name } | Sort-Object FullName
)

if (Test-Path -LiteralPath $OutputZip) {
    Remove-Item -LiteralPath $OutputZip -Force
}

$zip = [IO.Compression.ZipFile]::Open($OutputZip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($BuildDir.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            $file.FullName,
            $relative,
            [IO.Compression.CompressionLevel]::Optimal)
    }
}
finally {
    $zip.Dispose()
}

# --- report ----------------------------------------------------------------
$item = Get-Item -LiteralPath $OutputZip
$sha = (Get-FileHash -LiteralPath $OutputZip -Algorithm SHA256).Hash
Write-Host ''
Write-Host "Archive: $($item.FullName)"
Write-Host ("Size:    {0} bytes ({1:N1} MB)" -f $item.Length, ($item.Length / 1MB))
Write-Host "Entries: $($files.Count)"
Write-Host "SHA256:  $sha"
