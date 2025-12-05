using System;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    public class SystemTrayService : IDisposable
    {
        private readonly ILogger<SystemTrayService> _logger;
        private NotifyIcon? _notifyIcon;

        public SystemTrayService(ILogger<SystemTrayService> logger)
        {
            _logger = logger;
        }

        public void Initialize(Window mainWindow)
        {
            try
            {
                // Загружаем иконку CursorVerse.ico
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                var icon = System.IO.File.Exists(iconPath) 
                    ? new System.Drawing.Icon(iconPath)
                    : System.Drawing.SystemIcons.Application;
                
                _notifyIcon = new NotifyIcon
                {
                    Icon = icon,
                    Visible = true,
                    Text = "CursorVerse"
                };

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Открыть", null, (s, e) => 
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                });
                contextMenu.Items.Add("Выход", null, (s, e) => 
                {
                    System.Windows.Application.Current.Shutdown();
                });

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => 
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                };

                _logger.LogInformation("Системный трей инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка инициализации трея");
            }
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}
