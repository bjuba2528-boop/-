# 🔧 Исправления для загрузки питомцев

## Проблема
Приложение не могло правильно загрузить спрайтшиты питомцев из-за:
1. `GetPetSpriteSheetAsync` возвращал только первый кадр вместо полного спрайтшита
2. Отсутствовал HTTP API endpoint для получения списка питомцев
3. Ошибки `ERR_INVALID_URL` и `Tracking Prevention blocked access to storage`

## Решение

### 1. Исправлен метод загрузки спрайтшита

**Файл:** `CursorVerse.App/MainWindow.xaml.cs`

**Было:**
```csharp
// Вырезаем первый кадр (верхний левый)
return await Task.Run(() => ExtractFirstFrame(spritePath, frameWidth, frameHeight));
```

**Стало:**
```csharp
// Возвращаем ВЕСЬ спрайтшит в base64 (не только первый кадр!)
var imageBytes = await File.ReadAllBytesAsync(spritePath);
var base64 = Convert.ToBase64String(imageBytes);
_logger.LogInformation("✅ Спрайтшит загружен, размер: {Size} bytes", imageBytes.Length);
return base64;
```

### 2. Добавлен HTTP API для списка питомцев

**Файл:** `CursorVerse.Services/WebServerService.cs`

**Добавлено:**
- Конструктор теперь принимает `DPETEngine?` (опционально)
- Новый endpoint: `GET /api/pets/list` или `GET /api/pets`
- Метод `HandlePetListAsync` для обработки запросов

**Код:**
```csharp
public WebServerService(ILogger<WebServerService> logger, DPETEngine? dpetEngine = null)
{
    _logger = logger;
    _petImageHandler = new PetImageHandler(logger);
    _dpetEngine = dpetEngine;
}

private async Task HandlePetListAsync(HttpListenerContext context)
{
    // Возвращает JSON список всех доступных питомцев
    var pets = await _dpetEngine.GetPetListAsync();
    var json = Newtonsoft.Json.JsonConvert.SerializeObject(pets);
    // ... отправка response
}
```

### 3. Обновлена маршрутизация запросов

**Файл:** `CursorVerse.Services/WebServerService.cs`

**Добавлено в `ProcessRequest`:**
```csharp
// API для списка питомцев
if (path == "/api/pets/list" || path == "/api/pets")
{
    HandlePetListAsync(context).Wait();
    return;
}
```

## Тестирование

### Тестовая страница
Создана тестовая страница: `wwwroot/test-pet-api.html`

Откройте в браузере:
```
http://127.0.0.1:3000/test-pet-api.html
```

### Доступные тесты:
1. **Get Pet List** - получение списка всех питомцев
2. **Get Pet Data** - получение конфигурации конкретного питомца
3. **Get Sprite** - загрузка и отображение полного спрайтшита

### API Endpoints

#### 1. Список питомцев
```
GET http://127.0.0.1:3000/api/pets/list
GET http://127.0.0.1:3000/api/pets
```

**Response:**
```json
[
  {
    "id": "Alastor",
    "name": "Alastor Shimeji - EmberCL",
    "description": "Myself",
    "preview_path": "C:\\Users\\...\\Alastor\\Alastor Shimeji - EmberCL.png"
  }
]
```

#### 2. Preview питомца (первый кадр)
```
GET http://127.0.0.1:3000/api/pets/{petId}/preview
```

**Response:** PNG image (первый кадр спрайтшита)

#### 3. Полный спрайтшит
```
GET http://127.0.0.1:3000/api/pets/{petId}/sprite
```

**Response:** PNG image (полный спрайтшит со всеми кадрами)

#### 4. Данные питомца (через Tauri Mock API)
```javascript
await invoke('dpet_get_pet_data', { petId: 'Alastor' })
```

**Response:**
```json
{
  "package_id": "Alastor",
  "name": "Alastor Shimeji - EmberCL",
  "state": "stand",
  "config": {
    "name": "Alastor Shimeji - EmberCL",
    "img": "Alastor Shimeji - EmberCL.png",
    "width": 128,
    "height": 128,
    "bouncing": 2,
    "animePos": { ... }
  }
}
```

#### 5. Спрайтшит в base64 (через Tauri Mock API)
```javascript
await invoke('dpet_get_sprite_sheet', { packageId: 'Alastor' })
```

**Response:**
```json
{
  "base64Image": "iVBORw0KGgoAAAANSUhEUgAA...",
  "success": true
}
```

## Как использовать в dpet.html

```javascript
// Загрузка спрайтшита
async function loadSpriteSheet() {
    const result = await invoke('dpet_get_sprite_sheet', { 
        packageId: petData.package_id 
    });
    
    const base64Data = result?.base64Image || result;
    
    // Устанавливаем фон для спрайта
    spriteSheet = `data:image/png;base64,${base64Data}`;
    petSprite.style.backgroundImage = `url(${spriteSheet})`;
}
```

## Проверка работоспособности

### 1. Пересоберите проект:
```bash
dotnet build CursorVerse.sln --configuration Debug
```

### 2. Запустите приложение:
```bash
run.bat
```

### 3. Откройте тестовую страницу:
```
http://127.0.0.1:3000/test-pet-api.html
```

### 4. Проверьте логи:
```
logs\cursorverse-{дата}.log
```

Ищите строки:
- `✅ Спрайтшит загружен, размер: XXX bytes`
- `📋 Pet list request`
- `✅ Pet list sent: X pets`

## Возможные проблемы

### ERR_INVALID_URL
**Причина:** Неправильный формат URL или спецсимволы в пути
**Решение:** Используйте base64 кодирование вместо прямых путей к файлам

### Tracking Prevention blocked access
**Причина:** Браузерные ограничения на доступ к локальным файлам
**Решение:** Используйте HTTP API или base64 data URLs

### 404 Not Found
**Причина:** Питомец не найден в CustomPets или bundled-pets
**Решение:** Проверьте структуру папок и наличие JSON + PNG файлов

## Следующие шаги

1. ✅ Исправлена загрузка спрайтшитов
2. ✅ Добавлен HTTP API для списка питомцев
3. ✅ Создана тестовая страница
4. 🔄 Нужно протестировать в реальном приложении
5. 🔄 Обновить фронтенд для использования нового API

## Полезные ссылки

- [SHIMEJI_PETS_GUIDE.md](./SHIMEJI_PETS_GUIDE.md) - Подробная инструкция по добавлению питомцев
- [test-pet-api.html](./CursorVerse.App/wwwroot/test-pet-api.html) - Страница для тестирования API
