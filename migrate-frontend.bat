@echo off
setlocal enabledelayedexpansion

echo ========================================
echo CursorVerse - Миграция фронтенда
echo ========================================
echo.

set "ORIGINAL_DIR=%~dp0.."
set "CSHARP_DIR=%~dp0"
set "WWWROOT=%CSHARP_DIR%CursorVerse.App\wwwroot"

echo [1/5] Проверка исходного проекта...
if not exist "%ORIGINAL_DIR%\package.json" (
    echo [ОШИБКА] Исходный проект не найден!
    pause
    exit /b 1
)
echo ✓ Исходный проект найден

echo.
echo [2/5] Сборка React приложения...
cd /d "%ORIGINAL_DIR%"
call npm run build
if errorlevel 1 (
    echo [ОШИБКА] Сборка React не удалась
    pause
    exit /b 1
)
echo ✓ React приложение собрано

echo.
echo [3/5] Копирование dist в wwwroot...
if exist "%WWWROOT%" (
    echo Удаление старого wwwroot...
    rmdir /s /q "%WWWROOT%"
)
xcopy "%ORIGINAL_DIR%\dist" "%WWWROOT%" /E /I /Y /Q
if errorlevel 1 (
    echo [ОШИБКА] Не удалось скопировать dist
    pause
    exit /b 1
)
echo ✓ Фронтенд скопирован

echo.
echo [4/5] Копирование bundled-pets...
set "PETS_TARGET=%CSHARP_DIR%CursorVerse.App\bundled-pets"
if exist "%PETS_TARGET%" (
    rmdir /s /q "%PETS_TARGET%"
)
xcopy "%ORIGINAL_DIR%\bundled-pets" "%PETS_TARGET%" /E /I /Y /Q
echo ✓ Питомцы скопированы

echo.
echo [5/5] Копирование дополнительных ресурсов...
if exist "%ORIGINAL_DIR%\alastor-dpet" (
    xcopy "%ORIGINAL_DIR%\alastor-dpet" "%PETS_TARGET%\alastor-dpet" /E /I /Y /Q
)
if exist "%ORIGINAL_DIR%\example-dpet" (
    xcopy "%ORIGINAL_DIR%\example-dpet" "%PETS_TARGET%\example-dpet" /E /I /Y /Q
)
echo ✓ Ресурсы скопированы

echo.
echo ========================================
echo ✅ МИГРАЦИЯ ЗАВЕРШЕНА!
echo ========================================
echo.
echo Скопировано:
echo   - React приложение → wwwroot\
echo   - Питомцы → bundled-pets\
echo   - Дополнительные ресурсы
echo.
echo Следующие шаги:
echo   1. Запустите: run.bat
echo   2. Или соберите Release: build.bat
echo.
pause
