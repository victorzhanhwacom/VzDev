@echo off
setlocal enabledelayedexpansion

:: ==========================================
:: 1. Check if Node.js is installed
:: ==========================================
node -v >nul 2>nul
if %errorlevel% neq 0 (
    echo [INFO] Node.js not found. Installing now...

    winget install OpenJS.NodeJS.LTS --silent --accept-source-agreements --accept-package-agreements

    if %errorlevel% neq 0 (
        echo [ERROR] Node.js installation failed. Please install it manually.
        pause
        exit /b
    )

    echo [OK] Node.js installed successfully.
    echo [INFO] Refreshing environment variables...

    for /f "tokens=2*" %%A in ('reg query "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" /v Path') do set "Path=%%B"
    for /f "tokens=2*" %%A in ('reg query "HKCU\Environment" /v Path') do set "Path=!Path!;%%B"
)

:: ==========================================
:: 2. Check if server.js exists (must be in the same folder as this .bat)
:: ==========================================
if not exist "%~dp0server.js" (
    echo [ERROR] server.js not found. Make sure it is in the same folder as this .bat file.
    pause
    exit /b
)

:: ==========================================
:: 3. Start server
:: ==========================================
set /p PORT=Enter server port (default 8000): 
if "%PORT%"=="" set PORT=8000

echo.
echo Starting local server with Brotli/Gzip support, Port=%PORT%...
echo.

start http://localhost:%PORT%/index.html

node "%~dp0server.js" %PORT%

pause
endlocal
