using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EgeTracker.Models
{
    /// <summary>
    /// Предмет (ЕГЭ или практический курс: linux, english).
    /// </summary>
    public class Subject : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private bool _isEge;
        private string? _scoreScaleKey;
        private double _progressPercent;
        private int _tasksSolved;
        private int _tasksCorrect;
        private int _primaryScore;
        private int _secondaryScore;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Является ли предмет ЕГЭ (учитывается ли шкала перевода в 100-балльную).
        /// </summary>
        public bool IsEge
        {
            get => _isEge;
            set { _isEge = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Ключ для score_scale.json (math_profile, russian, informatics). null для linux/english.
        /// </summary>
        public string? ScoreScaleKey
        {
            get => _scoreScaleKey;
            set { _scoreScaleKey = value; OnPropertyChanged(); }
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            set { _progressPercent = value; OnPropertyChanged(); }
        }

        public int TasksSolved
        {
            get => _tasksSolved;
            set { _tasksSolved = value; OnPropertyChanged(); }
        }

        public int TasksCorrect
        {
            get => _tasksCorrect;
            set { _tasksCorrect = value; OnPropertyChanged(); }
        }

        public int PrimaryScore
        {
            get => _primaryScore;
            set { _primaryScore = value; OnPropertyChanged(); }
        }

        public int SecondaryScore
        {
            get => _secondaryScore;
            set { _secondaryScore = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Topic> Topics { get; set; } = new ObservableCollection<Topic>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}