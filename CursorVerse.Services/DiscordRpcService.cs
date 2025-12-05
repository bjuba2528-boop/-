using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DiscordRPC;
using DiscordRPC.Logging;

namespace CursorVerse.Services
{
    public class DiscordRpcService
    {
        private readonly ILogger<DiscordRpcService> _logger;
        private DiscordRpcClient? _client;
        private readonly DateTime _startTime;
        
        private const string ClientId = "1444795416846663914";

        public DiscordRpcService(ILogger<DiscordRpcService> logger)
        {
            _logger = logger;
            _startTime = DateTime.UtcNow;
        }

        public void Initialize()
        {
            try
            {
                _client = new DiscordRpcClient(ClientId);
                _client.Logger = new ConsoleLogger() { Level = DiscordRPC.Logging.LogLevel.Warning };
                
                _client.OnReady += (sender, e) =>
                {
                    _logger.LogInformation("Discord RPC подключен: {User}", e.User.Username);
                };

                _client.OnError += (sender, e) =>
                {
                    _logger.LogError("Discord RPC ошибка: {Message}", e.Message);
                };

                _client.Initialize();

                // Устанавливаем начальное состояние
                UpdatePresence("🖱️ CursorVerse запущен", "Настройка Windows");

                _logger.LogInformation("Discord RPC инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка инициализации Discord RPC");
            }
        }

        public void UpdatePresence(string? details = null, string? state = null)
        {
            if (_client == null || !_client.IsInitialized)
            {
                _logger.LogWarning("Discord RPC не инициализирован");
                return;
            }

            try
            {
                var presence = new RichPresence
                {
                    Details = details ?? "🖱️ CursorVerse",
                    State = state ?? "Персонализация Windows",
                    Assets = new Assets
                    {
                        LargeImageKey = "cursorverse_logo",
                        LargeImageText = "CursorVerse - Windows Customization"
                    },
                    Timestamps = new Timestamps
                    {
                        Start = _startTime
                    },
                    Buttons = new[]
                    {
                        new Button { Label = "📱 Telegram: t.me/CursorVerse", Url = "https://t.me/CursorVerse" }
                    }
                };

                _client.SetPresence(presence);
                _logger.LogDebug("Discord presence обновлен: {Details} | {State}", details, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления Discord presence");
            }
        }

        public void Shutdown()
        {
            _client?.Dispose();
            _logger.LogInformation("Discord RPC остановлен");
        }
    }
}
