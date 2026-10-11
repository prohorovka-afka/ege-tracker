using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Конфигурация одного предмета для составителя вариантов.
    /// </summary>
    public class SubjectConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isEge")]
        public bool IsEge { get; set; }

        [JsonPropertyName("scoreScaleKey")]
        public string? ScoreScaleKey { get; set; }

        [JsonPropertyName("tasksCount")]
        public int TasksCount { get; set; }

        [JsonPropertyName("taskNumbers")]
        public List<int> TaskNumbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// Контейнер для всех предметов (соответствует subjects.json).
    /// </summary>
    public class SubjectsConfig
    {
        [JsonPropertyName("subjects")]
        public List<SubjectConfig> Subjects { get; set; } = new List<SubjectConfig>();
    }
}