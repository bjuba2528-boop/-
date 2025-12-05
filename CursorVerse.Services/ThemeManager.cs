using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    public class ThemeManager
    {
        private readonly ILogger<ThemeManager> _logger;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public ThemeManager(ILogger<ThemeManager> logger)
        {
            _logger = logger;
        }

        public bool IsDarkModeEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки темной темы");
                return false;
            }
        }

        public void SetWindowDarkMode(IntPtr hwnd, bool useDarkMode)
        {
            try
            {
                int darkMode = useDarkMode ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                
                _logger.LogInformation("Темная тема окна: {Enabled}", useDarkMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка установки темной темы окна");
            }
        }

        public void ApplySystemTheme()
        {
            try
            {
                var isDark = IsDarkModeEnabled();
                _logger.LogInformation("Системная тема: {Theme}", isDark ? "Темная" : "Светлая");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка применения системной темы");
            }
        }
    }
}
