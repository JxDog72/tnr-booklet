# Puts TNR-Booklet in Windows Search the same way sysmonbar is found:
# a desktop shortcut whose name starts with tnr-booklet, plus a Start Menu entry.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $root "src\TnrBooklet\bin\Release\net9.0-windows10.0.19041.0\TNR-Booklet.exe"
if (-not (Test-Path $exe)) {
    Write-Host "TNR-Booklet.exe not built yet; skip search shortcut."
    exit 0
}

function New-TnrShortcut([string]$path, [string]$target) {
    $folder = Split-Path -Parent $path
    if (-not (Test-Path $folder)) {
        New-Item -ItemType Directory -Path $folder -Force | Out-Null
    }
    $shell = New-Object -ComObject WScript.Shell
    $lnk = $shell.CreateShortcut($path)
    $lnk.TargetPath = $target
    $lnk.WorkingDirectory = $root
    $lnk.WindowStyle = 1
    $lnk.Description = "TNR-Booklet - todos, notes, reminders"
    $lnk.Save()
}

$desktops = @(
    [Environment]::GetFolderPath("Desktop")
    (Join-Path $env:USERPROFILE "OneDrive\Desktop")
    (Join-Path $env:USERPROFILE "Desktop")
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique
$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"

foreach ($desktop in $desktops) {
    New-TnrShortcut (Join-Path $desktop "tnr-booklet.exe - Shortcut.lnk") $exe
}
New-TnrShortcut (Join-Path $startMenu "TNR-Booklet.lnk") $exe

Write-Host "Windows Search shortcuts ready (Desktop + Start Menu)."
