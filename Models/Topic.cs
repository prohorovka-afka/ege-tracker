using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EgeTracker.Models
{
    /// <summary>
    /// Тема внутри предмета. Прогресс по теме сохраняется в progress.json.
    /// </summary>
    public class Topic : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private double _progressPercent;
        private int _tasksSolved;
        private int _tasksCorrect;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}