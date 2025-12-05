using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace CursorVerse.Services
{
    public class TaskbarCustomizer
    {
        private readonly ILogger<TaskbarCustomizer> _logger;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public TaskbarCustomizer(ILogger<TaskbarCustomizer> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Инициализация TaskbarCustomizer");
            await Task.CompletedTask;
        }

        public void HideWindowsTaskbar()
        {
            try
            {
                var taskbarHandles = new[] { "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };
                foreach (var className in taskbarHandles)
                {
                    var hwnd = FindWindow(className, null);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_HIDE);
                        _logger.LogInformation("Панель задач скрыта: {ClassName}", className);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скрытия панели задач");
            }
        }

        public void ShowWindowsTaskbar()
        {
            try
            {
                var taskbarHandles = new[] { "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };
                foreach (var className in taskbarHandles)
                {
                    var hwnd = FindWindow(className, null);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_SHOW);
                        _logger.LogInformation("Панель задач показана: {ClassName}", className);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка показа панели задач");
            }
        }
    }

    public class WallpaperManager
    {
        private readonly ILogger<WallpaperManager> _logger;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        public WallpaperManager(ILogger<WallpaperManager> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Инициализация WallpaperManager");
            await Task.CompletedTask;
        }

        public async Task<List<string>> GetSpotlightWallpapersAsync()
        {
            return await Task.Run(() =>
            {
                var wallpapers = new List<string>();
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var spotlightPath = Path.Combine(localAppData, "Packages",
                    "Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy", "LocalState", "Assets");

                if (!Directory.Exists(spotlightPath))
                    return wallpapers;

                foreach (var file in Directory.GetFiles(spotlightPath))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length > 100_000) // Больше 100KB
                    {
                        wallpapers.Add(file);
                    }
                }

                _logger.LogInformation("Найдено {Count} Windows Spotlight обоев", wallpapers.Count);
                return wallpapers;
            });
        }

        public void SetWallpaper(string path)
        {
            try
            {
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                _logger.LogInformation("Обои установлены: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка установки обоев");
            }
        }
    }

    public class NotificationCenter
    {
        private readonly ILogger<NotificationCenter> _logger;
        private readonly Queue<string> _notifications = new();

        public NotificationCenter(ILogger<NotificationCenter> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Инициализация NotificationCenter");
            await Task.CompletedTask;
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                _notifications.Enqueue($"{title}: {message}");
                _logger.LogInformation("Уведомление: {Title} - {Message}", title, message);
                
                // TODO: Интеграция с Windows Toast Notifications
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка показа уведомления");
            }
        }

        public string[] GetRecentNotifications(int count = 10)
        {
            return _notifications.Reverse().Take(count).ToArray();
        }
    }
}
