@echo off
setlocal enabledelayedexpansion

rem ============================================================
rem  Library Management System - launcher
rem  Runs the exe in src\LibraryMS\bin\Debug\net48 (that folder is the
rem  only one with a matching LibraryMS.exe.config).
rem
rem  Build order: dotnet build (if a .NET SDK is installed)
rem              -> MSBuild 14 csc.exe (VS2015 / .NET Framework 4.8)
rem
rem  Output sanity check: a normal WinForms build is ~150 KB.  If the file
rem  is far bigger than the one the compiler just produced in obj\, or it is
rem  outside 40 KB - 600 KB, something rewrote it (in the lab this is usually
rem  a file-infector virus) - so it is deleted and rebuilt instead of run.
rem
rem  WinExe: running this script opens the login window; the console part of
rem  this script is only used for building.
rem ============================================================

cd /d "%~dp0"

set "SRC=src\LibraryMS"
set "PROJ=%SRC%\LibraryMS.csproj"
set "BIN=%SRC%\bin\Debug\net48"
set "EXE=%BIN%\LibraryMS.exe"
set "OBJEXE=%SRC%\obj\Debug\net48\LibraryMS.exe"
set "CFG=%BIN%\LibraryMS.exe.config"
set "MINSIZE=40000"
set "MAXSIZE=600000"

rem ---- decide whether the existing exe is usable ----
set "NEEDBUILD=1"
if not exist "%EXE%" goto dobuild
for %%A in ("%EXE%") do set "EXESIZE=%%~zA"
if not defined EXESIZE goto dobuild
if !EXESIZE! LSS %MINSIZE% (
    echo [WARN] Existing exe is too small ^(!EXESIZE! bytes^), rebuilding...
    goto dobuild
)
if !EXESIZE! GTR %MAXSIZE% (
    echo [WARN] Existing exe looks corrupted ^(!EXESIZE! bytes, expected about 150 KB^).
    echo        Deleting it and rebuilding. Run a full antivirus scan afterwards.
    del /f /q "%EXE%" >nul 2>nul
    goto dobuild
)
if exist "%OBJEXE%" (
    for %%B in ("%OBJEXE%") do set "OBJSIZE=%%~zB"
    set /a "SIZELIMIT=OBJSIZE*2"
    if !EXESIZE! GTR !SIZELIMIT! (
        echo [WARN] exe ^(!EXESIZE! bytes^) is much larger than the last compile output
        echo        ^(!OBJSIZE! bytes^). It was probably rewritten by a file-infector.
        echo        Deleting it and rebuilding. Run a full antivirus scan afterwards.
        del /f /q "%EXE%" >nul 2>nul
        goto dobuild
    )
)
set "NEEDBUILD=0"

:dobuild
if "%NEEDBUILD%"=="1" (
    echo [INFO] Building project...
    call :build
    if errorlevel 1 goto fail
)

if not exist "%EXE%" (
    echo [ERROR] Build finished but "%EXE%" is still missing.
    pause
    exit /b 1
)

rem ---- make sure the config sits next to the exe ----
if not exist "%CFG%" (
    if exist "%SRC%\App.config" copy /y "%SRC%\App.config" "%CFG%" >nul
)

:run
if not exist "%BIN%" (
    echo [ERROR] Output directory not found: "%BIN%"
    pause
    exit /b 1
)
"%EXE%"
exit /b 0

rem ============================================================
rem  :build  -  try dotnet build first, then fall back to csc.exe
rem  returns errorlevel 0 on success, 1 on failure
rem ============================================================
:build
where dotnet >nul 2>nul
if errorlevel 1 goto buildcsc

dotnet build "%PROJ%" -v q
if not errorlevel 1 (
    if exist "%EXE%" exit /b 0
)
echo [WARN] dotnet build did not produce a usable exe, falling back to csc.exe ...

:buildcsc
set "CSC=C:\Program Files (x86)\MSBuild\14.0\Bin\csc.exe"
if not exist "%CSC%" (
    echo [ERROR] Neither .NET SDK ^(dotnet^) nor MSBuild 14 csc.exe was found.
    echo         Install .NET Framework 4.8 Developer Pack, or copy a prebuilt
    echo         "%BIN%" folder from another machine and run this script again.
    exit /b 1
)

if not exist "%BIN%" mkdir "%BIN%"

set "RSP=%TEMP%\libraryms_sources.rsp"
if exist "%RSP%" del "%RSP%" >nul
for /r "%SRC%" %%F in (*.cs) do echo "%%F">> "%RSP%"
set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
for %%R in (mscorlib System System.Core System.Data System.Xml System.Configuration System.Drawing System.Windows.Forms) do echo /r:"!FW!\%%R.dll">> "%RSP%"

echo [INFO] Compiling with csc.exe (C# 6, .NET Framework 4.8 runtime, WinForms)...
"%CSC%" /nologo /target:winexe /langversion:6 /nostdlib+ /codepage:65001 /debug:pdbonly /utf8output /out:"%EXE%" "@%RSP%"
set "RC=%ERRORLEVEL%"
del "%RSP%" >nul 2>nul
if not "%RC%"=="0" (
    echo [ERROR] csc.exe build failed. Please check the errors above.
    exit /b 1
)
exit /b 0

:fail
pause
exit /b 1
