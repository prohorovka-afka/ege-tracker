using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Контейнер сохранения прогресса в progress.json.
    /// </summary>
    public class ProgressData
    {
        [JsonPropertyName("subjects")]
        public List<Subject> Subjects { get; set; } = new List<Subject>();

        [JsonPropertyName("task_results")]
        public List<TaskResult> TaskResults { get; set; } = new List<TaskResult>();

        [JsonPropertyName("variant_results")]
        public List<VariantResult> VariantResults { get; set; } = new List<VariantResult>();
    }
}