using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    public class ReferenceItem
    {
        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = string.Empty;

        [JsonPropertyName("subjectName")]
        public string SubjectName { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "html";
    }
}