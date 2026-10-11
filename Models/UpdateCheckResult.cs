using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    public class UpdateCheckResult
    {
        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = string.Empty;

        [JsonPropertyName("subjectName")]
        public string SubjectName { get; set; } = string.Empty;

        [JsonPropertyName("totalFound")]
        public int TotalFound { get; set; }

        [JsonPropertyName("alreadyHave")]
        public int AlreadyHave { get; set; }

        [JsonPropertyName("newTasks")]
        public int NewTasks { get; set; }

        [JsonPropertyName("savedToFile")]
        public bool SavedToFile { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}