using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EgeTracker.Models;
using EgeTracker.Services;

namespace EgeTracker.ViewModels
{
    /// <summary>
    /// ViewModel списка вариантов ЕГЭ 2027.
    /// Показывает 100 кнопок «Вариант N» для выбранного предмета.
    /// </summary>
    public class VariantListViewModel : ViewModelBase
    {
        private readonly VariantGeneratorService _generator = new VariantGeneratorService();

        public ObservableCollection<SubjectConfig> Subjects { get; } = new ObservableCollection<SubjectConfig>();
        public ObservableCollection<VariantButton> Variants { get; } = new ObservableCollection<VariantButton>();

        private SubjectConfig? _selectedSubject;
        public SubjectConfig? SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                if (SetField(ref _selectedSubject, value))
                    LoadVariants();
            }
        }

        public ICommand OpenVariantCommand { get; }

        public VariantListViewModel()
        {
            foreach (var s in App.SubjectConfigService.Subjects)
                Subjects.Add(s);

            SelectedSubject = Subjects.FirstOrDefault();

            OpenVariantCommand = new RelayCommand(p => OpenVariant(p as VariantButton));
        }

        private void LoadVariants()
        {
            Variants.Clear();
            if (SelectedSubject == null) return;

            var templates = _generator.GenerateVariants(SelectedSubject.Id);

            foreach (var template in templates)
            {
                int available = 0;
                foreach (var pos in template.KimPositions)
                {
                    if (App.TaskService.GetPrototypeTasks(SelectedSubject.Id, pos).Any())
                        available++;
                }

                Variants.Add(new VariantButton
                {
                    Template = template,
                    DisplayText = $"Вариант {template.VariantNumber}",
                    AvailableCount = available,
                    TotalCount = template.KimPositions.Count
                });
            }

            System.Diagnostics.Debug.WriteLine(
                $"[VariantList] Loaded {Variants.Count} variants for {SelectedSubject.Id}");
        }

        /// <summary>
        /// Открыть выбранный вариант — создаёт VariantViewModel и переходит на вкладку «Варианты».
        /// </summary>
        private void OpenVariant(VariantButton? button)
        {
            try
            {
                if (button == null || SelectedSubject == null) return;

                var tasks = _generator.BuildVariantTasks(SelectedSubject.Id, button.Template);

                var variantVm = new VariantViewModel();
                variantVm.LoadFromTasks(SelectedSubject.Id, tasks);
                variantVm.IsCustom = false; // ЕГЭ-вариант — с баллами

                if (Application.Current?.MainWindow?.DataContext is MainViewModel mainVm)
                {
                    mainVm.NavigateToVariantsWith(variantVm);
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                MessageBox.Show("Не удалось открыть вариант.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Кнопка одного варианта в списке.
    /// </summary>
    public class VariantButton : ViewModelBase
    {
        public VariantTemplate Template { get; set; } = null!;
        public string DisplayText { get; set; } = string.Empty;
        public int AvailableCount { get; set; }
        public int TotalCount { get; set; }

        public string ProgressText => $"{AvailableCount} / {TotalCount}";
    }
}