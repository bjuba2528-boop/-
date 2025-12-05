using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CursorVerse.Core.Models;

namespace CursorVerse.Services
{
    public class CursorManager
    {
        private readonly ILogger<CursorManager> _logger;
        private readonly string[] _cursorTypes = new[]
        {
            "Arrow", "Help", "AppStarting", "Wait", "Crosshair",
            "IBeam", "NWPen", "No", "SizeNS", "SizeWE",
            "SizeNWSE", "SizeNESW", "SizeAll", "UpArrow", "Hand"
        };

        public CursorManager(ILogger<CursorManager> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Инициализация CursorManager");
            await Task.CompletedTask;
        }

        public async Task<List<CursorScheme>> GetCursorLibraryAsync()
        {
            return await Task.Run(() =>
            {
                var schemes = new List<CursorScheme>();
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var libraryPath = Path.Combine(localAppData, "CursorVerse");

                if (!Directory.Exists(libraryPath))
                {
                    _logger.LogWarning("Библиотека курсоров не найдена: {Path}", libraryPath);
                    return schemes;
                }

                // Структура: CursorVerse/Категория/Пакет
                foreach (var categoryDir in Directory.GetDirectories(libraryPath))
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    
                    foreach (var packageDir in Directory.GetDirectories(categoryDir))
                    {
                        var scheme = BuildSchemeForFolder(packageDir, categoryName);
                        if (scheme != null)
                        {
                            schemes.Add(scheme);
                        }
                    }
                }

                _logger.LogInformation("Найдено {Count} схем курсоров", schemes.Count);
                return schemes;
            });
        }

        private CursorScheme? BuildSchemeForFolder(string folderPath, string category)
        {
            var cursorFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cur", StringComparison.OrdinalIgnoreCase) || 
                           f.EndsWith(".ani", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cursorFiles.Count == 0)
                return null;

            var cursors = MapFilesToTypes(cursorFiles);
            if (cursors.Count == 0)
                return null;

            var packageName = Path.GetFileName(folderPath);
            var previewPath = FindPreview(folderPath);

            return new CursorScheme
            {
                Name = packageName,
                Category = category,
                Cursors = cursors,
                Preview = previewPath
            };
        }

        private Dictionary<string, string> MapFilesToTypes(List<string> files)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var aliases = GetCursorAliases();

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();

                // Прямые совпадения
                foreach (var type in _cursorTypes)
                {
                    if (fileName.Contains(type.ToLowerInvariant()) && !result.ContainsKey(type))
                    {
                        result[type] = file;
                        break;
                    }
                }

