using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    public class HomePageViewModel : ViewModelBase
    {
        public string Greeting { get; private set; } = string.Empty;
        public string TodayText { get; private set; } = string.Empty;
        public string CountdownText { get; private set; } = string.Empty;
        public string MotivationText { get; private set; } = string.Empty;
        public string RankName { get; private set; } = "Новичок";
        public double RankProgress { get; private set; }
        public string RankProgressText { get; private set; } = string.Empty;

        public ObservableCollection<SubjectProgress> SubjectsProgress { get; } = new();
        public ObservableCollection<QuickCard> QuickCards { get; } = new();

        public ICommand NavigateCommand { get; }

        private static readonly DateTime EgeDate2026 = new DateTime(2026, 6, 1);

        private static readonly string[] Motivations = new[]
        {
            "Учись маленькими шагами — каждый день по чуть-чуть.",
            "Ошибка — это не провал, а точка роста.",
            "Регулярность побеждает интенсивность.",
            "Сегодня ты ближе к цели, чем вчера.",
            "Разбирай ошибки — именно там растут баллы.",
            "Лучше 30 минут каждый день, чем 5 часов раз в неделю.",
        };

        public HomePageViewModel()
        {
            NavigateCommand = new RelayCommand(p => Navigate(p as string));

            BuildGreeting();
            BuildCountdown();
            BuildSubjectProgress();
            BuildQuickCards();
            BuildRank();

            MotivationText = Motivations[new Random().Next(Motivations.Length)];
        }

        private void BuildGreeting()
        {
            int hour = DateTime.Now.Hour;
            string timeOfDay = hour switch
            {
                >= 5 and < 12 => "Доброе утро",
                >= 12 and < 18 => "День",
                >= 18 and < 23 => "Добрый вечер",
                _ => "Доброй ночи"
            };

            Greeting = $"{timeOfDay}!";

            var ru = new CultureInfo("ru-RU");
            TodayText = DateTime.Now.ToString("dddd, d MMMM yyyy", ru);
            TodayText = char.ToUpper(TodayText[0]) + TodayText.Substring(1);
        }

        private void BuildCountdown()
        {
            var days = (EgeDate2026 - DateTime.Today).Days;
            CountdownText = days > 0
                ? $"До ЕГЭ 2026 осталось {days} дней"
                : days == 0
                    ? "Сегодня ЕГЭ! Удачи!"
                    : $"ЕГЭ 2026 прошло {Math.Abs(days)} дней назад";
        }

        private void BuildSubjectProgress()
        {
            foreach (var s in App.ProgressService.Subjects)
            {
                SubjectsProgress.Add(new SubjectProgress
                {
                    SubjectId = s.Id,
                    Name = s.Name,
                    Percent = s.ProgressPercent,
                    IsEge = s.IsEge,
                    SecondaryScore = s.SecondaryScore
                });
            }
        }

        private void BuildQuickCards()
        {
            QuickCards.Add(new QuickCard("variants",   "📚", "Варианты",    "100 вариантов ЕГЭ"));
            QuickCards.Add(new QuickCard("builder",    "🧩", "Составитель", "Собери свой вариант"));
            QuickCards.Add(new QuickCard("notes",      "🗒", "Заметки",     "Записи и формулы"));
            QuickCards.Add(new QuickCard("tracker",    "📊", "Статистика",  "Прогресс по предметам"));
        }

        private void BuildRank()
        {
            int totalCorrect = App.ProgressService.Subjects.Sum(s => s.TasksCorrect);

            (string name, int min, int max)[] ranks = new[]
            {
                ("Новичок",  0,   20),
                ("Ученик",   20,  100),
                ("Профи",    100, 300),
                ("Эксперт",  300, 600),
                ("Мастер",   600, int.MaxValue),
            };

            var current = ranks.Last(r => totalCorrect >= r.min);
            RankName = current.name;

            if (current.max == int.MaxValue)
            {
                RankProgress = 100;
                RankProgressText = $"{totalCorrect} правильных ответов · Мастер";
            }
            else
            {
                int range = current.max - current.min;
                int progress = totalCorrect - current.min;
                RankProgress = range > 0 ? 100.0 * progress / range : 0;
                RankProgressText = $"{totalCorrect} / {current.max} до следующего ранга";
            }

            OnPropertyChanged(nameof(RankName));
            OnPropertyChanged(nameof(RankProgress));
            OnPropertyChanged(nameof(RankProgressText));
        }

        private void Navigate(string? tab)
        {
            if (string.IsNullOrEmpty(tab)) return;

            if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.Navigate(tab);
        }
    }

    public class SubjectProgress
    {
        public string SubjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Percent { get; set; }
        public bool IsEge { get; set; }
        public int SecondaryScore { get; set; }

        public string DisplayText => IsEge
            ? $"{Name} — {Percent:F0}% · {SecondaryScore} б."
            : $"{Name} — {Percent:F0}%";
    }

    public class QuickCard
    {
        public string Key { get; }
        public string Icon { get; }
        public string Title { get; }
        public string Description { get; }

        public QuickCard(string key, string icon, string title, string description)
        {
            Key = key;
            Icon = icon;
            Title = title;
            Description = description;
        }
    }
}