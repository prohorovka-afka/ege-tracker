using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    public class ExamModeSettings
    {
        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = string.Empty;

        [JsonPropertyName("subjectName")]
        public string SubjectName { get; set; } = string.Empty;

        [JsonPropertyName("variantNumber")]
        public int VariantNumber { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("isInformatics")]
        public bool IsInformatics { get; set; }

        [JsonPropertyName("enableHardBlock")]
        public bool EnableHardBlock { get; set; }

        [JsonPropertyName("tasks")]
        public List<TaskItem> Tasks { get; set; } = new();
    }
}
