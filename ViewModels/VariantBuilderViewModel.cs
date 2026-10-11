using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;
using EgeTracker.Views;

namespace EgeTracker.ViewModels
{
    public class VariantBuilderViewModel : ViewModelBase
    {
        private readonly SubjectConfigService _configService;

        public ObservableCollection<SubjectConfig> Subjects { get; } = new ObservableCollection<SubjectConfig>();
        public ObservableCollection<TaskNumberItem> TaskNumbers { get; } = new ObservableCollection<TaskNumberItem>();

        private SubjectConfig? _selectedSubject;
        public SubjectConfig? SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetField(ref _selectedSubject, value))
                    LoadTaskNumbers();
            }
        }

        /// <summary>
        /// Общее число доступных заданий (сумма по всем номерам).
        /// </summary>
        private int _totalAvailable;
        public int TotalAvailable
        {
            get => _totalAvailable;
            set => SetField(ref _totalAvailable, value);
        }

        /// <summary>
        /// Сколько номеров отмечено.
        /// </summary>
        private int _selectedCount;
        public int SelectedCount
        {
            get => _selectedCount;
            set => SetField(ref _selectedCount, value);
        }

        /// <summary>
        /// Сколько заданий будет в собранном варианте (по 1 на номер).
        /// </summary>
        public int VariantSize => SelectedCount;

        public ICommand SelectAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand BuildCommand { get; }

        public VariantBuilderViewModel()
        {
            _configService = App.SubjectConfigService;

            foreach (var s in _configService.Subjects)
                Subjects.Add(s);

            SelectedSubject = Subjects.FirstOrDefault();

            SelectAllCommand = new RelayCommand(_ => SetAll(true));
            ClearAllCommand  = new RelayCommand(_ => SetAll(false));
            BuildCommand     = new RelayCommand(_ => Build(), _ => SelectedCount > 0);
        }

        private void LoadTaskNumbers()
        {
            // Отписываемся от старых событий
            foreach (var t in TaskNumbers)
                t.PropertyChanged -= OnTaskNumberChanged;

            TaskNumbers.Clear();
            if (SelectedSubject == null) return;

            foreach (var number in SelectedSubject.TaskNumbers)
            {
                // ⭐ Считаем, сколько заданий этого номера реально есть в JSON
                int available = App.TaskService
                    .GetPrototypeTasks(SelectedSubject.Id, number)
                    .Count();

                var item = new TaskNumberItem
                {
                    Number = number,
                    IsSelected = false,
                    AvailableCount = available
                };
                item.PropertyChanged += OnTaskNumberChanged;
                TaskNumbers.Add(item);
            }

            UpdateCounters();
        }

        private void OnTaskNumberChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskNumberItem.IsSelected))
                UpdateCounters();
        }

        private void UpdateCounters()
        {
            TotalAvailable = TaskNumbers.Sum(t => t.AvailableCount);
            SelectedCount = TaskNumbers.Count(t => t.IsSelected);

            OnPropertyChanged(nameof(VariantSize));

            // Обновляем состояние команды «Составить вариант»
            (BuildCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void SetAll(bool selected)
        {
            foreach (var t in TaskNumbers)
                t.IsSelected = selected;

            UpdateCounters();
        }

        private void Build()
        {
            try
            {
                if (SelectedSubject == null)
                {
                    MessageBox.Show("Выберите предмет.", "Составитель",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedNumbers = TaskNumbers.Where(t => t.IsSelected).Select(t => t.Number).ToList();
                if (selectedNumbers.Count == 0)
                {
                    MessageBox.Show("Отметьте хотя бы один номер задания.", "Составитель",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var rng = new Random();
                var tasks = new List<TaskItem>();

                foreach (var number in selectedNumbers)
                {
                    var pool = App.TaskService
                        .GetPrototypeTasks(SelectedSubject.Id, number)
                        .ToList();

                    if (pool.Count == 0)
                    {
                        // Заглушка
                        tasks.Add(new TaskItem
                        {
                            Id = $"stub_{SelectedSubject.Id}_{number}",
                            Subject = SelectedSubject.Id,
                            TaskNumber = number,
                            KimPosition = number,
                            TaskType = "short_answer",
                            Question = "Задание пока недоступно: для этого номера нет JSON в папке tasks/.",
                            ImagePath = null,
                            Options = null,
                            CorrectAnswer = "",
                            MaxPrimaryScore = 1,
                            VideoUrl = null,
                            Prototypes = new List<string> { number.ToString() },
                            FipiId = $"stub_{number}"
                        });
                    }
                    else
                    {
                        tasks.Add(pool[rng.Next(pool.Count)]);
                    }
                }

                var variantVm = new VariantViewModel();
                variantVm.LoadFromTasks(SelectedSubject.Id, tasks);

                OpenVariantInMainWindow(variantVm);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                MessageBox.Show("Не удалось собрать вариант. Подробности в error.log",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenVariantInMainWindow(VariantViewModel variantVm)
        {
            if (Application.Current?.MainWindow?.DataContext is MainViewModel mainVm)
                mainVm.NavigateToVariantsWith(variantVm);
        }
    }

    /// <summary>
    /// Один номер задания с чекбоксом и счётчиком доступных заданий.
    /// </summary>
    public class TaskNumberItem : ViewModelBase
    {
        public int Number { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        // ⭐ Счётчик: сколько заданий этого номера реально есть
        private int _availableCount;
        public int AvailableCount
        {
            get => _availableCount;
            set
            {
                if (SetField(ref _availableCount, value))
                {
                    OnPropertyChanged(nameof(DisplayText));
                    OnPropertyChanged(nameof(CounterText));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(HasData));
                }
            }
        }

        public string DisplayText => $"Задание № {Number}";

        // ⭐ «35 заданий» / «1 задание» / «нет заданий» — с правильным склонением
        public string CounterText => AvailableCount switch
        {
            0 => "нет заданий",
            1 => "1 задание",
            2 or 3 or 4 => $"{AvailableCount} задания",
            _ => $"{AvailableCount} заданий"
        };

        public string StatusText => HasData ? $"✔ {CounterText}" : "— нет данных";

        public bool HasData => AvailableCount > 0;
    }
}