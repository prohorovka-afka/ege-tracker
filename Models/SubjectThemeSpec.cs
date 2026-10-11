using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Одна тема в спецификации ЕГЭ.
    /// </summary>
    public class ThemeSpec
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("kimPosition")]
        public int KimPosition { get; set; }
    }

    /// <summary>
    /// Спецификация тем предмета — соответствует ФИПИ 2027.
    /// </summary>
    public class SubjectThemeSpec
    {
        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = string.Empty;

        [JsonPropertyName("themes")]
        public List<ThemeSpec> Themes { get; set; } = new List<ThemeSpec>();
    }
}