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

echo [1/5] Проверка .NET SDK...
dotnet --version
echo.

echo [2/5] Восстановление пакетов...
dotnet restore
if errorlevel 1 (
    echo [ОШИБКА] Не удалось восстановить пакеты
    pause
    exit /b 1
)
echo.

echo [3/5] Подготовка питомцев (bundled-pets)...
powershell -NoProfile -Command "if (Test-Path 'CursorVerse.App/bundled-pets') { Remove-Item 'CursorVerse.App/bundled-pets' -Recurse -Force }; New-Item -ItemType Directory -Path 'CursorVerse.App/bundled-pets' | Out-Null; Get-ChildItem -Directory 'CustomPets' | Where-Object { (Get-ChildItem -Path $_.FullName -Filter '*.json' -File).Count -gt 0 -and (Get-ChildItem -Path $_.FullName -Filter '*.png' -File).Count -gt 0 } | ForEach-Object { Copy-Item -Path $_.FullName -Destination 'CursorVerse.App/bundled-pets' -Recurse -Force; Write-Output ('Copied ' + $_.Name) }"
if errorlevel 1 (
    echo [ОШИБКА] Не удалось подготовить bundled-pets
    pause
    exit /b 1
)
echo.

echo [4/5] Сборка проекта (Release)...
dotnet build -c Release
if errorlevel 1 (
    echo [ОШИБКА] Сборка не удалась
    pause
    exit /b 1
)
echo.

echo [5/5] Публикация single-file exe...
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
