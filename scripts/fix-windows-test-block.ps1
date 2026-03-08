# Fix Windows blocking .NET test assemblies (0x800711C7)
# Run as Administrator for full effect.
# See docs/TROUBLESHOOTING.md for manual steps.

param(
    [switch]$UnblockOnly,
    [switch]$AddExclusion
)

$ErrorActionPreference = "Continue"
$projectRoot = Split-Path -Parent $PSScriptRoot  # WhaleWire folder

Write-Host "WhaleWire: Fixing Windows test block (0x800711C7)" -ForegroundColor Cyan
Write-Host "Project root: $projectRoot" -ForegroundColor Gray
Write-Host ""

# 1. Unblock all files (removes Zone.Identifier from downloaded files)
Write-Host "[1/3] Unblocking files..." -ForegroundColor Yellow
$count = 0
Get-ChildItem -Path $projectRoot -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    try {
        Unblock-File -Path $_.FullName -ErrorAction SilentlyContinue
        $count++
    } catch {}
}
Write-Host "  Unblocked $count files" -ForegroundColor Green

# 2. Add Windows Defender exclusion (requires Admin)
if ($AddExclusion -or -not $UnblockOnly) {
    Write-Host "[2/3] Adding Windows Defender exclusion..." -ForegroundColor Yellow
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if ($isAdmin) {
        try {
            Add-MpPreference -ExclusionPath $projectRoot -ErrorAction Stop
            Write-Host "  Added exclusion for: $projectRoot" -ForegroundColor Green
        } catch {
            Write-Host "  Could not add exclusion: $_" -ForegroundColor Red
            Write-Host "  Run: Add-MpPreference -ExclusionPath '$projectRoot'" -ForegroundColor Gray
        }
    } else {
        Write-Host "  Run as Administrator to add Defender exclusion." -ForegroundColor Yellow
        Write-Host "  Or run manually: Add-MpPreference -ExclusionPath '$projectRoot'" -ForegroundColor Gray
    }
}

# 3. Remind about Smart App Control
Write-Host "[3/3] Smart App Control check" -ForegroundColor Yellow
Write-Host "  If tests still fail, try:" -ForegroundColor Gray
Write-Host "  - Settings > Privacy & security > Windows Security > App & browser control" -ForegroundColor Gray
Write-Host "  - Smart App Control settings > Turn Off" -ForegroundColor Gray
Write-Host "  - Restart, then: dotnet clean; dotnet build; dotnet test" -ForegroundColor Gray
Write-Host ""
Write-Host "Done. Run: dotnet clean; dotnet build; dotnet test" -ForegroundColor Cyan
