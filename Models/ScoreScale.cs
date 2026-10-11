using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Одна запись шкалы: первичный балл → вторичный (100-балльный).
    /// </summary>
    public class ScoreScaleEntry
    {
        [JsonPropertyName("primary")]
        public int Primary { get; set; }

        [JsonPropertyName("secondary")]
        public int Secondary { get; set; }
    }

    /// <summary>
    /// Шкала перевода первичных баллов в 100-балльные для всех предметов.
    /// Ключ словаря — subject_id (math_profile, russian, informatics).
    /// </summary>
    public class ScoreScale
    {
        [JsonPropertyName("math_profile")]
        public List<ScoreScaleEntry>? MathProfile { get; set; }

        [JsonPropertyName("russian")]
        public List<ScoreScaleEntry>? Russian { get; set; }

        [JsonPropertyName("informatics")]
        public List<ScoreScaleEntry>? Informatics { get; set; }
    }
}