@echo off
title TNR-Booklet
cd /d "%~dp0"

set "EXE=%~dp0src\TnrBooklet\bin\Release\net9.0-windows10.0.19041.0\TNR-Booklet.exe"

where dotnet >nul 2>&1
if errorlevel 1 (
    if exist "%EXE%" goto :run
    echo .NET SDK not found and no built TNR-Booklet.exe.
    echo Install .NET 9 from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Building latest TNR-Booklet...
dotnet build "%~dp0src\TnrBooklet\TnrBooklet.csproj" -c Release -v q
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

:run
if not exist "%EXE%" (
    echo Could not find:
    echo %EXE%
    pause
    exit /b 1
)

start "" "%EXE%"
exit /b 0
