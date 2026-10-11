using System.ComponentModel;
using System.Windows;
using EgeTracker.Models;
using EgeTracker.ViewModels;

namespace EgeTracker.Views
{
    public partial class ExamModeWindow : Window
    {
        private readonly ExamModeViewModel _vm;

        public ExamModeWindow(ExamModeSettings settings)
        {
            InitializeComponent();

            _vm = new ExamModeViewModel(settings);
            _vm.RequestClose += (_, __) => Close();
            DataContext = _vm;

            // Перехват Alt+F4 — только через кнопку завершения
            Closing += OnClosing;
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (!_vm.IsFinished)
            {
                e.Cancel = true;
                // Ничего не делаем — пользователь должен нажать «Завершить»
            }
        }

        private void OnFinishClick(object sender, RoutedEventArgs e)
        {
            _vm.RequestFinishFromUi();
        }
    }
}