@echo off
@setlocal

set Root=%~dp0..\..\..\..\Open

REM Load NuGet variable information
call "%Root%\build\scripts\LoadNuGetInfo.cmd" || goto :RestoreFailed

set NuGetAdditionalCommandLineArgs=-verbosity quiet -configfile "%Root%\nuget.config" -Project2ProjectTimeOut 1200

call %NugetExe% restore "%~dp0RoslynInsertionTool.sln" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

exit /b 0

:RestoreFailed
echo Restore failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1
