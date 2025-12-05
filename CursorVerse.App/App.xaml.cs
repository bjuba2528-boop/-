using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using CursorVerse.Services;

namespace CursorVerse.App
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальный обработчик исключений
            this.DispatcherUnhandledException += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL: {ex.Exception.Message}\n{ex.Exception.StackTrace}");
                ex.Handled = false;
            };
            
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                System.Diagnostics.Debug.WriteLine($"DOMAIN ERROR: {ex.ExceptionObject}");
            };

            try
            {
                // Настройка Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File("logs/cursorverse-.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                Log.Information("Запуск приложения...");

                // Настройка DI контейнера
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                Log.Information("DI контейнер создан");

                // Запуск главного окна
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Log.Information("MainWindow получено из DI");
                mainWindow.Show();
                Log.Information("MainWindow показано");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Критическая ошибка при запуске");
                System.Windows.MessageBox.Show($"Критическая ошибка: {ex.Message}\n\n{ex.StackTrace}", "CursorVerse", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                this.Shutdown(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Логирование
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            // Сервисы
            services.AddSingleton<CursorManager>();
            services.AddSingleton<DPETEngine>();
            services.AddSingleton<FavoritesService>();
            services.AddSingleton<TaskbarCustomizer>();
            services.AddSingleton<DiscordRpcService>();
            services.AddSingleton<LucyAIService>();
            services.AddSingleton<WallpaperManager>();
            services.AddSingleton<NotificationCenter>();
            services.AddSingleton<HotkeyService>();
            services.AddSingleton<SystemTrayService>();
            services.AddSingleton<AutostartService>();
            services.AddSingleton<ThemeManager>();
            services.AddSingleton<WebServerService>();

            // Главное окно
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
