using System.Collections.Generic;
using Newtonsoft.Json;

namespace CursorVerse.Core.Models
{
    public class CursorScheme
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("category")]
        public string? Category { get; set; }
        
        [JsonProperty("cursors")]
        public Dictionary<string, string> Cursors { get; set; } = new();
        
        [JsonProperty("preview")]
        public string? Preview { get; set; }
    }

    public class DPETPet
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("description")]
        public string? Description { get; set; }
        
        [JsonProperty("preview_path")]
        public string? PreviewPath { get; set; }
    }

    public class DPETConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("description")]
        public string? Description { get; set; }
        
        [JsonProperty("img")]
        public string? Img { get; set; }
        
        [JsonProperty("width")]
        public int? Width { get; set; }
        
        [JsonProperty("height")]
        public int? Height { get; set; }
        
        [JsonProperty("bouncing")]
        public int? Bouncing { get; set; }
        
        [JsonProperty("resources")]
        public string? Resources { get; set; }
        
        [JsonProperty("link")]
        public string? Link { get; set; }
        
        [JsonProperty("animePos")]
        public object? AnimePos { get; set; }
        
        public Dictionary<string, DPETAnimation>? Animations { get; set; }
    }

    public class DPETAnimation
    {
        public string Type { get; set; } = string.Empty;
        public int FrameCount { get; set; }
        public int FrameDelay { get; set; }
        public bool Loop { get; set; }
    }
}
