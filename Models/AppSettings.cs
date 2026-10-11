using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    public class AppSettings
    {
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "light";

        [JsonPropertyName("background_image_path")]
        public string? BackgroundImagePath { get; set; }

        [JsonPropertyName("background_opacity")]
        public double BackgroundOpacity { get; set; } = 0.35;

        [JsonPropertyName("last_subject_id")]
        public string? LastSubjectId { get; set; }
    }
}