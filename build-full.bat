@echo off
echo ========================================
echo CursorVerse C# - Полная сборка
echo ========================================
echo.

echo [Шаг 1/5] Проверка системы...
call check-system.bat
if errorlevel 1 exit /b 1
echo.

echo [Шаг 2/5] Миграция фронтенда...
if not exist "CursorVerse.App\wwwroot\index.html" (
    echo Фронтенд не мигрирован. Запуск миграции...
    call migrate-frontend.bat
    if errorlevel 1 exit /b 1
) else (
    echo ✅ Фронтенд уже мигрирован
)
echo.

echo [Шаг 3/5] Восстановление NuGet пакетов...
dotnet restore
if errorlevel 1 (
    echo ❌ Ошибка восстановления пакетов
    pause
    exit /b 1
)
echo.

echo [Шаг 4/5] Сборка Release...
dotnet build -c Release
if errorlevel 1 (
    echo ❌ Ошибка сборки
    pause
    exit /b 1
)
echo.

echo [Шаг 5/5] Публикация...
dotnet publish CursorVerse.App\CursorVerse.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
if errorlevel 1 (
    echo ❌ Ошибка публикации
    pause
    exit /b 1
)
echo.

echo ========================================
echo ✅ ПОЛНАЯ СБОРКА ЗАВЕРШЕНА!
echo ========================================
echo.
echo 📦 Результаты:
echo    - publish\CursorVerse.exe (single-file)
echo    - Размер: ~12-15 MB
echo    - Готово к распространению
echo.
echo 📋 Следующие шаги:
echo    1. Протестируйте: publish\CursorVerse.exe
echo    2. Создайте установщик: create-installer.bat
echo.
pause
