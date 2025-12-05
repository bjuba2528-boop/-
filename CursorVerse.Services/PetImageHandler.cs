using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    /// <summary>
    /// HTTP обработчик для раздачи preview и sprite картинок питомцев
    /// </summary>
    public class PetImageHandler
    {
        private readonly ILogger _logger;

        public PetImageHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url?.LocalPath ?? "";
                _logger.LogInformation("📸 PetImageHandler request: {Path}", path);

                // /api/pets/{petId}/preview - первый кадр спрайта в формате PNG
                if (path.StartsWith("/api/pets/") && path.EndsWith("/preview"))
                {
                    var petId = path.Replace("/api/pets/", "").Replace("/preview", "");
                    await ServePreviewImage(context, petId);
                    return;
                }

                // /api/pets/{petId}/sprite - полный спрайтsheet в формате PNG  
                if (path.StartsWith("/api/pets/") && path.EndsWith("/sprite"))
                {
                    var petId = path.Replace("/api/pets/", "").Replace("/sprite", "");
                    await ServeSpriteImage(context, petId);
                    return;
                }

                // 404
                context.Response.StatusCode = 404;
                await context.Response.OutputStream.WriteAsync(new byte[] { });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка в PetImageHandler");
                context.Response.StatusCode = 500;
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private async Task ServePreviewImage(HttpListenerContext context, string petId)
        {
            try
            {
                _logger.LogInformation("🖼️ Serving preview for pet: {PetId}", petId);

                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var petPath = Path.Combine(localAppData, "CursorVerse", "CustomPets", petId);

                if (!Directory.Exists(petPath))
                {
                    var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled-pets", petId);
                    if (Directory.Exists(bundledPath))
                    {
                        petPath = bundledPath;
                    }
                    else
                    {
                        _logger.LogWarning("❌ Pet directory not found: {PetId}", petId);
                        context.Response.StatusCode = 404;
                        return;
                    }
                }

                // Ищем JSON для размеров
                var jsonFiles = Directory.GetFiles(petPath, "*.json");
                int spriteWidth = 128;
                int spriteHeight = 128;

                if (jsonFiles.Length > 0)
                {
                    try
                    {
                        var json = File.ReadAllText(jsonFiles[0]);
                        dynamic? config = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                        if (config?.width != null) spriteWidth = (int)config.width;
                        if (config?.height != null) spriteHeight = (int)config.height;
                    }
                    catch { }
                }

                // Ищем PNG
                var pngFiles = Directory.GetFiles(petPath, "*.png");
                if (pngFiles.Length == 0)
                {
                    _logger.LogWarning("❌ No PNG found for pet: {PetId}", petId);
                    context.Response.StatusCode = 404;
                    return;
                }

                // Вырезаем первый кадр
                var imageBytes = ExtractFirstFrameAsBytes(pngFiles[0], spriteWidth, spriteHeight);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                context.Response.ContentType = "image/png";
                context.Response.ContentLength64 = imageBytes.Length;
                await context.Response.OutputStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                
                _logger.LogInformation("✅ Preview served: {PetId} ({Bytes} bytes)", petId, imageBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error serving preview for: {PetId}", petId);
                context.Response.StatusCode = 500;
            }
        }

        private async Task ServeSpriteImage(HttpListenerContext context, string petId)
        {
            try
            {
                _logger.LogInformation("🎬 Serving sprite for pet: {PetId}", petId);

                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var petPath = Path.Combine(localAppData, "CursorVerse", "CustomPets", petId);

                if (!Directory.Exists(petPath))
                {
                    var bundledPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundled-pets", petId);
                    if (Directory.Exists(bundledPath))
                    {
                        petPath = bundledPath;
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }

                // Ищем PNG
                var pngFiles = Directory.GetFiles(petPath, "*.png");
                if (pngFiles.Length == 0)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var imageBytes = await File.ReadAllBytesAsync(pngFiles[0]);
                
                context.Response.ContentType = "image/png";
                context.Response.ContentLength64 = imageBytes.Length;
                await context.Response.OutputStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                
                _logger.LogInformation("✅ Sprite served: {PetId} ({Bytes} bytes)", petId, imageBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error serving sprite for: {PetId}", petId);
                context.Response.StatusCode = 500;
            }
        }

        private byte[]? ExtractFirstFrameAsBytes(string imagePath, int frameWidth, int frameHeight)
        {
            try
            {
                using var originalImage = System.Drawing.Image.FromFile(imagePath);
                var firstFrame = new System.Drawing.Bitmap(frameWidth, frameHeight);
                using var graphics = System.Drawing.Graphics.FromImage(firstFrame);

                graphics.DrawImage(originalImage,
                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                    new System.Drawing.Rectangle(0, 0, frameWidth, frameHeight),
                    System.Drawing.GraphicsUnit.Pixel);

                using var ms = new MemoryStream();
                firstFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting first frame from: {Path}", imagePath);
                return null;
            }
        }
    }
}
