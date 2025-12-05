using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CursorVerse.Services
{
    public class FavoritesService
    {
        private readonly ILogger<FavoritesService> _logger;
        private readonly string _favoritesPath;
        private readonly string _registryPath = @"Software\CursorVerse\Favorites";
        
        private List<string> _favoritePets = new();
        private List<string> _favoriteCursors = new();

        public FavoritesService(ILogger<FavoritesService> logger)
        {
            _logger = logger;
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var favoritesDir = Path.Combine(localAppData, "CursorVerse", "Favorites");
            Directory.CreateDirectory(favoritesDir);
            _favoritesPath = Path.Combine(favoritesDir, "favorites.json");
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    LoadFavorites();
                    _logger.LogInformation("FavoritesService инициализирован");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка инициализации FavoritesService");
                }
            });
        }

        private void LoadFavorites()
        {
            try
            {
                // Сначала пытаемся загрузить из файла
                if (File.Exists(_favoritesPath))
                {
                    var json = File.ReadAllText(_favoritesPath);
                    var data = JsonConvert.DeserializeObject<FavoritesData>(json);
                    if (data != null)
                    {
                        _favoritePets = data.Pets ?? new List<string>();
                        _favoriteCursors = data.Cursors ?? new List<string>();
                        _logger.LogInformation("Загружены избранные: {PetCount} петомцев, {CursorCount} курсоров",
                            _favoritePets.Count, _favoriteCursors.Count);
                        return;
                    }
                }

                // Если файла нет, загружаем из реестра (для миграции)
                LoadFromRegistry();
                SaveToFile();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка загрузки избранных");
                _favoritePets = new List<string>();
                _favoriteCursors = new List<string>();
            }
        }

        private void LoadFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(_registryPath);
                if (key == null)
                    return;

                var petsValue = key.GetValue("Pets") as string;
                if (!string.IsNullOrEmpty(petsValue))
                {
                    _favoritePets = petsValue.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                var cursorsValue = key.GetValue("Cursors") as string;
                if (!string.IsNullOrEmpty(cursorsValue))
                {
                    _favoriteCursors = cursorsValue.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка загрузки избранных из реестра");
            }
        }

        private void SaveToFile()
        {
            try
            {
                var data = new FavoritesData
                {
                    Pets = _favoritePets,
                    Cursors = _favoriteCursors,
                    LastUpdated = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_favoritesPath, json);
                _logger.LogDebug("Избранные сохранены в файл");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения избранных в файл");
            }
        }

        /// <summary>
        /// Получить все избранные петомцы
        /// </summary>
        public async Task<List<string>> GetFavoritePetsAsync()
        {
            return await Task.FromResult(_favoritePets.ToList());
        }

        /// <summary>
        /// Получить все избранные курсоры
        /// </summary>
        public async Task<List<string>> GetFavoriteCursorsAsync()
        {
            return await Task.FromResult(_favoriteCursors.ToList());
        }

        /// <summary>
        /// Добавить петомца в избранные
        /// </summary>
        public async Task AddFavoritePetAsync(string petId)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!_favoritePets.Contains(petId))
                    {
                        _favoritePets.Add(petId);
                        SaveToFile();
                        _logger.LogInformation("Петомец добавлен в избранные: {PetId}", petId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка добавления петомца в избранные");
                }
            });
        }

        /// <summary>
        /// Удалить петомца из избранных
        /// </summary>
        public async Task RemoveFavoritePetAsync(string petId)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_favoritePets.Remove(petId))
                    {
                        SaveToFile();
                        _logger.LogInformation("Петомец удален из избранных: {PetId}", petId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка удаления петомца из избранных");
                }
            });
        }

        /// <summary>
        /// Проверить, находится ли петомец в избранных
        /// </summary>
        public async Task<bool> IsFavoritePetAsync(string petId)
        {
            return await Task.FromResult(_favoritePets.Contains(petId));
        }

        /// <summary>
        /// Добавить курсор в избранные
        /// </summary>
        public async Task AddFavoriteCursorAsync(string cursorId)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!_favoriteCursors.Contains(cursorId))
                    {
                        _favoriteCursors.Add(cursorId);
                        SaveToFile();
                        _logger.LogInformation("Курсор добавлен в избранные: {CursorId}", cursorId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка добавления курсора в избранные");
                }
            });
        }

        /// <summary>
        /// Удалить курсор из избранных
        /// </summary>
        public async Task RemoveFavoriteCursorAsync(string cursorId)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_favoriteCursors.Remove(cursorId))
                    {
                        SaveToFile();
                        _logger.LogInformation("Курсор удален из избранных: {CursorId}", cursorId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка удаления курсора из избранных");
                }
            });
        }

        /// <summary>
        /// Проверить, находится ли курсор в избранных
        /// </summary>
        public async Task<bool> IsFavoriteCursorAsync(string cursorId)
        {
            return await Task.FromResult(_favoriteCursors.Contains(cursorId));
        }

        /// <summary>
        /// Получить рекомендуемых петомцев (первые N избранных или случайные)
        /// </summary>
        public async Task<List<string>> GetRecommendedPetsAsync(int count = 3)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_favoritePets.Count >= count)
                    {
                        return _favoritePets.Take(count).ToList();
                    }

                    // Если недостаточно избранных, возвращаем все
                    return _favoritePets.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка получения рекомендуемых петомцев");
                    return new List<string>();
                }
            });
        }

        /// <summary>
        /// Быстрый доступ - спавнить случайного избранного петомца
        /// </summary>
        public async Task<string?> SpawnRandomFavoritePetAsync()
        {
            if (_favoritePets.Count == 0)
                return null;

            var random = new Random();
            var petId = _favoritePets[random.Next(_favoritePets.Count)];
            _logger.LogInformation("Спавнен случайный избранный петомец: {PetId}", petId);
            return await Task.FromResult(petId);
        }

        /// <summary>
        /// Очистить все избранные
        /// </summary>
        public async Task ClearAllAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    _favoritePets.Clear();
                    _favoriteCursors.Clear();
                    SaveToFile();
                    _logger.LogInformation("Все избранные очищены");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка очистки избранных");
                }
            });
        }

        /// <summary>
        /// Получить полное состояние избранных
        /// </summary>
        public async Task<FavoritesData> GetFavoritesStateAsync()
        {
            return await Task.FromResult(new FavoritesData
            {
                Pets = _favoritePets.ToList(),
                Cursors = _favoriteCursors.ToList(),
                LastUpdated = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Модель данных для избранных
    /// </summary>
    public class FavoritesData
    {
        [JsonProperty("pets")]
        public List<string> Pets { get; set; } = new();

        [JsonProperty("cursors")]
        public List<string> Cursors { get; set; } = new();

        [JsonProperty("last_updated")]
        public DateTime? LastUpdated { get; set; }
    }
}
