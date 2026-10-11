using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class PrototypesViewModel : ViewModelBase
    {
        private readonly Random _rng = new Random();

        public ObservableCollection<Subject> Subjects { get; } = new ObservableCollection<Subject>();
        public ObservableCollection<int> TaskNumbers { get; } = new ObservableCollection<int>();

        private Subject? _selectedSubject;
        public Subject? SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetField(ref _selectedSubject, value))
                    LoadTaskNumbers();
            }
        }

        private int? _selectedTaskNumber;
        public int? SelectedTaskNumber
        {
            get => _selectedTaskNumber;
            set
            {
                if (SetField(ref _selectedTaskNumber, value))
                    LoadRandomTask();
            }
        }

        private TaskItem? _currentTask;
        public TaskItem? CurrentTask
        {
            get => _currentTask;
            set => SetField(ref _currentTask, value);
        }

        private string _userAnswer = string.Empty;
        public string UserAnswer
        {
            get => _userAnswer;
            set => SetField(ref _userAnswer, value);
        }

        private string _resultMessage = string.Empty;
        public string ResultMessage
        {
            get => _resultMessage;
            set => SetField(ref _resultMessage, value);
        }

        private bool _lastWasCorrect;
        public bool LastWasCorrect
        {
            get => _lastWasCorrect;
            set => SetField(ref _lastWasCorrect, value);
        }

        private bool _showResult;
        public bool ShowResult
        {
            get => _showResult;
            set => SetField(ref _showResult, value);
        }

        // ⭐ Счётчик правильных ответов в текущей сессии
        private int _sessionCorrect;
        public int SessionCorrect
        {
            get => _sessionCorrect;
            set
            {
                if (SetField(ref _sessionCorrect, value))
                    OnPropertyChanged(nameof(TrainingProgressText));
            }
        }

        private int _sessionTotal;
        public int SessionTotal
        {
            get => _sessionTotal;
            set
            {
                if (SetField(ref _sessionTotal, value))
                    OnPropertyChanged(nameof(TrainingProgressText));
            }
        }

        // ⭐ Текст прогресса: «✔ 3 из 5»
        public string TrainingProgressText => SessionTotal == 0
            ? string.Empty
            : $"✔ {SessionCorrect} из {SessionTotal}";

        public ObservableCollection<TaskItem> TrainingTasks { get; } = new ObservableCollection<TaskItem>();
        private int _trainingIndex = 0;

        private bool _inTraining;
        public bool InTraining
        {
            get => _inTraining;
            set => SetField(ref _inTraining, value);
        }

        public ICommand CheckAnswerCommand { get; }
        public ICommand OpenVideoCommand { get; }
        public ICommand StartTrainingCommand { get; }
        public ICommand NextTrainingCommand { get; }
        public ICommand NextRandomCommand { get; }

        public PrototypesViewModel()
        {
            foreach (var s in App.ProgressService.Subjects)
                Subjects.Add(s);

            SelectedSubject = Subjects.FirstOrDefault();

            CheckAnswerCommand  = new RelayCommand(_ => CheckAnswer(), _ => CurrentTask != null && !string.IsNullOrWhiteSpace(UserAnswer));
            OpenVideoCommand    = new RelayCommand(_ => OpenVideo());
            StartTrainingCommand = new RelayCommand(_ => StartTraining());
            NextTrainingCommand  = new RelayCommand(_ => NextTraining());
            NextRandomCommand    = new RelayCommand(_ => LoadRandomTask());
        }

        private void LoadTaskNumbers()
        {
            TaskNumbers.Clear();
            if (SelectedSubject == null) return;
            foreach (var n in App.TaskService.GetTaskNumbers(SelectedSubject.Id))
                TaskNumbers.Add(n);
            SelectedTaskNumber = TaskNumbers.FirstOrDefault();
        }

        private void LoadRandomTask()
        {
            ShowResult = false;
            ResultMessage = string.Empty;
            UserAnswer = string.Empty;
            InTraining = false;
            TrainingTasks.Clear();
            _trainingIndex = 0;

            // Сбрасываем счётчики при смене задания
            SessionCorrect = 0;
            SessionTotal = 0;

            if (SelectedSubject == null || SelectedTaskNumber == null)
            {
                CurrentTask = null;
                return;
            }
            CurrentTask = App.TaskService.GetRandomByPrototype(SelectedSubject.Id, SelectedTaskNumber.Value, _rng);
        }

        private void CheckAnswer()
        {
            if (CurrentTask == null) return;

            bool correct = AnswersMatch(UserAnswer, CurrentTask.CorrectAnswer, CurrentTask.TaskType);
            LastWasCorrect = correct;
            ShowResult = true;

            SessionTotal++;

            // ⭐ Никаких первичных баллов — просто ✔ или ✘
            if (correct)
            {
                SessionCorrect++;
                ResultMessage = "✔ Верно!";
            }
            else
            {
                ResultMessage = $"✘ Неверно. Правильный ответ: {CurrentTask.CorrectAnswer}";
            }

            // Регистрируем результат в прогрессе (без первичных баллов как приоритета)
            var result = new TaskResult
            {
                TaskId = CurrentTask.Id,
                SubjectId = CurrentTask.Subject,
                KimPosition = CurrentTask.KimPosition,
                UserAnswer = UserAnswer,
                CorrectAnswer = CurrentTask.CorrectAnswer,
                IsCorrect = correct,
                PrimaryScore = correct ? 1 : 0,   // ⭐ просто 1 или 0, без max_primary_score
                Timestamp = DateTime.Now
            };
            App.ProgressService.RegisterTaskResult(result);
        }

        private static bool AnswersMatch(string user, string correct, string taskType)
        {
            string u = NormalizeAnswer(user);
            string c = NormalizeAnswer(correct);
            if (string.IsNullOrEmpty(u)) return false;

            if (string.Equals(taskType, "extended_answer", StringComparison.OrdinalIgnoreCase))
            {
                var keywords = c.Split(new[] { ' ', ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (keywords.Length == 0) return false;
                int hits = keywords.Count(k => u.Contains(k));
                return hits >= Math.Max(1, keywords.Length / 2);
            }

            return string.Equals(u, c, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAnswer(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return s.Trim().Replace(" ", "").Replace(",", ".").Replace("\u00A0", "").ToLowerInvariant();
        }

        private void OpenVideo()
        {
            if (CurrentTask?.VideoUrl == null) return;
            string url = CurrentTask.VideoUrl;
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                ResultMessage = "Не удалось открыть ссылку.";
            }
        }

        private void StartTraining()
        {
            if (CurrentTask == null) return;
            TrainingTasks.Clear();
            var similar = App.TaskService.GetSimilarTasks(CurrentTask.Subject, CurrentTask.TaskNumber, 15);
            foreach (var t in similar) TrainingTasks.Add(t);
            _trainingIndex = 0;
            InTraining = TrainingTasks.Count > 0;

            // Сбрасываем счётчики для тренировки
            SessionCorrect = 0;
            SessionTotal = 0;

            if (InTraining)
            {
                CurrentTask = TrainingTasks[0];
                UserAnswer = string.Empty;
                ShowResult = false;
                ResultMessage = string.Empty;
            }
        }

        private void NextTraining()
        {
            if (!InTraining) return;
            _trainingIndex++;
            if (_trainingIndex >= TrainingTasks.Count)
            {
                InTraining = false;
                ResultMessage = $"Тренировка завершена. ✔ {SessionCorrect} из {SessionTotal}.";
                return;
            }
            CurrentTask = TrainingTasks[_trainingIndex];
            UserAnswer = string.Empty;
            ShowResult = false;
            ResultMessage = string.Empty;
        }
    }
}