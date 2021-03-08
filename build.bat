@echo off
SETLOCAL ENABLEEXTENSIONS
SETLOCAL ENABLEDELAYEDEXPANSION

set TOOLSVERSION=0.2.7-beta.20210308

echo ==============================================================================
echo RJBUILD
echo ==============================================================================
set START=%TIME%

REM ----------------------------------------------------------------------------
REM Parse arguments
REM ----------------------------------------------------------------------------
REM  /CHECK   - perform checks only. Build/Test will be ignored.
REM  /BUILD   - perform build
REM  /CLEAN   - perform clean
REM  /TEST    - perform tests
REM  /PACKAGE - create NuGet .nupkg files for assemblies
REM  /DOC     - build documentation
REM  /RELEASE - enable release mode
REM  /DIRTY   - allow building with a dirty revision control system
set CHECK=
set CLEAN=
set BUILD=
set TEST=
set PACKAGE=
set DOC=
set RELEASE=
set DIRTY=
set DOTIME=
set LATEST=
set FORCE=

for %%a in (%*) do (
  if /I "%%a" EQU "/CHECK"     set CHECK=true
  if /I "%%a" EQU "/BUILD"     set BUILD=true
  if /I "%%a" EQU "/CLEAN"     set CLEAN=true
  if /I "%%a" EQU "/TEST"      set TEST=true
  if /I "%%a" EQU "/PACKAGE"   set PACKAGE=true
  if /I "%%a" EQU "/DOC"       set DOC=true
  if /I "%%a" EQU "/RELEASE"   set RELEASE=true
  if /I "%%a" EQU "/DIRTY"     set DIRTY=true
  if /I "%%a" EQU "/TIME"      set DOTIME=true
  if /I "%%a" EQU "/LATEST"    set LATEST=true
  if /I "%%a" EQU "/FORCE"     set FORCE=true
)

REM No options performs a build and test by default.
if "%CHECK%" EQU "true" goto :OPTIONCHECK
if "%BUILD%" EQU "true" goto :OPTIONCHECK
if "%PACKAGE%" EQU "true" goto :OPTIONCHECK
if "%CLEAN%" EQU "true" goto :OPTIONCHECK
if "%TEST%" EQU "true" goto :OPTIONCHECK
if "%DOC%" EQU "true" goto :OPTIONCHECK
set BUILD=true
set TEST=true

:OPTIONCHECK
if "%CHECK%" EQU "true" (
  set BUILD=
  set CLEAN=
  set TEST=
  set PACKAGE=
)

if "%CLEAN%" EQU "true" (
  set TEST=
  set PACKAGE=
)

if "%RELEASE%" EQU "true" (
  set "RELFLAG=/r"
  set DOC=true
  set PACKAGE=true
  set BUILD=true
  set TEST=true
  if "%DIRTY%" EQU "true" (
    echo "Mode /RELEASE cannot be set with /DIRTY"
    exit /b
  )
)

REM There are four combinations:
REM  build                        BUILDFLAG=/T
REM  build and test               BUILDFLAG=
REM  build and package            BUILDFLAG=/T /P
REM  build and test and package   BUILDFLAG=/P
set BUILDFLAG=
if "%BUILD%" EQU "true" (
  if "%TEST%" NEQ "true" set BUILDFLAG=/T
  if "%PACKAGE%" EQU "true" (
    set "BUILDFLAG=/P"
    if "%TEST%" NEQ "true" set "BUILDFLAG=/T /P"
  )

  REM clear the flags, as they're no longer needed and part of the BUILDFLAG
  set TEST=

  REM Should only package after a build for consistency.
  set PACKAGE=
)

if "%DIRTY%" EQU "true" set DIRTYFLAG=/d

set TESTFLAG=/i

set TESTFORCE=
if "%FORCE%" EQU "true" set TESTFORCE=/f

REM ----------------------------------------------------------------------------
REM Set up the build environment
REM ----------------------------------------------------------------------------
set "RJTOOLSVERSION=%CD%\Tools\A20413A"
if "%LATEST%" EQU "true" set TOOLSVERSION=latest
echo Tools: %TOOLSVERSION%
set "RJTOOLSVERSION=%CD%\Tools\%TOOLSVERSION%"

if not exist "%RJTOOLSVERSION%" (
  svn co https://svn.home.lan/binaries/dotnet/rjbuild/%TOOLSVERSION% Tools/%TOOLSVERSION%
  if not exist "%RJTOOLSVERSION%" (
    echo. Tools not found. Please update your build environment.
    exit /b
  )
)

REM The following variables tell where the required tools can be found, if
REM they're not in the path, or if you want to specify precisely which tools
REM should be used
REM
REM RJBUILD_GITPROVIDER=<path to git.exe>
REM RJBUILD_BUILDPROVIDER=<path to msbuild.exe>
REM RJBUILD_NUGETPROVIDER=<path to nuget.exe 4.3 or later>

reg Query "HKLM\Hardware\Description\System\CentralProcessor\0" | find /i "x86" > NUL && set OS=32BIT || set OS=64BIT

if %OS%==32BIT goto :OS32VARS
if %OS%==64BIT goto :OS64VARS

