@echo off
echo ========================================================
echo   Building and Packaging Mero Dokan Cafe Setup
echo ========================================================

echo.
echo [1/3] Building and Publishing Application in Release mode...
dotnet publish "MeroDokanCafe.csproj" -c Release -o "publish"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Dotnet publish failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Checking Inno Setup Compiler...
set "ISCC_PATH="
if exist "C:\Users\bbhat\AppData\Local\Programs\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Users\bbhat\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if "%ISCC_PATH%"=="" if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if "%ISCC_PATH%"=="" if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"

if "%ISCC_PATH%"=="" (
    echo [ERROR] Inno Setup 6 was not found on your system!
    pause
    exit /b 1
)

echo.
echo [3/3] Compiling Setup Installer...
"%ISCC_PATH%" "installer_script.iss"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Installer compilation failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo   SUCCESS! Setup created at:
echo   Installer_Output\MeroDokanCafe_Setup_v1.0.exe
echo ========================================================
pause
