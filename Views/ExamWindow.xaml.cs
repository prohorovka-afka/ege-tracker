using System;
using System.ComponentModel;
using System.Windows;
using EgeTracker.Services;
using EgeTracker.ViewModels;

namespace EgeTracker.Views
{
    public partial class ExamWindow : Window
    {
        private readonly ExamWindowViewModel _vm = new();
        private bool _hardBlockActive;

        public ExamWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            _vm.RequestClose += (_, __) => Close();
            _vm.PropertyChanged += OnVmPropertyChanged;

            Closing += OnClosing;
            Closed += OnClosed;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Активируем жёсткую блокировку, когда начинается ЭКЗАМЕН
            // и снимаем при переходе на РЕЗУЛЬТАТЫ
            if (e.PropertyName == nameof(ExamWindowViewModel.CurrentScreen))
            {
                if (_vm.IsExamScreen && !_vm.IsInformatics)
                {
                    ActivateHardBlock();
                }
                else if (_hardBlockActive)
                {
                    DeactivateHardBlock();
                }
            }
        }

        private void ActivateHardBlock()
        {
            _hardBlockActive = true;
            Topmost = true;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;

            // ⚠️ Хук клавиш — включи, если готов к возможному бану антивируса
            // KeyboardHookService.Install();
        }

        private void DeactivateHardBlock()
        {
            _hardBlockActive = false;
            Topmost = false;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;

            if (KeyboardHookService.IsActive)
                KeyboardHookService.Uninstall();
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            // Блокируем закрытие при активном экзамене (математика/русский)
            if (_hardBlockActive && _vm.IsExamScreen)
            {
                e.Cancel = true;
            }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (KeyboardHookService.IsActive)
                KeyboardHookService.Uninstall();

            // Возвращаем главное окно
            if (Application.Current?.MainWindow is Window main)
            {
                main.Show();
                main.WindowState = WindowState.Normal;
                main.Activate();
            }
        }
    }
}