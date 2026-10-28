@echo off
setlocal
title Menjalankan SIGMA

set "SIGMA_ROOT=%~dp0"
set "BACKEND_DIR=%SIGMA_ROOT%Backend"
set "FRONTEND_DIR=%SIGMA_ROOT%Frontend"

rem Kompatibilitas dengan struktur folder lama.
if not exist "%BACKEND_DIR%\src\PTK.SIGMA.Api\PTK.SIGMA.Api.csproj" set "BACKEND_DIR=%SIGMA_ROOT%Web SIGMA\PTK.SIGMA.Api"
if not exist "%FRONTEND_DIR%\PTK.SIGMA.Web\PTK.SIGMA.Web.csproj" set "FRONTEND_DIR=%SIGMA_ROOT%Web SIGMA\PTK.SIGMA.Web"

if not exist "%SIGMA_ROOT%.runtime\minio\bin\minio.exe" (
    echo ERROR: MinIO tidak ditemukan.
    echo Pastikan file ini berada di folder D:\Downloads\Web SIGMA
    pause
    exit /b 1
)

if not exist "%BACKEND_DIR%\src\PTK.SIGMA.Api\PTK.SIGMA.Api.csproj" (
    echo ERROR: Project backend SIGMA tidak ditemukan.
    echo Lokasi yang diperiksa: %BACKEND_DIR%
    pause
    exit /b 1
)

if not exist "%FRONTEND_DIR%\PTK.SIGMA.Web\PTK.SIGMA.Web.csproj" (
    echo ERROR: Project frontend SIGMA tidak ditemukan.
    echo Lokasi yang diperiksa: %FRONTEND_DIR%
    pause
    exit /b 1
)

powershell.exe -NoProfile -Command "if (Get-NetTCPConnection -State Listen -LocalPort 9000 -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 (
    echo Menjalankan MinIO...
    start "SIGMA - MinIO" powershell.exe -NoProfile -NoExit -ExecutionPolicy Bypass -Command "$env:MINIO_ROOT_USER='minioadmin'; $env:MINIO_ROOT_PASSWORD='minioadmin'; Set-Location -LiteralPath '%SIGMA_ROOT%'; & '.\.runtime\minio\bin\minio.exe' server '.\.runtime\minio\data' --console-address ':9001'"
) else (
    echo MinIO sudah aktif.
)

powershell.exe -NoProfile -Command "if (Get-NetTCPConnection -State Listen -LocalPort 5000 -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 (
    echo Menjalankan backend...
    start "SIGMA - Backend" powershell.exe -NoProfile -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%BACKEND_DIR%'; dotnet run --project '.\src\PTK.SIGMA.Api' --launch-profile https"
) else (
    echo Backend sudah aktif.
)

powershell.exe -NoProfile -Command "if (Get-NetTCPConnection -State Listen -LocalPort 5051 -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }" >nul 2>&1
if errorlevel 1 (
    echo Menjalankan website...
    start "SIGMA - Website" powershell.exe -NoProfile -NoExit -ExecutionPolicy Bypass -Command "Set-Location -LiteralPath '%FRONTEND_DIR%'; dotnet watch --project '.\PTK.SIGMA.Web' --no-hot-reload --launch-profile https"
) else (
    echo Website sudah aktif.
)

echo Menunggu website siap...
for /L %%i in (1,1,90) do (
    curl.exe -k -sS -o NUL --max-time 2 "https://localhost:5051" >nul 2>&1
    if not errorlevel 1 goto :website_ready
    ping.exe 127.0.0.1 -n 2 >nul
)

:website_ready
start "" "https://localhost:5051"

endlocal
exit /b 0
