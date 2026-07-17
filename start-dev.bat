@echo off
setlocal

chcp 65001 >nul

set "ROOT=%~dp0"
set "FRONTEND_DIR=%ROOT%frontend"
set "API_URL=http://localhost:5297"
set "WEB_URL=http://localhost:5173"

echo.
echo ========================================
echo  Interface JSON Compare Platform
echo ========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] 未检测到 dotnet，请先安装 .NET 10 SDK。
    pause
    exit /b 1
)

dotnet --list-sdks | findstr /R "^10\." >nul
if errorlevel 1 (
    echo [ERROR] 当前机器未检测到 .NET 10 SDK，请先安装 .NET 10 SDK。
    pause
    exit /b 1
)

where npm >nul 2>nul
if errorlevel 1 (
    echo [ERROR] 未检测到 npm，请先安装 Node.js。
    pause
    exit /b 1
)

if not exist "%FRONTEND_DIR%\node_modules" (
    echo [INFO] 首次运行未发现 frontend\node_modules，开始安装前端依赖...
    pushd "%FRONTEND_DIR%"
    call npm install
    if errorlevel 1 (
        popd
        echo [ERROR] 前端依赖安装失败，请检查 npm 网络或 package-lock.json。
        pause
        exit /b 1
    )
    popd
)

echo [INFO] 正在启动后端 API：%API_URL%
start "JsonDiff API" /D "%ROOT%" cmd /k "dotnet run --project backend\JsonDiffPlatform.Api\JsonDiffPlatform.Api.csproj --urls %API_URL%"

echo [INFO] 正在启动前端 Web：%WEB_URL%
rem 前端开发服务通过 Vite 代理把 /api 请求转发到后端 API。
start "JsonDiff Web" /D "%FRONTEND_DIR%" cmd /k "set VITE_API_PROXY_TARGET=%API_URL%&&npm run dev"

echo.
echo [OK] 已打开两个服务窗口。
echo      API: %API_URL%/api/health
echo      Web: %WEB_URL%
echo.
echo 如需停止服务，请关闭对应的 API / Web 命令行窗口。
pause
