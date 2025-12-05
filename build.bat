@echo off
echo ========================================
echo CursorVerse C# - Сборка проекта
echo ========================================
echo.

REM Проверка .NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ОШИБКА] .NET SDK не найден!
    echo Установите .NET 8.0 SDK: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/4] Проверка .NET SDK...
dotnet --version
echo.

echo [2/4] Восстановление пакетов...
dotnet restore
if errorlevel 1 (
    echo [ОШИБКА] Не удалось восстановить пакеты
    pause
    exit /b 1
)
echo.

echo [3/4] Сборка проекта (Release)...
dotnet build -c Release
if errorlevel 1 (
    echo [ОШИБКА] Сборка не удалась
    pause
    exit /b 1
)
echo.

echo [4/4] Публикация single-file exe...
dotnet publish CursorVerse.App\CursorVerse.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
if errorlevel 1 (
    echo [ОШИБКА] Публикация не удалась
    pause
    exit /b 1
)
echo.

echo ========================================
echo ✅ СБОРКА ЗАВЕРШЕНА!
echo ========================================
echo.
echo Исполняемый файл: publish\CursorVerse.exe
echo.
pause
