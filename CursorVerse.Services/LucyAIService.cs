using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CursorVerse.Services
{
    public class LucyAIService
    {
        private readonly ILogger<LucyAIService> _logger;
        private readonly SpeechSynthesizer _synthesizer;
        private SpeechRecognitionEngine? _recognizer;
        
        // Gemini API Key - значение по умолчанию, но может быть изменено
        private static string _geminiApiKey = "AIzaSyCoi6Vjo1Uax3GqlrHy8mvM1TZb6SYS-OA";
        
        public static string GeminiApiKey
        {
            get => _geminiApiKey;
            set => _geminiApiKey = value;
        }

        public LucyAIService(ILogger<LucyAIService> logger)
        {
            _logger = logger;
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SelectVoiceByHints(VoiceGender.Female);
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Инициализация Lucy AI");
            
            try
            {
                _recognizer = new SpeechRecognitionEngine();
                _recognizer.LoadGrammar(new DictationGrammar());
                _recognizer.SetInputToDefaultAudioDevice();
                
                _logger.LogInformation("Lucy AI инициализирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка инициализации распознавания речи");
            }

            await Task.CompletedTask;
        }

        public async Task SpeakAsync(string text)
        {
            await Task.Run(() =>
            {
                try
                {
                    _synthesizer.SpeakAsync(text);
                    _logger.LogDebug("Lucy говорит: {Text}", text);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка синтеза речи");
                }
            });
        }

        public async Task<string?> ListenAsync()
        {
            if (_recognizer == null)
            {
                _logger.LogWarning("Распознавание речи не инициализировано");
                return null;
            }

            return await Task.Run(() =>
            {
                try
                {
                    _recognizer.RecognizeAsync(RecognizeMode.Single);
                    var result = _recognizer.Recognize(TimeSpan.FromSeconds(10));
                    
                    if (result != null)
                    {
                        _logger.LogDebug("Распознано: {Text}", result.Text);
                        return result.Text;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка распознавания речи");
                }

                return null;
            });
        }
    }
}
