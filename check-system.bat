@echo off
echo ========================================
echo CursorVerse C# - Диагностика системы
echo ========================================
echo.

set "ERRORS=0"

REM Проверка .NET SDK
echo [1/6] Проверка .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ❌ .NET SDK не установлен
    echo    Скачайте: https://dotnet.microsoft.com/download
    set /a ERRORS+=1
) else (
    echo ✅ .NET SDK установлен
    dotnet --version
)
echo.

REM Проверка Node.js (для миграции)
echo [2/6] Проверка Node.js...
node --version >nul 2>&1
if errorlevel 1 (
    echo ⚠️  Node.js не установлен (нужен только для миграции фронтенда)
) else (
    echo ✅ Node.js установлен
    node --version
)
echo.

REM Проверка исходного проекта
echo [3/6] Проверка исходного проекта...
if exist "..\package.json" (
    echo ✅ Исходный Tauri проект найден
) else (
    echo ⚠️  Исходный проект не найден в родительской папке
)
echo.

REM Проверка wwwroot (фронтенд)
echo [4/6] Проверка фронтенда...
if exist "CursorVerse.App\wwwroot\index.html" (
    echo ✅ Фронтенд мигрирован (wwwroot существует)
) else (
    echo ⚠️  Фронтенд не мигрирован
    echo    Запустите: migrate-frontend.bat
)
echo.

REM Проверка проектных файлов
echo [5/6] Проверка структуры проекта...
set "PROJECT_OK=1"

if not exist "CursorVerse.sln" (
    echo ❌ CursorVerse.sln не найден
    set "PROJECT_OK=0"
    set /a ERRORS+=1
)

if not exist "CursorVerse.App\CursorVerse.App.csproj" (
    echo ❌ CursorVerse.App.csproj не найден
    set "PROJECT_OK=0"
    set /a ERRORS+=1
)

if not exist "CursorVerse.Core\CursorVerse.Core.csproj" (
    echo ❌ CursorVerse.Core.csproj не найден
    set "PROJECT_OK=0"
    set /a ERRORS+=1
)

if not exist "CursorVerse.Services\CursorVerse.Services.csproj" (
    echo ❌ CursorVerse.Services.csproj не найден
    set "PROJECT_OK=0"
    set /a ERRORS+=1
)

if "%PROJECT_OK%"=="1" (
    echo ✅ Все проектные файлы на месте
)
echo.

REM Проверка WebView2 Runtime
echo [6/6] Проверка WebView2 Runtime...
reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if errorlevel 1 (
    echo ⚠️  WebView2 Runtime может быть не установлен
    echo    Будет установлен автоматически при запуске
) else (
    echo ✅ WebView2 Runtime установлен
)
echo.

REM Итоговый результат
echo ========================================
if %ERRORS%==0 (
    echo ✅ ВСЁ ГОТОВО К ЗАПУСКУ!
    echo ========================================
    echo.
    echo Следующие шаги:
    echo   1. Если фронтенд не мигрирован: migrate-frontend.bat
    echo   2. Запуск: run.bat
    echo   3. Сборка Release: build.bat
) else (
    echo ❌ ОБНАРУЖЕНЫ ОШИБКИ: %ERRORS%
    echo ========================================
    echo.
    echo Исправьте ошибки выше и запустите снова
)
echo.
pause
