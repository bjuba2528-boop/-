# Система Избранных Петомцев и Курсоров

## 📋 Описание

Реализована умная система избранных для быстрого доступа к любимым петомцам и курсорам. Система хранит данные в JSON файле с поддержкой миграции из реестра.

## 🎯 Функциональность

### FavoritesService (C#)

Основной сервис управления избранными, находится в `CursorVerse.Services\FavoritesService.cs`.

#### Методы для петомцев:

```csharp
// Получить все избранные петомцы
Task<List<string>> GetFavoritePetsAsync()

// Добавить петомца в избранные
Task AddFavoritePetAsync(string petId)

// Удалить петомца из избранных
Task RemoveFavoritePetAsync(string petId)

// Проверить, находится ли петомец в избранных
Task<bool> IsFavoritePetAsync(string petId)

// Получить рекомендуемых петомцев (первые N)
Task<List<string>> GetRecommendedPetsAsync(int count = 3)

// Спавнить случайного избранного петомца
Task<string?> SpawnRandomFavoritePetAsync()
```

#### Методы для курсоров:

```csharp
// Получить все избранные курсоры
Task<List<string>> GetFavoriteCursorsAsync()

// Добавить курсор в избранные
Task AddFavoriteCursorAsync(string cursorId)

// Удалить курсор из избранных
Task RemoveFavoriteCursorAsync(string cursorId)

// Проверить, находится ли курсор в избранных
Task<bool> IsFavoriteCursorAsync(string cursorId)
```

#### Другие методы:

```csharp
// Получить полное состояние избранных
Task<FavoritesData> GetFavoritesStateAsync()

// Очистить все избранные
Task ClearAllAsync()
```

## 📁 Хранилище данных

### Расположение:
```
%LOCALAPPDATA%\CursorVerse\Favorites\favorites.json
```

### Структура JSON:
```json
{
  "pets": [
    "alastor",
    "chiikawa",
    "rambley",
    "angel_dust"
  ],
  "cursors": [
    "cursorgalaxy_set_1",
    "anime_cursor_pack_2"
  ],
  "last_updated": "2025-12-05T10:30:45.123Z"
}
```

## 🎮 Tauri API для React

### Команды для петомцев:

```typescript
// Получить список избранных петомцев
invoke('get_favorite_pets')
// Возвращает: ["pet_id_1", "pet_id_2", ...]

// Добавить петомца в избранные
invoke('add_favorite_pet', { pet_id: 'alastor' })
// Возвращает: true

// Удалить петомца из избранных
invoke('remove_favorite_pet', { pet_id: 'alastor' })
// Возвращает: true

// Проверить, избран ли петомец
invoke('is_favorite_pet', { pet_id: 'alastor' })
// Возвращает: true | false

// Получить рекомендуемых петомцев
invoke('get_recommended_pets', { count: 3 })
// Возвращает: ["pet_1", "pet_2", "pet_3"]

// Спавнить случайного избранного петомца
invoke('spawn_random_favorite_pet')
// Возвращает: { success: true, pet_id: "alastor" }
```

### Команды для курсоров:

```typescript
// Получить список избранных курсоров
invoke('get_favorite_cursors')
// Возвращает: ["cursor_id_1", "cursor_id_2", ...]

// Добавить курсор в избранные
invoke('add_favorite_cursor', { cursor_id: 'anime_pack_1' })
// Возвращает: true

// Удалить курсор из избранных
invoke('remove_favorite_cursor', { cursor_id: 'anime_pack_1' })
// Возвращает: true

// Проверить, избран ли курсор
invoke('is_favorite_cursor', { cursor_id: 'anime_pack_1' })
// Возвращает: true | false
```

## 🎨 React компонент пример

```typescript
import { invoke } from '@tauri-apps/api/tauri';

export const FavoritePetsPanel = () => {
  const [favorites, setFavorites] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadFavorites();
  }, []);

  const loadFavorites = async () => {
    try {
      const pets = await invoke('get_favorite_pets');
      setFavorites(pets);
    } catch (error) {
      console.error('Ошибка загрузки избранных:', error);
    }
  };

  const toggleFavorite = async (petId: string) => {
    try {
      setLoading(true);
      const isFavorite = await invoke('is_favorite_pet', { pet_id: petId });
      
      if (isFavorite) {
        await invoke('remove_favorite_pet', { pet_id: petId });
      } else {
        await invoke('add_favorite_pet', { pet_id: petId });
      }
      
      await loadFavorites();
    } catch (error) {
      console.error('Ошибка:', error);
    } finally {
      setLoading(false);
    }
  };

  const spawnRandom = async () => {
    try {
      setLoading(true);
      const result = await invoke('spawn_random_favorite_pet');
      if (result.success) {
        console.log('Спавнен петомец:', result.pet_id);
      }
    } catch (error) {
      console.error('Ошибка спавна:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="favorites-panel">
      <h2>Избранные Петомцы ({favorites.length})</h2>
      
      <button onClick={spawnRandom} disabled={loading}>
        🎲 Спавнить случайного
      </button>

      <div className="favorites-list">
        {favorites.map(petId => (
          <div key={petId} className="favorite-item">
            <span>{petId}</span>
            <button onClick={() => toggleFavorite(petId)}>
              ⭐ Убрать
            </button>
          </div>
        ))}
      </div>
    </div>
  );
};
```

