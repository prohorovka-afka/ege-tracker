using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class VariantViewModel : ViewModelBase
    {
        public ObservableCollection<Subject> Subjects { get; } = new ObservableCollection<Subject>();
        public ObservableCollection<VariantEntry> Entries { get; } = new ObservableCollection<VariantEntry>();

        private Subject? _selectedSubject;
        public Subject? SelectedSubject
        {
            get => _selectedSubject;
            set { if (SetField(ref _selectedSubject, value)) { } }
        }

        /// <summary>
        /// Пользовательский вариант (из составителя) — без первичных баллов.
        /// </summary>
        private bool _isCustom;
        public bool IsCustom
        {
            get => _isCustom;
            set => SetField(ref _isCustom, value);
        }

        private string _summary = string.Empty;
        public string Summary
        {
            get => _summary;
            set => SetField(ref _summary, value);
        }

        private bool _hasResults;
        public bool HasResults
        {
            get => _hasResults;
            set => SetField(ref _hasResults, value);
        }

        public List<TaskItem> WrongTasks { get; private set; } = new List<TaskItem>();

        private bool _showWorkOnErrors;
        public bool ShowWorkOnErrors
        {
            get => _showWorkOnErrors;
            set => SetField(ref _showWorkOnErrors, value);
        }

        public ICommand BuildVariantCommand { get; }
        public ICommand CheckVariantCommand { get; }
        public ICommand OpenVideoCommand { get; }
        public ICommand WorkOnErrorsCommand { get; }

        public VariantViewModel()
        {
            foreach (var s in App.ProgressService.Subjects)
                Subjects.Add(s);
            SelectedSubject = Subjects.FirstOrDefault();

            BuildVariantCommand  = new RelayCommand(_ => BuildVariant());
            CheckVariantCommand  = new RelayCommand(_ => CheckVariant());
            OpenVideoCommand     = new RelayCommand(p => OpenVideo(p as VariantEntry));
            WorkOnErrorsCommand  = new RelayCommand(_ => StartWorkOnErrors(),
                                                    _ => ShowWorkOnErrors && WrongTasks.Count > 0);
        }

        private void BuildVariant()
        {
            Entries.Clear();
            Summary = string.Empty;
            HasResults = false;
            ShowWorkOnErrors = false;
            WrongTasks.Clear();
            IsCustom = false;

            if (SelectedSubject == null) return;

            var tasks = App.TaskService.BuildVariant(SelectedSubject.Id);
            foreach (var t in tasks)
                Entries.Add(new VariantEntry(t));
        }

        private void CheckVariant()
        {
            if (SelectedSubject == null || Entries.Count == 0) return;

            int correctCount = 0;
            int primary = 0;
            int maxPrimary = 0;

            foreach (var e in Entries)
            {
                bool ok = AnswersMatch(e.UserAnswer, e.Task.CorrectAnswer, e.Task.TaskType);
                e.IsCorrect = ok;
                e.PrimaryAwarded = ok ? e.Task.MaxPrimaryScore : 0;
                primary += e.PrimaryAwarded;
                maxPrimary += e.Task.MaxPrimaryScore;
                if (ok) correctCount++;
            }

            double percent = Entries.Count == 0 ? 0 : 100.0 * correctCount / Entries.Count;
            string summary;

            if (IsCustom)
            {
                summary = $"✔ Правильных: {correctCount} из {Entries.Count} ({percent:F1}%)";
            }
            else
            {
                summary = $"Набрано: {primary} из {maxPrimary} первичных баллов.\n" +
                          $"Правильных ответов: {correctCount} из {Entries.Count} ({percent:F1}%).";

                if (SelectedSubject.IsEge && !string.IsNullOrEmpty(SelectedSubject.ScoreScaleKey))
                {
                    var secondary = App.ScoreService.ConvertToSecondary(SelectedSubject.ScoreScaleKey!, primary);
                    if (secondary.HasValue)
                        summary += $"\nПеревод: {primary} первичных = {secondary.Value} баллов из 100.";
                }
            }

            Summary = summary;
            HasResults = true;

            WrongTasks = Entries.Where(e => !e.IsCorrect).Select(e => e.Task).ToList();
            ShowWorkOnErrors = WrongTasks.Count > 0;

            var vr = new VariantResult
            {
                SubjectId = SelectedSubject.Id,
                VariantId = Guid.NewGuid().ToString("N"),
                PrimaryScore = primary,
                MaxPrimaryScore = maxPrimary,
                SecondaryScore = 0,
                PercentCorrect = percent,
                Timestamp = DateTime.Now,
                TaskResults = Entries.Select(e => new TaskResult
                {
                    TaskId = e.Task.Id,
                    SubjectId = e.Task.Subject,
                    KimPosition = e.Task.KimPosition,
                    UserAnswer = e.UserAnswer,
                    CorrectAnswer = e.Task.CorrectAnswer,
                    IsCorrect = e.IsCorrect,
                    PrimaryScore = e.PrimaryAwarded,
                    Timestamp = DateTime.Now
                }).ToList()
            };
            App.ProgressService.RegisterVariantResult(vr);

            foreach (var e in Entries) e.Refresh();
        }

        /// <summary>
        /// Загрузить вариант из готового списка заданий (составитель, список вариантов).
        /// </summary>
        public void LoadFromTasks(string subjectId, List<TaskItem> tasks)
        {
            try
            {
                Entries.Clear();
                Summary = string.Empty;
                HasResults = false;
                ShowWorkOnErrors = false;
                WrongTasks.Clear();

                SelectedSubject = Subjects.FirstOrDefault(s => s.Id == subjectId);

                foreach (var t in tasks)
                    Entries.Add(new VariantEntry(t));
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private static bool AnswersMatch(string user, string correct, string taskType)
        {
            string u = (user ?? string.Empty).Trim().Replace(" ", "").Replace(",", ".").ToLowerInvariant();
            string c = (correct ?? string.Empty).Trim().Replace(" ", "").Replace(",", ".").ToLowerInvariant();
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

        private void OpenVideo(VariantEntry? e)
        {
            if (e?.Task.VideoUrl == null) return;
            string url = e.Task.VideoUrl;
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void StartWorkOnErrors()
        {
            try
            {
                if (WrongTasks.Count == 0) return;

                var errorVm = new ErrorWorkViewModel(WrongTasks);
                var errorView = new Views.ErrorWorkView { DataContext = errorVm };

                var window = new Window
                {
                    Title = "Работа над ошибками",
                    Width = 900, Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current?.MainWindow,
                    Content = errorView,
                    Background = (System.Windows.Media.Brush)Application.Current!.Resources["BrushSurface"]
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                MessageBox.Show("Не удалось открыть работу над ошибками.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class VariantEntry : ViewModelBase
    {
        public TaskItem Task { get; }

        private string _userAnswer = string.Empty;
        public string UserAnswer
        {
            get => _userAnswer;
            set => SetField(ref _userAnswer, value);
        }

        private bool _isCorrect;
        public bool IsCorrect
        {
            get => _isCorrect;
            set { if (SetField(ref _isCorrect, value)) OnPropertyChanged(nameof(StatusText)); }
        }

        private int _primaryAwarded;
        public int PrimaryAwarded
        {
            get => _primaryAwarded;
            set { if (SetField(ref _primaryAwarded, value)) OnPropertyChanged(nameof(StatusText)); }
        }

        public string StatusText => IsCorrect ? "✔ Верно" : "✘ Неверно";
        public bool HasVideo => Task.HasVideo;

        public VariantEntry(TaskItem task) { Task = task; }

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsCorrect));
            OnPropertyChanged(nameof(PrimaryAwarded));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(HasVideo));
        }
    }
}