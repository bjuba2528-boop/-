using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    public class WebServerService
    {
        private readonly ILogger<WebServerService> _logger;
        private HttpListener? _httpListener;
        private bool _isRunning = false;
        private PetImageHandler? _petImageHandler;
        private DPETEngine? _dpetEngine;

        public WebServerService(ILogger<WebServerService> logger, DPETEngine? dpetEngine = null)
        {
            _logger = logger;
            _petImageHandler = new PetImageHandler(logger);
            _dpetEngine = dpetEngine;
        }

        public async Task StartAsync(string webRootPath, int port = 3000)
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _httpListener.Start();
                _isRunning = true;

                _logger.LogInformation("Web сервер запущен на http://127.0.0.1:{Port}", port);

                // Слушаем запросы в фоне
                _ = Task.Run(() => HandleRequestsAsync(webRootPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запуска web сервера");
            }
        }

        private async Task HandleRequestsAsync(string webRootPath)
        {
            while (_isRunning)
            {
                try
                {
                    HttpListenerContext context = await _httpListener!.GetContextAsync();
                    ProcessRequest(context, webRootPath);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        _logger.LogWarning(ex, "Ошибка обработки запроса");
                    }
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context, string webRootPath)
        {
            try
            {
                string path = context.Request.Url!.LocalPath;
                
                // API для списка питомцев
                if (path == "/api/pets/list" || path == "/api/pets")
                {
                    HandlePetListAsync(context).Wait();
                    return;
                }
                
                // Обрабатываем запросы API для картинок питомцев
                if (path.StartsWith("/api/pets/"))
                {
                    _petImageHandler!.HandleRequestAsync(context).Wait();
                    return;
                }
                
                // Обрабатываем запросы API для preview картинок курсоров
                if (path.StartsWith("/api/cursors/"))
                {
                    HandleCursorPreviewAsync(context, path).Wait();
                    return;
                }
                
                if (path == "/") path = "/index.html";

                string filePath = Path.Combine(webRootPath, path.TrimStart('/'));
                filePath = Path.GetFullPath(filePath); // Нормализуем путь

                // Безопасность: проверяем, что файл находится в webRootPath
                if (!filePath.StartsWith(Path.GetFullPath(webRootPath)))
                {
                    context.Response.StatusCode = 403;
                    context.Response.Close();
                    return;
                }

                if (File.Exists(filePath))
                {
                    byte[] buffer = File.ReadAllBytes(filePath);
                    
                    // Устанавливаем Content-Type
                    string contentType = GetContentType(filePath);
                    context.Response.ContentType = contentType;
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.StatusCode = 200;
                }
                else
                {
                    // Если файл не найден, отправляем index.html (для React router)
                    string indexPath = Path.Combine(webRootPath, "index.html");
                    if (File.Exists(indexPath))
                    {
                        byte[] buffer = File.ReadAllBytes(indexPath);
                        context.Response.ContentType = "text/html; charset=utf-8";
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.StatusCode = 200;
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                    }
                }

                context.Response.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке файла");
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }

        private async Task HandlePetListAsync(HttpListenerContext context)
        {
            try
            {
                _logger.LogInformation("📋 Pet list request");
                
                if (_dpetEngine == null)
                {
                    context.Response.StatusCode = 503;
                    var errorBytes = System.Text.Encoding.UTF8.GetBytes("{\"error\":\"DPET Engine not available\"}");
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    return;
                }

                var pets = await _dpetEngine.GetPetListAsync();
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(pets);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                
                _logger.LogInformation("✅ Pet list sent: {Count} pets", pets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving pet list");
                context.Response.StatusCode = 500;
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private async Task HandleCursorPreviewAsync(HttpListenerContext context, string path)
        {
            try
            {
                // /api/cursors/{cursorId}/preview
                var parts = path.Split('/');
                if (parts.Length < 4 || parts[3] != "preview")
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var cursorId = parts[2];
                _logger.LogInformation("🖼️ Cursor preview request: {CursorId}", cursorId);

                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var cursorPath = Path.Combine(localAppData, "CursorVerse", "Cursors", cursorId);

                if (!Directory.Exists(cursorPath))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                // Ищем PNG или GIF файл для preview
                var imageFiles = Directory.GetFiles(cursorPath, "*.png")
                    .Concat(Directory.GetFiles(cursorPath, "*.gif"))
                    .FirstOrDefault();

                if (imageFiles == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var imageBytes = await File.ReadAllBytesAsync(imageFiles);
                var contentType = imageFiles.EndsWith(".png") ? "image/png" : "image/gif";

                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = imageBytes.Length;
                await context.Response.OutputStream.WriteAsync(imageBytes, 0, imageBytes.Length);

                _logger.LogInformation("✅ Cursor preview served: {CursorId}", cursorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving cursor preview");
                context.Response.StatusCode = 500;
            }
        }

        private static string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "text/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                _ => "application/octet-stream"
            };
        }

        public void Stop()
        {
            _isRunning = false;
            _httpListener?.Stop();
            _httpListener?.Close();
            _logger.LogInformation("Web сервер остановлен");
        }
    }
}
