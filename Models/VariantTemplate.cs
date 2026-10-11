using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Шаблон одного варианта ЕГЭ — список позиций КИМ в правильном порядке.
    /// </summary>
    public class VariantTemplate
    {
        [JsonPropertyName("variantNumber")]
        public int VariantNumber { get; set; }

        [JsonPropertyName("subjectId")]
        public string SubjectId { get; set; } = string.Empty;

        /// <summary>
        /// Позиции КИМ в правильном порядке (например, 1, 2, 3, ..., 27).
        /// </summary>
        [JsonPropertyName("kimPositions")]
        public List<int> KimPositions { get; set; } = new List<int>();
    }
}