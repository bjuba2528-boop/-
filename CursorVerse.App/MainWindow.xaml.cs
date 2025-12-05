using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CursorVerse.Services;

namespace CursorVerse.App
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly CursorManager _cursorManager;
        private readonly DPETEngine _dpetEngine;
        private readonly FavoritesService _favoritesService;
        private readonly DiscordRpcService _discordRpc;
        private readonly LucyAIService _lucyAI;
        private readonly HotkeyService _hotkeyService;
        private readonly SystemTrayService _systemTray;
        private readonly WallpaperManager _wallpaperManager;
        private readonly TaskbarCustomizer _taskbarCustomizer;
        private readonly NotificationCenter _notificationCenter;
        private readonly AutostartService _autostartService;
        private readonly ThemeManager _themeManager;
        private readonly WebServerService _webServer;

        public MainWindow(
            ILogger<MainWindow> logger,
            CursorManager cursorManager,
            DPETEngine dpetEngine,
            FavoritesService favoritesService,
            DiscordRpcService discordRpc,
            LucyAIService lucyAI,
            HotkeyService hotkeyService,
            SystemTrayService systemTray,
            WallpaperManager wallpaperManager,
            TaskbarCustomizer taskbarCustomizer,
            NotificationCenter notificationCenter,
            AutostartService autostartService,
            ThemeManager themeManager,
            WebServerService webServer)
        {
            InitializeComponent();
            
            _logger = logger;
            _cursorManager = cursorManager;
            _dpetEngine = dpetEngine;
            _favoritesService = favoritesService;
            _discordRpc = discordRpc;
            _lucyAI = lucyAI;
            _hotkeyService = hotkeyService;
            _systemTray = systemTray;
            _wallpaperManager = wallpaperManager;
            _taskbarCustomizer = taskbarCustomizer;
            _notificationCenter = notificationCenter;
            _autostartService = autostartService;
            _themeManager = themeManager;
            _webServer = webServer;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Начало инициализации...");
                
                // Инициализация WebView2
                _logger.LogInformation("Инициализация WebView2...");
                await webView.EnsureCoreWebView2Async();
                
                // Настройка моста C# <-> JavaScript
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // Запуск локального веб-сервера
                _logger.LogInformation("Запуск веб-сервера...");
                var wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                await _webServer.StartAsync(wwwrootPath, 3000);
                
                // Загрузка React приложения с локального сервера
                _logger.LogInformation("Загрузка приложения с http://127.0.0.1:3000");
                webView.Source = new Uri("http://127.0.0.1:3000/");

                // Инициализация сервисов (с обработкой ошибок)
                try
                {
                    _logger.LogInformation("Инициализация CursorManager...");
                    await _cursorManager.InitializeAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка инициализации CursorManager"); }

                try
                {
                    _logger.LogInformation("Инициализация DPETEngine...");
                    await _dpetEngine.InitializeAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка инициализации DPETEngine"); }

                try
                {
                    _logger.LogInformation("Инициализация FavoritesService...");
                    await _favoritesService.InitializeAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка инициализации FavoritesService"); }

                try
                {
                    _logger.LogInformation("Инициализация DiscordRpc...");
                    _discordRpc.Initialize();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка инициализации DiscordRpc"); }

                try
                {
                    _logger.LogInformation("Инициализация LucyAI...");
                    await _lucyAI.InitializeAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка инициализации LucyAI"); }

                try
                {
                    _logger.LogInformation("Регистрация горячих клавиш...");
                    _hotkeyService.RegisterHotkeys();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка регистрации горячих клавиш"); }

                try
                {
                    _logger.LogInformation("Инициализация SystemTray...");
                    _systemTray.Initialize(this);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Ошибка инициализации SystemTray"); }

                _logger.LogInformation("CursorVerse успешно запущен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка инициализации приложения");
                MessageBox.Show($"Ошибка запуска: {ex.Message}\n\n{ex.StackTrace}", "CursorVerse", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // WebMessageAsJson уже содержит JSON строку, но она может быть экранирована
                var rawMessage = e.WebMessageAsJson;
                _logger.LogDebug("Raw WebMessage: {Message}", rawMessage);
                
                // Если сообщение пришло как строка в JSON (экранированный JSON), нужно распарсить дважды
                dynamic message;
                try 
                {
                    // Первый парсинг - получаем строку
                    var parsed = JsonConvert.DeserializeObject<dynamic>(rawMessage);
                    
                    // Если результат - строка, парсим ещё раз
                    if (parsed is string jsonString)
                    {
                        message = JsonConvert.DeserializeObject<dynamic>(jsonString);
                    }
                    else
                    {
                        message = parsed;
                    }
                }
                catch
                {
                    // Пробуем TryParseAsJson
                    message = JsonConvert.DeserializeObject<dynamic>(e.TryGetWebMessageAsString() ?? rawMessage);
                }
                
                string? messageType = message?.type?.ToString();
                
                _logger.LogInformation("Parsed message type: {Type}", messageType);
                
                // Обработка Tauri-стиля invoke
                if (messageType == "tauri_invoke")
                {
                    string? command = message?.command?.ToString();
                    int? requestId = (int?)message?.requestId;
                    var args = message?.args;
                    
                    _logger.LogInformation("Tauri invoke: {Command} (requestId: {RequestId})", command, requestId);
                    
                    object? result = null;
                    
                    switch (command)
                    {
                        case "get_cursor_library":
                        case "scan_cursor_library":
                            _logger.LogInformation("Загрузка библиотеки курсоров...");
                            result = await _cursorManager.GetCursorLibraryAsync();
                            _logger.LogInformation("Найдено курсоров: {Count}", (result as System.Collections.ICollection)?.Count ?? 0);
                            break;
                        
                        case "get_dpet_list":
                        case "scan_dpet":
                            result = await _dpetEngine.GetPetListAsync();
                            break;
                        
                        case "apply_cursor":
                            // Может прийти либо scheme объект, либо просто name/id
                            var scheme = args?.scheme;
                            string? schemeName = null;
                            
                            if (scheme != null)
                            {
                                schemeName = scheme.name?.ToString() ?? scheme.Name?.ToString();
                            }
                            else
                            {
                                schemeName = args?.cursor_id?.ToString() ?? args?.cursorId?.ToString() ?? args?.name?.ToString();
                            }
                            
                            if (!string.IsNullOrEmpty(schemeName))
                            {
                                await _cursorManager.ApplyCursorAsync(schemeName);
                                result = "Cursor applied successfully";
                            }
                            break;
                        
                        case "reset_cursor":
                        case "restore_cursor":
                            // Сброс к стандартному курсору
                            await _cursorManager.ResetCursorAsync();
                            result = "Cursor reset successfully";
                            break;
                        
                        case "spawn_dpet":
                            string? petId = args?.pet_id?.ToString() ?? args?.petId?.ToString();
                            if (!string.IsNullOrEmpty(petId))
                            {
                                await _dpetEngine.SpawnPetAsync(petId);
                                result = new { success = true };
                            }
                            break;
                        
                        case "is_autostart_enabled":
                            result = _autostartService.IsEnabled();
                            break;
                        
                        case "enable_autostart":
                            _autostartService.Enable();
                            result = true;
                            break;
                        
                        case "disable_autostart":
                            _autostartService.Disable();
                            result = true;
                            break;
                        
                        case "get_favorites":
                            // Возвращаем пустой массив избранных
                            result = new string[0];
                            break;
                        
                        case "get_favorite_pets":
                            result = await _favoritesService.GetFavoritePetsAsync();
                            break;
                        
                        case "get_favorite_cursors":
                            result = await _favoritesService.GetFavoriteCursorsAsync();
                            break;
                        
                        case "add_favorite_pet":
                            var addPetId = args?.pet_id?.ToString() ?? args?.petId?.ToString();
                            if (!string.IsNullOrEmpty(addPetId))
                            {
                                await _favoritesService.AddFavoritePetAsync(addPetId);
                                result = true;
                            }
                            break;
                        
                        case "remove_favorite_pet":
                            var removePetId = args?.pet_id?.ToString() ?? args?.petId?.ToString();
                            if (!string.IsNullOrEmpty(removePetId))
                            {
                                await _favoritesService.RemoveFavoritePetAsync(removePetId);
                                result = true;
                            }
                            break;
                        
                        case "is_favorite_pet":
                            var checkPetId = args?.pet_id?.ToString() ?? args?.petId?.ToString();
                            if (!string.IsNullOrEmpty(checkPetId))
                            {
                                result = await _favoritesService.IsFavoritePetAsync(checkPetId);
                            }
                            break;
                        
                        case "add_favorite_cursor":
                            var addCursorId = args?.cursor_id?.ToString() ?? args?.cursorId?.ToString();
                            if (!string.IsNullOrEmpty(addCursorId))
                            {
                                await _favoritesService.AddFavoriteCursorAsync(addCursorId);
                                result = true;
                            }
                            break;
                        
                        case "remove_favorite_cursor":
                            var removeCursorId = args?.cursor_id?.ToString() ?? args?.cursorId?.ToString();
                            if (!string.IsNullOrEmpty(removeCursorId))
                            {
                                await _favoritesService.RemoveFavoriteCursorAsync(removeCursorId);
                                result = true;
                            }
                            break;
                        
                        case "is_favorite_cursor":
                            var checkCursorId = args?.cursor_id?.ToString() ?? args?.cursorId?.ToString();
                            if (!string.IsNullOrEmpty(checkCursorId))
                            {
                                result = await _favoritesService.IsFavoriteCursorAsync(checkCursorId);
                            }
                            break;
                        
                        case "get_recommended_pets":
                            var count = (int?)args?.count ?? 3;
                            result = await _favoritesService.GetRecommendedPetsAsync(count);
                            break;
                        
                        case "spawn_random_favorite_pet":
                            var randomPetId = await _favoritesService.SpawnRandomFavoritePetAsync();
                            if (randomPetId != null)
                            {
                                await _dpetEngine.SpawnPetAsync(randomPetId);
                                result = new { success = true, pet_id = randomPetId };
                            }
                            else
                            {
                                result = new { success = false, message = "No favorite pets" };
                            }
                            break;
                        
                        case "get_preview_base64":
                            string? previewPath = args?.path?.ToString();
                            if (!string.IsNullOrEmpty(previewPath) && File.Exists(previewPath))
                            {
                                result = _cursorManager.ConvertImageToBase64(previewPath);
                            }
                            break;
                        
                        case "download_cursor_library":
                        case "download_cursorlib":
                            result = await DownloadCursorLibraryAsync();
                            break;
                        
                        case "get_gemini_api_key":
                            result = LucyAIService.GeminiApiKey;
                            break;
                        
                        case "set_gemini_api_key":
                            if (args is string newKey && !string.IsNullOrEmpty(newKey))
                            {
                                LucyAIService.GeminiApiKey = newKey;
                                _logger.LogInformation("Gemini API Key обновлён");
                                result = new { success = true, message = "API Key обновлён" };
                            }
                            else
                            {
                                result = new { success = false, message = "Некорректный ключ" };
                            }
                            break;
                        
                        case "dpet_load_packages":
                            result = await _dpetEngine.GetPetPackagesAsync();
                            break;
                        
                        case "dpet_get_active_pets":
                            result = _dpetEngine.GetActivePets();
                            break;
                        
                        case "dpet_spawn_pet":
                            var spawnPetId = args?.pet_id?.ToString() ?? args?.petId?.ToString();
                            if (!string.IsNullOrEmpty(spawnPetId))
                            {
                                await _dpetEngine.SpawnPetAsync(spawnPetId);
                                result = new { success = true };
                            }
                            break;
                        
                        case "dpet_remove_all":
                            _dpetEngine.RemoveAllPets();
                            result = new { success = true };
                            break;
                        
                        case "init_discord_rpc":
                        case "discord_rpc_connect":
                            try
                            {
                                _discordRpc.Initialize();
                                result = new { success = true };
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Ошибка инициализации Discord RPC");
                                result = new { success = false, error = ex.Message };
                            }
                            break;
                        
                        case "update_discord_presence":
                            var details = args?.details?.ToString();
                            var state = args?.state?.ToString();
                            _discordRpc.UpdatePresence(details, state);
                            result = new { success = true };
                            break;
                        
                        case "dpet_get_pet_data":
                            var getPetId = args?.petId?.ToString();
                            var logPetId = getPetId ?? "unknown";
                            _logger.LogInformation("📦 Запрос данных питомца: {PetId}", (object)logPetId);
                            if (!string.IsNullOrEmpty(getPetId))
                            {
                                var petConfig = await GetPetDataAsync(getPetId);
                                if (petConfig != null)
                                {
                                    _logger.LogInformation("✅ Данные питомца загружены: {PetId}", (object)logPetId);
                                    result = petConfig;
                                }
                                else
                                {
                                    _logger.LogWarning("❌ Питомец не найден: {PetId}", (object)logPetId);
                                    result = null;
                                }
                            }
                            break;
                        
                        case "dpet_get_sprite_sheet":
                            var packageId = args?.packageId?.ToString() ?? args?.package_id?.ToString();
                            var logPackageId = packageId ?? "unknown";
                            _logger.LogInformation("📦 Запрос спрайта для питомца: {PackageId}", (object)logPackageId);
                            if (!string.IsNullOrEmpty(packageId))
                            {
                                var base64Image = await GetPetSpriteSheetAsync(packageId);
                                if (base64Image != null)
                                {
                                    _logger.LogInformation("✅ Спрайт загружен, длина base64: {Length}", (object)base64Image.Length);
                                    result = new { base64Image = base64Image, success = true };
                                }
                                else
                                {
                                    _logger.LogWarning("❌ Спрайт не найден для: {PackageId}", (object)logPackageId);
                                    result = new { base64Image = (string?)null, success = false };
                                }
                            }
                            break;
                        
                        default:
                            _logger.LogWarning("Неизвестная Tauri команда: {Command}", command);
                            result = null;
                            break;
                    }
                    
                    // Отправляем ответ обратно в JS
                    if (requestId.HasValue)
                    {
                        await SendTauriResponse(requestId.Value, command ?? "", result);
                    }
                    return;
                }
                
                // Старый формат команд
                string? cmd = message?.command?.ToString();

                _logger.LogDebug("Получена команда из UI: {Command}", cmd);

                switch (cmd)
                {
                    case "get_cursor_library":
                        var cursors = await _cursorManager.GetCursorLibraryAsync();
                        await SendToWebView("cursor_library_response", cursors);
                        break;

                    case "apply_cursor":
                        string cursorId = message?.data?.cursorId;
                        await _cursorManager.ApplyCursorAsync(cursorId);
                        break;

                    case "get_dpet_list":
                        var pets = await _dpetEngine.GetPetListAsync();
                        await SendToWebView("dpet_list_response", pets);
                        break;

                    case "spawn_dpet":
                        string petId = message?.data?.petId;
                        await _dpetEngine.SpawnPetAsync(petId);
                        break;

                    case "lucy_speak":
                        string text = message?.data?.text;
                        await _lucyAI.SpeakAsync(text);
                        break;

                    case "lucy_listen":
                        var result = await _lucyAI.ListenAsync();
                        await SendToWebView("lucy_listen_response", result);
                        break;

                    case "update_discord_presence":
                        _discordRpc.UpdatePresence(
                            message?.data?.details?.ToString(),
                            message?.data?.state?.ToString()
                        );
                        break;

                    case "get_spotlight_wallpapers":
                        var wallpapers = await _wallpaperManager.GetSpotlightWallpapersAsync();
                        await SendToWebView("spotlight_wallpapers_response", wallpapers);
                        break;

                    case "set_wallpaper":
                        string wallpaperPath = message?.data?.path;
                        _wallpaperManager.SetWallpaper(wallpaperPath);
                        break;

                    case "hide_taskbar":
                        _taskbarCustomizer.HideWindowsTaskbar();
                        break;

                    case "show_taskbar":
                        _taskbarCustomizer.ShowWindowsTaskbar();
                        break;

                    case "show_notification":
                        string notifTitle = message?.data?.title;
                        string notifMessage = message?.data?.message;
                        _notificationCenter.ShowNotification(notifTitle, notifMessage);
                        break;

                    case "get_autostart_status":
                        var isEnabled = _autostartService.IsEnabled();
                        await SendToWebView("autostart_status_response", new { enabled = isEnabled });
                        break;

                    case "set_autostart":
                        bool enable = message?.data?.enable ?? false;
                        if (enable)
                            _autostartService.Enable();
                        else
                            _autostartService.Disable();
                        break;

                    case "apply_theme":
                        _themeManager.ApplySystemTheme();
                        break;

                    default:
                        _logger.LogWarning("Неизвестная команда: {Command}", cmd);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки сообщения из WebView");
            }
        }

        private async Task SendToWebView(string eventName, object data)
        {
            var message = new { @event = eventName, data };
            var json = JsonConvert.SerializeObject(message);
            await webView.ExecuteScriptAsync($"window.postMessage({json}, '*')");
        }

        private async Task SendTauriResponse(int requestId, string command, object? data)
        {
            var message = new { 
                @event = command + "_response", 
                requestId = requestId,
                data = data 
            };
            var json = JsonConvert.SerializeObject(message);
            _logger.LogDebug("Отправка Tauri ответа: requestId={RequestId}", requestId);
            await webView.ExecuteScriptAsync($"window.postMessage({json}, '*')");
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task<object> DownloadCursorLibraryAsync()
        {
            try
            {
                _logger.LogInformation("Начало установки CursorLib...");
                
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var targetDir = Path.Combine(localAppData, "CursorVerse");
                var zipPath = Path.Combine(Path.GetTempPath(), "CursorLib.zip");
                var tempExtractPath = Path.Combine(Path.GetTempPath(), "CursorLib_Extract");
                
                const string url = "https://github.com/ShustovCarleone/Cursorlib/releases/download/v1.4.0/CursorLib.zip";
                
                _logger.LogInformation("Скачивание с {Url}", url);
                
                // Скачиваем архив
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    
                    await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                
                _logger.LogInformation("Архив скачан: {Size} байт", new FileInfo(zipPath).Length);
                
                // Распаковываем во временную папку
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);
                
                _logger.LogInformation("Распаковка во временную папку");
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempExtractPath, true);
                
                // Ищем папку CursorVerse внутри распакованного архива
                var cursorVerseFolder = Path.Combine(tempExtractPath, "CursorVerse");
                
                if (Directory.Exists(cursorVerseFolder))
                {
                    // Если внутри архива есть папка CursorVerse
                    _logger.LogInformation("Найдена внутренняя папка CursorVerse");
                    
                    // Удаляем старую папку CursorVerse если она существует
                    if (Directory.Exists(targetDir))
                    {
                        _logger.LogInformation("Удаление старой папки {TargetDir}", targetDir);
                        Directory.Delete(targetDir, true);
                    }
                    
                    // Перемещаем найденную папку CursorVerse на место
                    _logger.LogInformation("Перемещение папки CursorVerse");
                    Directory.Move(cursorVerseFolder, targetDir);
                }
                else
                {
                    // Если папки CursorVerse нет, удаляем старую и перемещаем всё содержимое
                    _logger.LogInformation("Папка CursorVerse не найдена в архиве, перемещаем всё содержимое");
                    
                    if (Directory.Exists(targetDir))
                    {
                        _logger.LogInformation("Удаление старой папки {TargetDir}", targetDir);
                        Directory.Delete(targetDir, true);
                    }
                    
                    Directory.CreateDirectory(targetDir);
                    
                    foreach (var dir in Directory.GetDirectories(tempExtractPath))
                    {
                        var dirName = Path.GetFileName(dir);
                        Directory.Move(dir, Path.Combine(targetDir, dirName));
                    }
                    
                    foreach (var file in Directory.GetFiles(tempExtractPath))
                    {
                        var fileName = Path.GetFileName(file);
                        File.Move(file, Path.Combine(targetDir, fileName));
                    }
                }
                
                // Очистка
                File.Delete(zipPath);
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);
                
                _logger.LogInformation("CursorLib успешно установлен!");
                return new { success = true, message = "Библиотека курсоров успешно установлена" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка установки CursorLib");
                return new { success = false, error = ex.Message };
            }
        }

        private async Task<object?> GetPetDataAsync(string petId)
        {
            try
            {
                // Ищем питомца в CustomPets или bundled-pets
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
                        _logger.LogWarning("Папка питомца не найдена: {PetId}", petId);
                        return null;
                    }
                }

                // Ищем JSON конфиг
                var jsonFiles = Directory.GetFiles(customPetsPath, "*.json");
                if (jsonFiles.Length == 0)
                {
                    _logger.LogWarning("JSON конфиг не найден для питомца: {PetId}", petId);
                    return null;
                }

                var json = await File.ReadAllTextAsync(jsonFiles[0]);
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                
                // Возвращаем полные данные питомца
                return new
                {
                    package_id = petId,
                    name = config?.name?.ToString() ?? petId,
                    state = "stand",
                    config = config,
                    animations = config?.animations ?? new { }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки данных питомца: {PetId}", petId);
                return null;
            }
        }

        private async Task<string?> GetPetSpriteSheetAsync(string packageId)
        {
            try
            {
                // Ищем в CustomPets
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var customPetsPath = Path.Combine(localAppData, "CursorVerse", "CustomPets", packageId);
                
                if (!Directory.Exists(customPetsPath))
                {
                    // Ищем в bundled-pets
                    var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled-pets", packageId);
                    if (Directory.Exists(bundledPath))
                    {
                        customPetsPath = bundledPath;
                    }
                    else
                    {
                        _logger.LogWarning("Папка питомца не найдена: {PackageId}", packageId);
                        return null;
                    }
                }

                // Ищем PNG файл (полный spritesheet)
                var pngFiles = Directory.GetFiles(customPetsPath, "*.png");
                if (pngFiles.Length == 0)
                {
                    _logger.LogWarning("PNG файл не найден для питомца: {PackageId}", packageId);
                    return null;
                }

                var spritePath = pngFiles[0];
                _logger.LogInformation("🖼️ Загрузка полного спрайтшита: {Path}", spritePath);
                
                // Возвращаем ВЕСЬ спрайтшит в base64 (не только первый кадр!)
                var imageBytes = await File.ReadAllBytesAsync(spritePath);
                var base64 = Convert.ToBase64String(imageBytes);
                _logger.LogInformation("✅ Спрайтшит загружен, размер: {Size} bytes", imageBytes.Length);
                return base64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки spritesheet для питомца: {PackageId}", packageId);
                return null;
            }
        }

        private string? ExtractFirstFrame(string spritePath, int frameWidth, int frameHeight)
        {
            try
            {
                using var originalImage = System.Drawing.Image.FromFile(spritePath);
                using var firstFrame = new System.Drawing.Bitmap(frameWidth, frameHeight);
                using var graphics = System.Drawing.Graphics.FromImage(firstFrame);
                
                // Копируем первый кадр (0, 0)
                graphics.DrawImage(originalImage, 
                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                    System.Drawing.GraphicsUnit.Pixel);

                // Конвертируем в base64
                using var ms = new MemoryStream();
                firstFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка извлечения первого кадра: {Path}", spritePath);
                return null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _webServer.Stop();
            _discordRpc.Shutdown();
            _hotkeyService.UnregisterHotkeys();
            _systemTray.Dispose();
            base.OnClosed(e);
        }
    }
}
