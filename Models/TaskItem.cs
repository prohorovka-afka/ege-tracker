using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EgeTracker.Models
{
    /// <summary>
    /// Задание из банка ФИПИ.
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _subject = string.Empty;
        private int _taskNumber;
        private int _kimPosition;
        private string _taskType = "short_answer";
        private string _question = string.Empty;
        private string? _imagePath;
        private List<string>? _options;
        private string _correctAnswer = string.Empty;
        private int _maxPrimaryScore = 1;
        private string? _videoUrl;
        private List<string>? _prototypes;
        private string? _fipiId;
        private List<string>? _answerCriteria;

        [JsonPropertyName("id")]
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("subject")]
        public string Subject
        {
            get => _subject;
            set { _subject = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("task_number")]
        public int TaskNumber
        {
            get => _taskNumber;
            set { _taskNumber = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("kim_position")]
        public int KimPosition
        {
            get => _kimPosition;
            set { _kimPosition = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("task_type")]
        public string TaskType
        {
            get => _taskType;
            set { _taskType = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("question")]
        public string Question
        {
            get => _question;
            set { _question = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("image_path")]
        public string? ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("options")]
        public List<string>? Options
        {
            get => _options;
            set { _options = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer
        {
            get => _correctAnswer;
            set { _correctAnswer = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("max_primary_score")]
        public int MaxPrimaryScore
        {
            get => _maxPrimaryScore;
            set { _maxPrimaryScore = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("video_url")]
        public string? VideoUrl
        {
            get => _videoUrl;
            set { _videoUrl = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("prototypes")]
        public List<string>? Prototypes
        {
            get => _prototypes;
            set { _prototypes = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("fipi_id")]
        public string? FipiId
        {
            get => _fipiId;
            set { _fipiId = value; OnPropertyChanged(); }
        }

        [JsonPropertyName("answer_criteria")]
        public List<string>? AnswerCriteria
        {
            get => _answerCriteria;
            set { _answerCriteria = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoUrl)
                                && VideoUrl!.StartsWith("http", System.StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}