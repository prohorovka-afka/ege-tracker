using System;
using System.Linq;
using System.Windows;
using EgeTracker.Models;

namespace EgeTracker.Views
{
    public partial class ExamSetupWindow : Window
    {
        private static readonly string[] AllowedSubjects = { "russian", "math_profile", "informatics" };

        public ExamModeSettings? Result { get; private set; }

        public ExamSetupWindow()
        {
            InitializeComponent();

            foreach (var s in App.ProgressService.Subjects.Where(s => AllowedSubjects.Contains(s.Id)))
                SubjectCombo.Items.Add(s);

            if (SubjectCombo.Items.Count > 0)
                SubjectCombo.SelectedIndex = 0;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (SubjectCombo.SelectedItem is not Subject subject)
            {
                MessageBox.Show("Выберите предмет.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(VariantText.Text, out int variant) || variant < 1)
            {
                MessageBox.Show("Введите корректный номер варианта.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationText.Text, out int duration) || duration < 1)
                duration = 235;

            var tasks = App.VariantGeneratorService.BuildVariantTasks(
                subject.Id,
                new VariantTemplate
                {
                    VariantNumber = variant,
                    SubjectId = subject.Id,
                    KimPositions = App.SubjectConfigService.FindById(subject.Id)?.TaskNumbers ?? new(),
                });

            Result = new ExamModeSettings
            {
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                VariantNumber = variant,
                DurationMinutes = duration,
                IsInformatics = subject.Id == "informatics",
                Tasks = tasks,
            };

            DialogResult = true;
            Close();
        }
    }
}