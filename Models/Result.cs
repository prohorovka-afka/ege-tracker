using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Результат решения одного задания.
    /// </summary>
    public class TaskResult : INotifyPropertyChanged
    {
        private string _taskId = string.Empty;
        private string _subjectId = string.Empty;
        private int _kimPosition;
        private string _userAnswer = string.Empty;
        private string _correctAnswer = string.Empty;
        private bool _isCorrect;
        private int _primaryScore;
        private DateTime _timestamp = DateTime.Now;

        [JsonPropertyName("task_id")]
        public string TaskId
        {
            get => _taskId;
            set { _taskId = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("subject_id")]
        public string SubjectId
        {
            get => _subjectId;
            set { _subjectId = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("kim_position")]
        public int KimPosition
        {
            get => _kimPosition;
            set { _kimPosition = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("user_answer")]
        public string UserAnswer
        {
            get => _userAnswer;
            set { _userAnswer = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer
        {
            get => _correctAnswer;
            set { _correctAnswer = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("is_correct")]
        public bool IsCorrect
        {
            get => _isCorrect;
            set { _isCorrect = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("primary_score")]
        public int PrimaryScore
        {
            get => _primaryScore;
            set { _primaryScore = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Результат решения полного варианта.
    /// </summary>
    public class VariantResult
    {
        [JsonPropertyName("subject_id")]
        public string SubjectId { get; set; } = string.Empty;

        [JsonPropertyName("variant_id")]
        public string VariantId { get; set; } = string.Empty;

        [JsonPropertyName("primary_score")]
        public int PrimaryScore { get; set; }

        [JsonPropertyName("max_primary_score")]
        public int MaxPrimaryScore { get; set; }

        [JsonPropertyName("secondary_score")]
        public int SecondaryScore { get; set; }

        [JsonPropertyName("percent_correct")]
        public double PercentCorrect { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [JsonPropertyName("task_results")]
        public List<TaskResult> TaskResults { get; set; } = new List<TaskResult>();
    }
}