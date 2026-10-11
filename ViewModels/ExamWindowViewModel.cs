using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class ExamWindowViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer;
        private readonly ExamVariantBuilder _builder = new();
        private int _remainingSeconds;

        private string _currentScreen = "select";
        public string CurrentScreen
        {
            get => _currentScreen;
            set
            {
                if (SetField(ref _currentScreen, value))
                {
                    OnPropertyChanged(nameof(IsSelectScreen));
                    OnPropertyChanged(nameof(IsExamScreen));
                    OnPropertyChanged(nameof(IsResultsScreen));
                }
            }
        }

        public bool IsSelectScreen  => CurrentScreen == "select";
        public bool IsExamScreen    => CurrentScreen == "exam";
        public bool IsResultsScreen => CurrentScreen == "results";

        public ObservableCollection<SubjectConfig> AvailableSubjects { get; } = new();

        private SubjectConfig? _selectedSubject;
        public SubjectConfig? SelectedSubject
        {
            get => _selectedSubject;
            set => SetField(ref _selectedSubject, value);
        }

        private bool _showInfo;
        public bool ShowInfo
        {
            get => _showInfo;
            set => SetField(ref _showInfo, value);
        }

        public string InfoText =>
            "Russkij yazyk - 27 zadaniy, 210 minut.\n" +
            "Matematika (profil) - 20 zadaniy, 235 minut.\n" +
            "Informatika - 27 zadaniy, 235 minut.\n\n" +
            "Variant formiruetsya avtomaticheski iz banka FIPI.\n" +
            "Dlya matematiki i russkogo vklyuchaetsya zhestkaya blokirovka.";

        public ObservableCollection<ExamTaskEntry> Entries { get; } = new();

        private string _subjectName = string.Empty;
        public string SubjectName
        {
            get => _subjectName;
            set => SetField(ref _subjectName, value);
        }

        private bool _isInformatics;
        public bool IsInformatics
        {
            get => _isInformatics;
            set => SetField(ref _isInformatics, value);
        }

        private string _timerText = "00:00:00";
        public string TimerText
        {
            get => _timerText;
            set => SetField(ref _timerText, value);
        }

        private string _timerColor = "#4F8DF7";
        public string TimerColor
        {
            get => _timerColor;
            set => SetField(ref _timerColor, value);
        }

        private string _resultSummary = string.Empty;
        public string ResultSummary
        {
            get => _resultSummary;
            set => SetField(ref _resultSummary, value);
        }

        public ObservableCollection<ResultRow> ResultRows { get; } = new();

        public ICommand StartExamCommand { get; }
        public ICommand ShowInfoCommand { get; }
        public ICommand FinishExamCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand LaunchVsCodeCommand { get; }
        public ICommand LaunchExcelCommand { get; }
        public ICommand LaunchNotepadCommand { get; }
        public ICommand LaunchCalculatorCommand { get; }
        public ICommand LaunchPaintCommand { get; }

        public event EventHandler? RequestClose;

        public ExamWindowViewModel()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;

            var allowed = new[] { "russian", "math_profile", "informatics" };
            foreach (var cfg in App.SubjectConfigService.Subjects.Where(s => allowed.Contains(s.Id)))
                AvailableSubjects.Add(cfg);

            SelectedSubject = AvailableSubjects.FirstOrDefault();

            StartExamCommand  = new RelayCommand(_ => StartExam(), _ => SelectedSubject != null);
            ShowInfoCommand   = new RelayCommand(_ => ShowInfo = !ShowInfo);
            FinishExamCommand = new RelayCommand(_ => FinishExam());
            CloseCommand      = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));

            LaunchVsCodeCommand     = new RelayCommand(_ => ExternalToolsService.LaunchVsCode());
            LaunchExcelCommand      = new RelayCommand(_ => ExternalToolsService.LaunchExcel());
            LaunchNotepadCommand    = new RelayCommand(_ => ExternalToolsService.LaunchNotepad());
            LaunchCalculatorCommand = new RelayCommand(_ => ExternalToolsService.LaunchCalculator());
            LaunchPaintCommand      = new RelayCommand(_ => ExternalToolsService.LaunchPaint());
        }

        private void StartExam()
        {
            if (SelectedSubject == null) return;

            var tasks = _builder.BuildExamVariant(SelectedSubject.Id);
            if (tasks.Count == 0)
            {
                MessageBox.Show("Bank zadaniy pust.", "Vnimanie",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubjectName = SelectedSubject.Name;
            IsInformatics = SelectedSubject.Id == "informatics";

            Entries.Clear();
            foreach (var t in tasks)
                Entries.Add(new ExamTaskEntry(t));

            _remainingSeconds = ExamVariantBuilder.GetDurationMinutes(SelectedSubject.Id) * 60;
            UpdateTimerText();

            CurrentScreen = "exam";
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                FinishExamInternal(timeExpired: true);
                return;
            }

            _remainingSeconds--;
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
            TimerText = $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            TimerColor = _remainingSeconds <= 900 ? "#F87171" : "#4F8DF7";
        }

        private void FinishExam()
        {
            var confirm = MessageBox.Show(
                "Zavershit ekzamen?",
                "Zavershenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;
            FinishExamInternal(timeExpired: false);
        }

        private void FinishExamInternal(bool timeExpired)
        {
            _timer.Stop();

            int correct = 0;
            int total = Entries.Count;

            ResultRows.Clear();

            foreach (var e in Entries)
            {
                bool ok = CheckAnswer(e.Task, e.UserAnswer);
                if (ok) correct++;

                ResultRows.Add(new ResultRow
                {
                    KimPosition = e.Task.KimPosition,
                    UserAnswer = string.IsNullOrWhiteSpace(e.UserAnswer) ? "-" : e.UserAnswer,
                    CorrectAnswer = string.IsNullOrWhiteSpace(e.Task.CorrectAnswer) ? "-" : e.Task.CorrectAnswer,
                    IsCorrect = ok,
                });
            }

            double percent = total > 0 ? 100.0 * correct / total : 0;

            ResultSummary = timeExpired
                ? $"Vremya vyshlo! Pravilnyh: {correct} iz {total} ({percent:F1}%)"
                : $"Pravilnyh: {correct} iz {total} ({percent:F1}%)";

            var vr = new VariantResult
            {
                SubjectId = SelectedSubject?.Id ?? string.Empty,
                VariantId = $"exam_{DateTime.Now:yyyyMMddHHmmss}",
                PrimaryScore = correct,
                MaxPrimaryScore = total,
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
                    IsCorrect = CheckAnswer(e.Task, e.UserAnswer),
                    PrimaryScore = CheckAnswer(e.Task, e.UserAnswer) ? 1 : 0,
                    Timestamp = DateTime.Now,
                }).ToList(),
            };

            App.ProgressService.RegisterVariantResult(vr);

            CurrentScreen = "results";
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
    }

    public class ResultRow
    {
        public int KimPosition { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }

        public string Status => IsCorrect ? "Verno" : "Neverno";
        public string StatusColor => IsCorrect ? "#34D399" : "#F87171";
        public string DisplayHeader => $"Zadanie {KimPosition}";
        public string DisplayAnswers => $"Vash: {UserAnswer} | Pravilnyy: {CorrectAnswer}";
    }
}
