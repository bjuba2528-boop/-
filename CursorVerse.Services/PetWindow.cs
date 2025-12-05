using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    /// <summary>
    /// Простое окно питомца без WebView2 - просто картинка с анимацией
    /// </summary>
    public class PetWindow : Window
    {
        private readonly ILogger _logger;
        private System.Windows.Controls.Image? _petImage;
        private Random _random = new();
        private string _petId;
        private string _petPath;
        private static readonly HttpClient _httpClient = new();
        
        // Физика
        private double _velocityY = 0;
        private double _velocityX = 0;
        private double _positionX;
        private double _positionY;
        private const double Gravity = 0.5;
        private const double Friction = 0.92;
        private bool _isOnGround = false;
        private DateTime _lastBounceTime = DateTime.Now;

        public PetWindow(string petId, string petPath, ILogger logger)
        {
            _petId = petId;
            _petPath = petPath;
            _logger = logger;

            // Настройка окна
            Width = 128;
            Height = 128;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            // Случайная позиция на экране
            _positionX = _random.Next(100, 800);
            _positionY = _random.Next(100, 600);
            Left = _positionX;
            Top = _positionY;

            Title = $"Pet: {petId}";

            // Создаём Image контрол
            _petImage = new System.Windows.Controls.Image
            {
                Width = 128,
                Height = 128,
                Stretch = System.Windows.Media.Stretch.UniformToFill
            };

            Content = _petImage;

            // Загружаем изображение питомца
            LoadPetImage();

            // Таймер анимации
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS
            };
            timer.Tick += (s, e) => UpdateAnimation();
            timer.Start();

            Closing += (s, e) =>
            {
                timer.Stop();
            };
        }

        private void LoadPetImage()
        {
            try
            {
                _logger.LogInformation("🐾 Загрузка изображения питомца: {PetId}", _petId);

                // Используем HTTP API вместо прямого доступа к файлам
                var imageUrl = $"http://127.0.0.1:3000/api/pets/{_petId}/preview";
                _logger.LogInformation("📡 Loading from: {Url}", imageUrl);

                var task = _httpClient.GetByteArrayAsync(imageUrl);
                task.Wait(5000); // Ждём 5 секунд
                
                var imageBytes = task.Result;

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogWarning("❌ Empty response from: {Url}", imageUrl);
                    ShowPlaceholder();
                    return;
                }

                using var ms = new MemoryStream(imageBytes);
                var bitmap = new Bitmap(ms);
                _petImage!.Source = BitmapToImageSource(bitmap);
                _logger.LogInformation("✅ Изображение питомца загружено успешно ({Bytes} bytes)", imageBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка загрузки изображения питомца");
                ShowPlaceholder();
            }
        }

        private Bitmap? ExtractFirstFrame(string imagePath, int frameWidth, int frameHeight)
        {
            try
            {
                using var originalImage = System.Drawing.Image.FromFile(imagePath);
                var firstFrame = new Bitmap(frameWidth, frameHeight);
                using var graphics = Graphics.FromImage(firstFrame);

                graphics.DrawImage(originalImage,
                    new Rectangle(0, 0, frameWidth, frameHeight),
                    new Rectangle(0, 0, frameWidth, frameHeight),
                    GraphicsUnit.Pixel);

                return firstFrame;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка извлечения первого кадра");
                return null;
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private void ShowPlaceholder()
        {
            try
            {
                // Создаём заглушку - розовый круг
                var bitmap = new Bitmap(128, 128);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Transparent);

                // Розовый круг
                using var brush = new SolidBrush(Color.FromArgb(255, 105, 180));
                graphics.FillEllipse(brush, 10, 10, 108, 108);

                // Глаза
                graphics.FillEllipse(Brushes.Black, 40, 40, 12, 12);
                graphics.FillEllipse(Brushes.Black, 76, 40, 12, 12);

                // Рот
                graphics.DrawArc(new Pen(Brushes.Black, 2), 50, 60, 28, 20, 0, 180);

                _petImage!.Source = BitmapToImageSource(bitmap);
                _logger.LogInformation("🎨 Заглушка питомца отображена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания заглушки");
            }
        }

        private void UpdateAnimation()
        {
            // Простая физика: гравитация и падение
            _velocityY += Gravity;
            _velocityY *= Friction;
            _velocityX *= Friction;

            _positionY += _velocityY;
            _positionX += _velocityX;

            // Сохраняем в пределах экрана
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth = SystemParameters.PrimaryScreenWidth;

            // Проверка пола
            if (_positionY + 128 >= screenHeight - 30) // -30 для панели задач
            {
                _positionY = screenHeight - 158;
                _velocityY = 0;
                _isOnGround = true;

                // Случайный прыжок
                if (DateTime.Now.Subtract(_lastBounceTime).TotalMilliseconds > 3000)
                {
                    _velocityY = -_random.Next(5, 15);
                    _velocityX = _random.Next(-3, 4);
                    _lastBounceTime = DateTime.Now;
                }
            }
            else
            {
                _isOnGround = false;
            }

            // Боковые границы
            if (_positionX < 0)
            {
                _positionX = 0;
                _velocityX = Math.Abs(_velocityX);
            }
            if (_positionX + 128 > screenWidth)
            {
                _positionX = screenWidth - 128;
                _velocityX = -Math.Abs(_velocityX);
            }

            Left = _positionX;
            Top = _positionY;
        }
    }
}