:OS32VARS
echo Machine: 32-bit
set "RJBUILD_NUGETPROVIDER=%RJTOOLSVERSION%\nuget.exe"
if exist "C:\Program Files\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\bin\msbuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2019\Professional\MSBuild\Current\bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files\Microsoft Visual Studio\2019\Professional\MSBuild\Current\bin\msbuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\bin\msbuild.exe"
)
set "RJBUILD_NUNIT2PROVIDER=C:\Program Files\NUnit 2.6.4\bin\nunit-console.exe"
goto :RJBUILDVARS

:OS64VARS
echo Machine: 64-bit
set "RJBUILD_NUGETPROVIDER=%RJTOOLSVERSION%\nuget.exe"
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild.exe"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\msbuild.exe"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\bin\msbuild.exe" (
  set "RJBUILD_BUILDPROVIDER=C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\bin\msbuild.exe"
)
set "RJBUILD_NUNIT2PROVIDER=C:\Program Files (x86)\NUnit 2.6.4\bin\nunit-console.exe"
goto :RJBUILDVARS

:RJBUILDVARS
set "RJBUILD=%RJTOOLSVERSION%\RjBuild\RjBuild.exe"
set "PROJECT=%CD%\rjcp.rjproj"

REM If the local machine has two specific paths, we'll use that for NUnit2 tests
REM which enhance the test quality of the test cases. This can ensure that test
REM cases don't write to the TestDirectory (M: is readonly) and output is
REM written to a temp drive R:, which could be a RAMDISK
if not exist M:\distribute goto :TESTMODECHECKEND
if not exist R: goto :TESTMODECHECKEND

echo Test Mode: Enhanced testing enabled. Using M:\distribute and R:
set RJBUILD_NUNIT2CWD=R:\
set RJBUILD_NUNIT2WORKBASE=R:\
set RJBUILD_NUNIT2DEPLOYBASE=M:\distribute
set RJBUILD_NUNIT3CWD=R:\
set RJBUILD_NUNIT3WORKBASE=R:\
set RJBUILD_NUNIT3DEPLOYBASE=M:\distribute

:TESTMODECHECKEND
echo.


:CHECK
if "%CHECK%" NEQ "true" goto :CLEAN
echo ==============================================================================
echo RJBUILD check
echo ==============================================================================
"%RJBUILD%" check %RELFLAG% "%PROJECT%" || goto :FINISH
echo.
goto :FINISH

:CLEAN
if "%CLEAN%" NEQ "true" goto :BUILD
echo ==============================================================================
echo RJBUILD clean
echo ==============================================================================
"%RJBUILD%" clean "%PROJECT%" || goto :FINISH
echo.

:BUILD
if "%BUILD%" NEQ "true" goto :TEST
echo ==============================================================================
echo RJBUILD build
echo ==============================================================================
"%RJBUILD%" build %BUILDFLAG% %DIRTYFLAG% %TESTFLAG% %TESTFORCE% %RELFLAG% "%PROJECT%" || goto :FINISH
echo.

:TEST
if "%TEST%" NEQ "true" goto :PACKAGE
echo ==============================================================================
echo RJBUILD test
echo ==============================================================================
"%RJBUILD%" test %DIRTYFLAG% %TESTFLAG% %TESTFORCE% "%PROJECT%" || goto :FINISH
echo.

:PACKAGE
if "%PACKAGE%" NEQ "true" goto :DOC
echo ==============================================================================
echo RJBUILD package
echo ==============================================================================
"%RJBUILD%" package %DIRTYFLAG% %RELFLAG% "%PROJECT%" || goto :FINISH
echo.

:DOC
if "%DOC%" NEQ "true" goto :FINISH
echo ==============================================================================
echo Generate documentation
echo ==============================================================================
echo For .NET 4.5
echo ------------------------------------------------------------------------------
"%RJBUILD_BUILDPROVIDER%" rjcp.shfbproj /t:Rebuild /verbosity:minimal /p:Configuration=Release /p:OutputPath="%CD%\distribute\Help\net45" /nologo
echo.

:FINISH
if "%DOTIME%" NEQ "true" goto :FINISHNOTIME

set END=%TIME%
set options="tokens=1-4 delims=:.,"
for /f %options% %%a in ("%start%") do set start_h=%%a&set /a start_m=100%%b %% 100&set /a start_s=100%%c %% 100&set /a start_ms=100%%d %% 100
for /f %options% %%a in ("%end%") do set end_h=%%a&set /a end_m=100%%b %% 100&set /a end_s=100%%c %% 100&set /a end_ms=100%%d %% 100

set /a hours=%end_h%-%start_h%
set /a mins=%end_m%-%start_m%
set /a secs=%end_s%-%start_s%
set /a ms=%end_ms%-%start_ms%
if %ms% lss 0 set /a secs = %secs% - 1 & set /a ms = 100%ms%
if %secs% lss 0 set /a mins = %mins% - 1 & set /a secs = 60%secs%
if %mins% lss 0 set /a hours = %hours% - 1 & set /a mins = 60%mins%
if %hours% lss 0 set /a hours = 24%hours%
if 1%ms% lss 100 set ms=0%ms%

set /a totalsecs = %hours%*3600 + %mins%*60 + %secs%
echo ==============================================================================
echo Execution time: %totalsecs%.%ms%s

:FINISHNOTIME
exit /b 0