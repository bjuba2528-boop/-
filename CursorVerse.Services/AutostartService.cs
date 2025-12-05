using System;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    public class AutostartService
    {
        private readonly ILogger<AutostartService> _logger;
        private const string AppName = "CursorVerse";

        public AutostartService(ILogger<AutostartService> logger)
        {
            _logger = logger;
        }

        public bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                var value = key?.GetValue(AppName);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки автозапуска");
                return false;
            }
        }

        public void Enable()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    _logger.LogError("Не удалось получить путь к exe");
                    return;
                }

                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.SetValue(AppName, $"\"{exePath}\"");
                
                _logger.LogInformation("Автозапуск включен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка включения автозапуска");
            }
        }

        public void Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue(AppName, false);
                
                _logger.LogInformation("Автозапуск отключен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отключения автозапуска");
            }
        }
    }
}
