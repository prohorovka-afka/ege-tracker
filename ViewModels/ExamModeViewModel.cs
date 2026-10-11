using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class ExamModeViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer;
        private int _remainingSeconds;

        public string SubjectName { get; }
        public int VariantNumber { get; }
        public bool IsInformatics { get; }

        public ObservableCollection<ExamTaskEntry> Entries { get; } = new();

        private string _timerText = "00:00:00";
        public string TimerText
        {
            get => _timerText;
            set => SetField(ref _timerText, value);
        }

        private bool _isFinished;
        public bool IsFinished
        {
            get => _isFinished;
            set => SetField(ref _isFinished, value);
        }

        private string _resultText = string.Empty;
        public string ResultText
        {
            get => _resultText;
            set => SetField(ref _resultText, value);
        }

        public ICommand FinishCommand { get; }
        public event EventHandler? RequestClose;

        public ExamModeViewModel(ExamModeSettings settings)
        {
            SubjectName = settings.SubjectName;
            VariantNumber = settings.VariantNumber;
            IsInformatics = settings.IsInformatics;

            foreach (var t in settings.Tasks)
                Entries.Add(new ExamTaskEntry(t));

            _remainingSeconds = settings.DurationMinutes * 60;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();

            UpdateTimerText();

            FinishCommand = new RelayCommand(_ => Finish());
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                Finish();
                return;
            }

            _remainingSeconds--;
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
            TimerText = $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }

        private void Finish()
        {
            if (IsFinished) return;
            IsFinished = true;
            _timer.Stop();

            int correct = 0, total = Entries.Count;

            foreach (var e in Entries)
            {
                if (string.IsNullOrWhiteSpace(e.UserAnswer)) continue;
                bool ok = CheckAnswer(e.Task, e.UserAnswer);
                if (ok) correct++;
            }

            double percent = total > 0 ? 100.0 * correct / total : 0;
            ResultText = $"Правильных: {correct} из {total} ({percent:F1}%)";

            var result = new VariantResult
            {
                SubjectId = Entries.FirstOrDefault()?.Task.Subject ?? string.Empty,
                VariantId = $"exam_{VariantNumber}_{DateTime.Now:yyyyMMddHHmmss}",
                PrimaryScore = correct,
                MaxPrimaryScore = total,
                SecondaryScore = 0,
                PercentCorrect = percent,
                Timestamp = DateTime.Now,
                TaskResults = Entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.UserAnswer))
                    .Select(e => new TaskResult
                    {
                        TaskId = e.Task.Id,
                        SubjectId = e.Task.Subject,
                        KimPosition = e.Task.KimPosition,
                        UserAnswer = e.UserAnswer,
                        CorrectAnswer = e.Task.CorrectAnswer,
                        IsCorrect = CheckAnswer(e.Task, e.UserAnswer),
                        PrimaryScore = CheckAnswer(e.Task, e.UserAnswer) ? 1 : 0,
                        Timestamp = DateTime.Now,
                    }).ToList(),
            };

            App.ProgressService.RegisterVariantResult(result);
        }

        private static bool CheckAnswer(TaskItem task, string userAnswer)
        {
            string u = Normalize(userAnswer);
            string c = Normalize(task.CorrectAnswer);
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(c)) return false;

            if (c.Contains("|"))
                return c.Split('|').Any(v => v == u);

            return u == c;
        }

        private static string Normalize(string s) =>
            (s ?? string.Empty).Trim().Replace(" ", "").Replace(",", ".").ToLowerInvariant();

        public void RequestFinishFromUi()
        {
            var confirm = MessageBox.Show(
                "Завершить экзамен? Ваши ответы будут проверены.",
                "Завершение экзамена",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                Finish();
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class ExamTaskEntry : ViewModelBase
    {
        public TaskItem Task { get; }

        private string _userAnswer = string.Empty;
        public string UserAnswer
        {
            get => _userAnswer;
            set => SetField(ref _userAnswer, value);
        }

        public ExamTaskEntry(TaskItem task)
        {
            Task = task;
        }
    }
}