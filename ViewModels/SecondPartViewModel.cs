using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    /// <summary>
    /// ViewModel проверки второй части ЕГЭ.
    /// Работает для одного задания — открывается по клику из варианта.
    /// </summary>
    public class SecondPartViewModel : ViewModelBase
    {
        private readonly SecondPartChecker _checker = new();

        public TaskItem Task { get; }
        public string SubjectId { get; }
        public bool IsRussian => SubjectId == "russian";
        public bool IsInformatics => SubjectId == "informatics";
        public bool IsMath => SubjectId == "math_profile";

        private string _userAnswer = string.Empty;
        public string UserAnswer
        {
            get => _userAnswer;
            set => SetField(ref _userAnswer, value);
        }

        private string _checkResult = string.Empty;
        public string CheckResult
        {
            get => _checkResult;
            set => SetField(ref _checkResult, value);
        }

        private bool _showResult;
        public bool ShowResult
        {
            get => _showResult;
            set => SetField(ref _showResult, value);
        }

        // Для математики/физики — ручное выставление баллов
        private int _manualScore;
        public int ManualScore
        {
            get => _manualScore;
            set => SetField(ref _manualScore, value);
        }

        public ObservableCollection<int> AvailableScores { get; } = new();
        public List<CriteriaItem> EssayCriteria { get; } = new();
        public ObservableCollection<CriteriaGrade> EssayGrades { get; } = new();

        public ICommand CheckCommand { get; }
        public ICommand SaveScoreCommand { get; }

        public SecondPartViewModel(TaskItem task, string subjectId)
        {
            Task = task;
            SubjectId = subjectId;

            // Варианты баллов 0..MaxPrimaryScore
            for (int i = 0; i <= Math.Max(1, task.MaxPrimaryScore); i++)
                AvailableScores.Add(i);

            // Критерии для сочинения
            if (IsRussian && task.AnswerCriteria != null)
            {
                EssayCriteria.Add(new CriteriaItem { Name = "Содержание",  MaxPoints = 1, Keywords = task.AnswerCriteria });
                EssayCriteria.Add(new CriteriaItem { Name = "Речь",        MaxPoints = 1, Keywords = new() });
                EssayCriteria.Add(new CriteriaItem { Name = "Грамотность", MaxPoints = 2, Keywords = new() });
            }

            CheckCommand = new RelayCommand(_ => Check());
            SaveScoreCommand = new RelayCommand(_ => SaveScore(), _ => ShowResult);
        }

        private void Check()
        {
            if (Task == null) return;
            ShowResult = true;

            if (IsRussian && EssayCriteria.Count > 0)
            {
                // Оценка сочинения
                var grades = _checker.GradeEssay(UserAnswer, EssayCriteria);
                EssayGrades.Clear();
                int total = 0;

                foreach (var c in EssayCriteria)
                {
                    int pts = grades.TryGetValue(c.Name, out var g) ? g : 0;
                    EssayGrades.Add(new CriteriaGrade { Name = c.Name, Points = pts, Max = c.MaxPoints });
                    total += pts;
                }

                CheckResult = $"Предварительная оценка: {total} баллов (из {EssayCriteria.Sum(c => c.MaxPoints)}). " +
                              "Проверьте сочинение вручную и скорректируйте балл.";
                ManualScore = total;
            }
            else if (IsInformatics)
            {
                bool ok = _checker.CheckMultipleVariants(Task.CorrectAnswer, UserAnswer);
                CheckResult = ok
                    ? "✔ Верно! (совпало с одним из допустимых вариантов)"
                    : $"✘ Неверно. Правильный ответ: {Task.CorrectAnswer}";
                ManualScore = ok ? Task.MaxPrimaryScore : 0;
            }
            else
            {
                // Математика/физика — показываем эталон + просим выставить балл вручную
                CheckResult = $"Эталонный ответ:\n{Task.CorrectAnswer}\n\n" +
                              "Оцените свою работу и выставьте балл вручную.";
            }
        }

        private void SaveScore()
        {
            var result = new TaskResult
            {
                TaskId = Task.Id,
                SubjectId = SubjectId,
                KimPosition = Task.KimPosition,
                UserAnswer = UserAnswer,
                CorrectAnswer = Task.CorrectAnswer,
                IsCorrect = ManualScore >= Task.MaxPrimaryScore / 2,
                PrimaryScore = ManualScore,
                Timestamp = DateTime.Now,
            };

            App.ProgressService.RegisterTaskResult(result);
            CheckResult += $"\n\n✅ Балл {ManualScore} сохранён.";
        }
    }

    public class CriteriaGrade : ViewModelBase
    {
        public string Name { get; set; } = string.Empty;

        private int _points;
        public int Points
        {
            get => _points;
            set { if (SetField(ref _points, value)) OnPropertyChanged(nameof(Display)); }
        }

        public int Max { get; set; }

        public string Display => $"{Name}: {Points} / {Max}";
    }
}