## 🔄 Миграция с реестра

При первом запуске сервис автоматически:

1. Проверяет наличие файла `favorites.json`
2. Если файл отсутствует, загружает данные из реестра:
   ```
   HKEY_CURRENT_USER\Software\CursorVerse\Favorites
   ```
3. Сохраняет загруженные данные в JSON файл
4. При следующих запусках использует только JSON

## ✨ Особенности

### 🎯 Умная рекомендация
```csharp
// Получить первых 3 избранных петомцев для быстрого доступа
var recommended = await favoritesService.GetRecommendedPetsAsync(3);
```

### 🎲 Случайный спавн
```csharp
// Спавнить случайного избранного петомца
// Отлично для развлечения пользователя
var randomPetId = await favoritesService.SpawnRandomFavoritePetAsync();
```

### 💾 Автосохранение
- Все изменения автоматически сохраняются в JSON файл
- Данные синхронизируются между F# бэкенд и React фронтенд
- Сохранение происходит асинхронно (не блокирует UI)

### 🔐 Безопасность
- Проверка существования петомца перед добавлением
- Обработка исключений при работе с файлами
- Валидация ID перед операциями

## 📊 Логирование

Сервис логирует все важные события:

```
[INFO] Петомец добавлен в избранные: alastor
[INFO] Петомец удален из избранных: chiikawa
[INFO] Все избранные очищены
[DEBUG] Избранные сохранены в файл
```

## 🚀 Производительность

- **Загрузка**: O(n) - линейный поиск в списке
- **Добавление**: O(n) - проверка существования + добавление
- **Удаление**: O(n) - удаление из списка
- **Сохранение**: Асинхронное, не блокирует UI
- **Файловые операции**: Кэшируются в памяти, минимум I/O

## 🔗 Интеграция

### DI Контейнер (App.xaml.cs):
```csharp
services.AddSingleton<FavoritesService>();
```

### Инициализация (MainWindow.xaml.cs):
```csharp
await _favoritesService.InitializeAsync();
```

### WebView команды (MainWindow.xaml.cs):
```csharp
case "get_favorite_pets":
    result = await _favoritesService.GetFavoritePetsAsync();
    break;

case "add_favorite_pet":
    var petId = args?.pet_id?.ToString();
    if (!string.IsNullOrEmpty(petId))
    {
        await _favoritesService.AddFavoritePetAsync(petId);
        result = true;
    }
    break;
```

## 📝 Примеры использования

### Добавить петомца в избранные через UI кнопку:
```typescript
const handleAddFavorite = async (petId: string) => {
  await invoke('add_favorite_pet', { pet_id: petId });
  showNotification('Петомец добавлен в избранные! ⭐');
};
```

### Показать список избранных:
```typescript
const favorites = await invoke('get_favorite_pets');
console.log(`У вас ${favorites.length} избранных петомцев`);
```

### Спавнить любимца одним кликом:
```typescript
const onQuickSpawn = async () => {
  const result = await invoke('spawn_random_favorite_pet');
  if (result.success) {
    playAnimation('pet-appear');
  }
};
```

## 🐛 Отладка

Для просмотра логов:
```bash
# Windows
notepad %LOCALAPPDATA%\CursorVerse\logs\cursorverse-YYYY-MM-DD.log

# Просмотр файла избранных
notepad %LOCALAPPDATA%\CursorVerse\Favorites\favorites.json
```

## 🔮 Будущие улучшения

- [ ] Сортировка избранных по категориям
- [ ] Группировка избранных по типам (анимированные, классические)
- [ ] Синхронизация избранных между устройствами (облако)
- [ ] История недавно использованных петомцев
- [ ] Автоматические рекомендации на основе истории
- [ ] Экспорт/импорт избранных

---

**Версия**: 1.7.0  
**Статус**: ✅ Готово к использованию  
**Последнее обновление**: 5 декабря 2025
