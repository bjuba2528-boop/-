@echo off
echo ========================================
echo Очистка проекта CursorVerse
echo ========================================
echo.

echo Удаление временных файлов...

REM Удаление папок сборки
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
if exist "publish" rmdir /s /q "publish"
if exist "installer-output" rmdir /s /q "installer-output"

REM Удаление в подпроектах
if exist "CursorVerse.App\bin" rmdir /s /q "CursorVerse.App\bin"
if exist "CursorVerse.App\obj" rmdir /s /q "CursorVerse.App\obj"
if exist "CursorVerse.Core\bin" rmdir /s /q "CursorVerse.Core\bin"
if exist "CursorVerse.Core\obj" rmdir /s /q "CursorVerse.Core\obj"
if exist "CursorVerse.Services\bin" rmdir /s /q "CursorVerse.Services\bin"
if exist "CursorVerse.Services\obj" rmdir /s /q "CursorVerse.Services\obj"

REM Удаление логов
if exist "logs" rmdir /s /q "logs"

echo.
echo ✅ Очистка завершена!
echo.
pause
