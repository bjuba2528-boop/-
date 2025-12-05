@echo off
echo ========================================
echo CursorVerse - Создание установщика
echo ========================================
echo.

REM Проверка Inno Setup
set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%INNO_PATH%" (
    echo [ОШИБКА] Inno Setup не найден!
    echo Установите с: https://jrsoftware.org/isdl.php
    pause
    exit /b 1
)

echo [1/3] Сборка Release...
call build.bat
if errorlevel 1 (
    echo [ОШИБКА] Сборка не удалась
    pause
    exit /b 1
)

echo.
echo [2/3] Создание установщика...
"%INNO_PATH%" installer.iss
if errorlevel 1 (
    echo [ОШИБКА] Создание установщика не удалось
    pause
    exit /b 1
)

echo.
echo [3/3] Готово!
echo.
echo ========================================
echo ✅ УСТАНОВЩИК СОЗДАН!
echo ========================================
echo.
echo Файл: installer-output\CursorVerse-Setup-1.6.0.exe
echo.
pause
