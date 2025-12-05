using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CursorVerse.Core.Models;

namespace CursorVerse.Services
{
    public class DPETEngine
    {
        private readonly ILogger<DPETEngine> _logger;
        private readonly List<Window> _activePets = new();

        public DPETEngine(ILogger<DPETEngine> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Инициализация DPETEngine");
            await Task.CompletedTask;
        }

        public async Task<List<DPETPet>> GetPetListAsync()
        {
            return await Task.Run(() =>
            {
                var pets = new List<DPETPet>();
                
                // Ищем в bundled-pets
                var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled-pets");
                if (Directory.Exists(bundledPath))
                {
                    LoadPetsFromDirectory(bundledPath, pets);
                }
                
                // Ищем в локальной папке CustomPets (рядом с .sln файлом)
                var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
                var localCustomPetsPath = Path.Combine(projectRoot, "CustomPets");
                if (Directory.Exists(localCustomPetsPath))
                {
                    _logger.LogInformation("🎯 Загрузка питомцев из локальной папки: {Path}", localCustomPetsPath);
                    LoadPetsFromDirectory(localCustomPetsPath, pets);
                }
                
                // Ищем в CustomPets (AppData) - для совместимости
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var customPetsPath = Path.Combine(localAppData, "CursorVerse", "CustomPets");
                if (Directory.Exists(customPetsPath))
                {
                    _logger.LogInformation("📂 Загрузка питомцев из AppData: {Path}", customPetsPath);
                    LoadPetsFromDirectory(customPetsPath, pets);
                }
                
                _logger.LogInformation("Найдено {Count} питомцев", pets.Count);
                return pets;
            });
        }
        
        private void LoadPetsFromDirectory(string path, List<DPETPet> pets)
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Папка с питомцами не найдена: {Path}", path);
                return;
            }

            _logger.LogInformation("🔍 Сканирование папки питомцев: {Path}", path);
            var dirs = Directory.GetDirectories(path);
            _logger.LogInformation("📂 Найдено папок: {Count}", dirs.Length);

            foreach (var petDir in dirs)
            {
                var petName = Path.GetFileName(petDir);
                _logger.LogDebug("🔎 Проверка папки: {PetName}", petName);
                
                // Ищем любой .json файл в папке
                var jsonFiles = Directory.GetFiles(petDir, "*.json");
                if (jsonFiles.Length == 0)
                {
                    _logger.LogDebug("⚠️ JSON файл не найден в: {PetDir}", petDir);
                    continue;
                }

                var configPath = jsonFiles[0]; // Берем первый найденный JSON
                _logger.LogInformation("📄 Найден конфиг: {ConfigPath}", configPath);

                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<DPETConfig>(json);
                    
                    if (config != null && !string.IsNullOrEmpty(config.Name))
                    {
                        // Ищем изображение для превью
                        string? previewPath = null;
                        
                        // 1. Ищем .png файл с именем питомца
                        var pngFile = Path.Combine(petDir, config.Img ?? $"{config.Name}.png");
                        if (File.Exists(pngFile))
                        {
                            previewPath = pngFile;
                        }
                        else
                        {
                            // 2. Ищем любой .png в папке
                            var pngFiles = Directory.GetFiles(petDir, "*.png");
                            if (pngFiles.Length > 0)
                            {
                                previewPath = pngFiles[0];
                            }
                        }

                        // Генерируем base64 превью напрямую вместо URL
                        string? previewDataUrl = null;
                        if (!string.IsNullOrEmpty(previewPath) && File.Exists(previewPath))
                        {
                            try
                            {
                                // Читаем размеры из конфига
                                int frameWidth = config.Width ?? 128;
                                int frameHeight = config.Height ?? 128;
                                
                                // Извлекаем первый кадр
                                using var originalImage = System.Drawing.Image.FromFile(previewPath);
                                using var firstFrame = new System.Drawing.Bitmap(frameWidth, frameHeight);
                                using var graphics = System.Drawing.Graphics.FromImage(firstFrame);
                                
                                graphics.DrawImage(originalImage, 
                                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                                    System.Drawing.GraphicsUnit.Pixel);

                                // Конвертируем в base64
                                using var ms = new System.IO.MemoryStream();
                                firstFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var base64 = Convert.ToBase64String(ms.ToArray());
                                previewDataUrl = $"data:image/png;base64,{base64}";
                                
                                _logger.LogDebug("✅ Превью создано для {Name}: {Size} bytes", config.Name, ms.Length);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "⚠️ Не удалось создать превью для {Name}", config.Name);
                            }
                        }
                        
                        pets.Add(new DPETPet
                        {
                            Id = petName,
                            Name = config.Name,
                            Description = config.Resources ?? config.Link ?? "Desktop Pet",
                            PreviewPath = previewDataUrl // Теперь это data URL с base64!
                        });
                        
                        _logger.LogInformation("✅ Загружен питомец: {Name} (ID: {Id})", config.Name, petName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Ошибка загрузки конфига питомца: {Path}", configPath);
                }
            }
        }

        public async Task SpawnPetAsync(string petId)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Ищем папку с питомцем
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var customPetsPath = Path.Combine(localAppData, "CursorVerse", "CustomPets", petId);

                    if (!Directory.Exists(customPetsPath))
                    {
                        var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled-pets", petId);
                        if (Directory.Exists(bundledPath))
                        {
                            customPetsPath = bundledPath;
                        }
                        else
                        {
                            _logger.LogWarning("❌ Папка питомца не найдена: {PetId}", petId);
                            return;
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var petWindow = new PetWindow(petId, customPetsPath, _logger);
                        petWindow.Show();
                        _activePets.Add(petWindow as Window);

                        petWindow.Closed += (s, e) => _activePets.Remove(petWindow as Window);
                    });

                    _logger.LogInformation("🐾 Питомец создан: {PetId}", petId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Ошибка создания питомца");
                }
            });
        }

        public void RemoveAllPets()
        {
            foreach (var pet in _activePets.ToList())
            {
                pet.Close();
            }
            _activePets.Clear();
        }

        // Алиас для совместимости с фронтендом
        public async Task<List<DPETPet>> GetPetPackagesAsync()
        {
            return await GetPetListAsync();
        }

        // Получить активных питомцев
        public List<object> GetActivePets()
        {
            return _activePets.Select(pet => new
            {
                id = pet.Title,
                name = pet.Title
            }).ToList<object>();
        }
    }
}
