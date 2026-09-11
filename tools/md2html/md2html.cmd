@echo off
chcp 65001 > nul
REM md2html.cmd - convert .md to .html in the same folder
REM Usage: 1) right-click menu  2) drag-and-drop a .md onto this .cmd

if "%~1"=="" (
    echo Usage: md2html.cmd ^<file.md^>
    echo Or drag a .md file onto this .cmd
    pause
    exit /b 1
)

where py >nul 2>&1
if %errorlevel%==0 (
    py -3 "%~dp0md2html.py" "%~1"
) else (
    python "%~dp0md2html.py" "%~1"
)
set RC=%errorlevel%

if %RC%==0 (
    echo.
    echo [OK] Converted. Opening HTML...
    start "" "%~dpn1.html"
    exit /b 0
)

echo.
echo [FAIL] Conversion failed (errorlevel=%RC%)
echo Check the error message above. Press any key to close.
pause > nul
exit /b %RC%
