using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    /// <summary>
    /// ViewModel для работы над ошибками после варианта.
    /// </summary>
    public class ErrorWorkViewModel : ViewModelBase
    {
        private readonly List<TaskItem> _tasks = new List<TaskItem>();
        private int _index;

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

        private string _progressText = string.Empty;
        public string ProgressText
        {
            get => _progressText;
            set => SetField(ref _progressText, value);
        }

        public ICommand CheckAnswerCommand { get; }
        public ICommand OpenVideoCommand { get; }
        public ICommand NextTrainingCommand { get; }

        public ErrorWorkViewModel(IEnumerable<TaskItem> wrongTasks)
        {
            foreach (var t in wrongTasks)
            {
                // По каждому неверному заданию — 15 аналогов того же прототипа.
                var similar = App.TaskService.GetSimilarTasks(t.Subject, t.TaskNumber, 15);
                _tasks.AddRange(similar);
            }
            _index = 0;

            CheckAnswerCommand = new RelayCommand(_ => CheckAnswer());
            OpenVideoCommand = new RelayCommand(_ => OpenVideo());
            NextTrainingCommand = new RelayCommand(_ => Next());

            UpdateCurrent();
        }

        private void UpdateCurrent()
        {
            if (_tasks.Count == 0)
            {
                CurrentTask = null;
                ProgressText = "Нет заданий для отработки.";
                return;
            }
            CurrentTask = _tasks[_index];
            UserAnswer = string.Empty;
            ResultMessage = string.Empty;
            ProgressText = $"Задание {_index + 1} из {_tasks.Count}";
        }

        private void CheckAnswer()
        {
            if (CurrentTask == null) return;
            bool correct = string.Equals(
                (UserAnswer ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", ""),
                (CurrentTask.CorrectAnswer ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", ""),
                StringComparison.OrdinalIgnoreCase);
            ResultMessage = correct ? "Верно!" : $"Неверно. Ответ: {CurrentTask.CorrectAnswer}";

            var result = new TaskResult
            {
                TaskId = CurrentTask.Id,
                SubjectId = CurrentTask.Subject,
                KimPosition = CurrentTask.KimPosition,
                UserAnswer = UserAnswer,
                CorrectAnswer = CurrentTask.CorrectAnswer,
                IsCorrect = correct,
                PrimaryScore = correct ? CurrentTask.MaxPrimaryScore : 0,
                Timestamp = DateTime.Now
            };
            App.ProgressService.RegisterTaskResult(result);
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
            }
        }

        private void Next()
        {
            if (_tasks.Count == 0) return;
            _index = (_index + 1) % _tasks.Count;
            UpdateCurrent();
        }
    }
}