                // Алиасы
                foreach (var (alias, canonicalType) in aliases)
                {
                    if (fileName.Contains(alias) && !result.ContainsKey(canonicalType))
                    {
                        result[canonicalType] = file;
                    }
                }
            }

            return result;
        }

        private Dictionary<string, string> GetCursorAliases()
        {
            return new Dictionary<string, string>
            {
                { "pointer", "Arrow" }, { "normal", "Arrow" },
                { "link", "AppStarting" }, { "working", "AppStarting" },
                { "busy", "Wait" },
                { "cross", "Crosshair" }, { "precision", "Crosshair" },
                { "text", "IBeam" }, { "beam", "IBeam" },
                { "move", "SizeAll" },
                { "dgn1", "SizeNESW" }, { "diagonal1", "SizeNESW" },
                { "dgn2", "SizeNWSE" }, { "diagonal2", "SizeNWSE" },
                { "horz", "SizeWE" }, { "horizontal", "SizeWE" },
                { "vert", "SizeNS" }, { "vertical", "SizeNS" },
                { "alternate", "Hand" },
                { "unavailable", "No" }
            };
        }

        private string? FindPreview(string folderPath)
        {
            var previewNames = new[] { "preview.png", "preview.jpg", "preview.jpeg", "preview.webp", "preview.gif" };
            
            foreach (var name in previewNames)
            {
                var path = Path.Combine(folderPath, name);
                if (File.Exists(path))
                {
                    return path; // Возвращаем путь, а не base64
                }
            }

            // Ищем любое изображение
            var imageExts = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            var firstImage = Directory.GetFiles(folderPath)
                .FirstOrDefault(f => imageExts.Contains(Path.GetExtension(f).ToLowerInvariant()));

            return firstImage;
        }

        public string? ConvertImageToBase64(string imagePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(imagePath);
                var ext = Path.GetExtension(imagePath).ToLowerInvariant();
                var mimeType = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };
                
                return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось конвертировать изображение: {Path}", imagePath);
                return null;
            }
        }

        public async Task ApplyCursorAsync(string schemeId)
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Начало применения схемы курсоров: {SchemeId}", schemeId);
                    
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var libraryPath = Path.Combine(localAppData, "CursorVerse");
                    
                    // Ищем схему курсора по ID
                    string? schemeFolder = null;
                    foreach (var categoryDir in Directory.GetDirectories(libraryPath))
                    {
                        var packageDirs = Directory.GetDirectories(categoryDir);
                        var found = packageDirs.FirstOrDefault(d => Path.GetFileName(d) == schemeId);
                        if (found != null)
                        {
                            schemeFolder = found;
                            _logger.LogInformation("Найдена папка схемы: {Path}", schemeFolder);
                            break;
                        }
                    }

                    if (schemeFolder == null)
                    {
                        _logger.LogError("Схема курсора не найдена: {SchemeId}", schemeId);
                        return;
                    }

                    // Сканируем все файлы курсоров
                    var cursorFiles = MapFilesToTypes(Directory.GetFiles(schemeFolder, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".cur", StringComparison.OrdinalIgnoreCase) || 
                                   f.EndsWith(".ani", StringComparison.OrdinalIgnoreCase))
                        .ToList());

                    if (cursorFiles.Count == 0)
                    {
                        _logger.LogError("Файлы курсоров не найдены в: {Path}", schemeFolder);
                        return;
                    }

                    _logger.LogInformation("Найдено {Count} файлов курсоров", cursorFiles.Count);

                    // Применяем курсор через реестр
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true))
                    {
                        if (key == null)
                        {
                            _logger.LogError("Не удалось открыть ключ реестра для курсоров");
                            return;
                        }

                        // Применяем каждый тип курсора
                        foreach (var (cursorType, cursorFile) in cursorFiles)
                        {
                            try
                            {
                                key.SetValue(cursorType, cursorFile);
                                _logger.LogDebug("Применен курсор {Type}: {File}", cursorType, cursorFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Ошибка при установке курсора {Type}", cursorType);
                            }
                        }
                    }

                    // Применяем изменения через Windows API
                    _logger.LogInformation("Отправка сигнала обновления курсоров системе...");
                    const uint SPI_SETCURSORS = 87;
                    const uint SPIF_UPDATEINIFILE = 0x01;
                    const uint SPIF_SENDCHANGE = 0x02;
                    
                    NativeMethods.SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                    _logger.LogInformation("Схема курсоров успешно применена: {SchemeId}", schemeId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка применения курсора");
                }
            });
        }

        public async Task ResetCursorAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    const uint SPI_SETCURSORS = 87;
                    const uint SPIF_UPDATEINIFILE = 0x01;
                    const uint SPIF_SENDCHANGE = 0x02;

                    _logger.LogInformation("Сброс курсоров к стандартным...");
                    
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Cursors", writable: true))
                    {
                        if (key == null)
                        {
                            _logger.LogError("Не удалось открыть ключ реестра для сброса курсоров");
                            return;
                        }

                        // Сбрасываем все типы курсоров на пусто (система использует стандартные)
                        var cursorTypes = new[]
                        {
                            "Arrow", "Help", "AppStarting", "Wait", "Crosshair",
                            "IBeam", "NWPen", "No", "SizeNS", "SizeWE",
                            "SizeNWSE", "SizeNESW", "SizeAll", "UpArrow", "Hand"
                        };

                        foreach (var cursorType in cursorTypes)
                        {
                            try
                            {
                                key.DeleteValue(cursorType, false);
                                _logger.LogDebug("Сброшен курсор: {Type}", cursorType);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Ошибка при сбросе курсора {Type}", cursorType);
                            }
                        }
                    }

                    // Отправляем сигнал системе
                    NativeMethods.SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                    _logger.LogInformation("Курсоры успешно сброшены к стандартным");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при сбросе курсоров");
                }
            });
        }
    }

    // Нативные методы для работы с курсором
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
    }
}
