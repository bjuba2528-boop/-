using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using CursorVerse.Services;

namespace CursorVerse.App
{
    public class TestApp
    {
        public static void Main()
        {
            Console.WriteLine("Тестирование DI контейнера...");

            try
            {
                // Настройка Serilog
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .CreateLogger();

                // Настройка DI контейнера
                var services = new ServiceCollection();
                
                // Логирование
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });

                // Сервисы
                services.AddSingleton<CursorManager>();
                services.AddSingleton<DPETEngine>();
                services.AddSingleton<TaskbarCustomizer>();
                services.AddSingleton<DiscordRpcService>();
                services.AddSingleton<LucyAIService>();
                services.AddSingleton<WallpaperManager>();
                services.AddSingleton<NotificationCenter>();
                services.AddSingleton<HotkeyService>();
                services.AddSingleton<SystemTrayService>();
                services.AddSingleton<AutostartService>();
                services.AddSingleton<ThemeManager>();

                var serviceProvider = services.BuildServiceProvider();
                
                Console.WriteLine("✅ DI контейнер создан успешно!");
                
                // Попытка получить сервис
                var logger = serviceProvider.GetRequiredService<ILogger<TestApp>>();
                logger.LogInformation("Логирование работает");

                Console.WriteLine("✅ Все сервисы загружены!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
