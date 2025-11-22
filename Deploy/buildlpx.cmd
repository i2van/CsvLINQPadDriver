@echo off

set version=8.15.0
set fileName=CsvLINQPadDriver.%version%
set ext=lpx
set ext6=%ext%6
set rootDir=%~dp0
set rootDir=%rootDir:~0,-1%

set zip="%ProgramFiles%\7-Zip\7z.exe"

echo on

call :pack %fileName%-net10.%ext6% net10.0-windows || goto break
call :pack %fileName%-net9.%ext6%  net9.0-windows  || goto break
call :pack %fileName%-net8.%ext6%  net8.0-windows  || goto break
call :pack %fileName%.%ext%        net481          || goto break

:break

@echo off

@echo.

if not %errorlevel%==0 echo ERROR: Packaging has failed. See log for details.

exit /b %errorlevel%

:pack

@echo off

set lpx=%rootDir%\%1
set folder=%rootDir%\..\bin\Release\%2

set additional=%folder%\CsvLINQPadDriver.deps.json

if exist %lpx% del %lpx%
if exist %folder%\Microsoft.Bcl.*.dll set additional=^
%folder%\Microsoft.Bcl.AsyncInterfaces.dll ^
%folder%\Microsoft.Bcl.HashCode.dll ^
%folder%\CsvHelper.dll ^
%folder%\Humanizer.dll ^
%folder%\Microsoft.WindowsAPICodePack.dll ^
%folder%\Microsoft.WindowsAPICodePack.Shell.dll ^
%folder%\System.Threading.Tasks.Extensions.dll ^
%folder%\UnicodeCharsetDetector.dll ^
%folder%\UtfUnknown.dll

echo on

%zip% a -tzip -mx=9 %lpx% ^
%rootDir%\header.xml ^
%rootDir%\..\README.md ^
%rootDir%\..\LICENSE ^
%folder%\*Connection.png ^
%folder%\CsvLINQPadDriver.dll ^
%additional% || exit /b 1

@exit /b 0
