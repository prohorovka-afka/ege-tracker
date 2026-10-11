using System;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Состояние приложения — сохраняется в AppState.json при выходе,
    /// восстанавливается при запуске.
    /// </summary>
    public class AppState
    {
        [JsonPropertyName("last_tab")]
        public string LastTab { get; set; } = "home";

        [JsonPropertyName("last_subject_id")]
        public string? LastSubjectId { get; set; }

        [JsonPropertyName("last_task_number")]
        public int? LastTaskNumber { get; set; }

        [JsonPropertyName("last_variant_id")]
        public string? LastVariantId { get; set; }

        [JsonPropertyName("last_note_id")]
        public string? LastNoteId { get; set; }

        [JsonPropertyName("notes_font_size")]
        public double NotesFontSize { get; set; } = 16;

        [JsonPropertyName("saved_at")]
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}