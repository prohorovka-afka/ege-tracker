using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Ответ пользователя на задание 2-й части.
    /// </summary>
    public class SecondPartAnswer
    {
        [JsonPropertyName("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [JsonPropertyName("taskNumber")]
        public int TaskNumber { get; set; }

        [JsonPropertyName("userAnswer")]
        public string UserAnswer { get; set; } = string.Empty;

        [JsonPropertyName("userScore")]
        public int UserScore { get; set; }

        [JsonPropertyName("maxScore")]
        public int MaxScore { get; set; }

        [JsonPropertyName("checkedAt")]
        public DateTime CheckedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Критерий оценки для развёрнутого ответа.
    /// </summary>
    public class CriteriaItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("maxPoints")]
        public int MaxPoints { get; set; }

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();
    }
}