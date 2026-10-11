using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EgeTracker.Services;
using Microsoft.Win32;

namespace EgeTracker.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly UpdateCheckerService _updateChecker = new();
        private readonly EnglishBankService _englishBank = new();
        private readonly LinuxBankService _linuxBank = new();

        public string Theme => App.SettingsService.Settings.Theme;
        public string? BackgroundPath => App.SettingsService.Settings.BackgroundImagePath;

        private bool _isCheckingUpdates;
        public bool IsCheckingUpdates
        {
            get => _isCheckingUpdates;
            set => SetField(ref _isCheckingUpdates, value);
        }

        private string _updateStatus = string.Empty;
        public string UpdateStatus
        {
            get => _updateStatus;
            set => SetField(ref _updateStatus, value);
        }

        private bool _isLoadingEnglish;
        public bool IsLoadingEnglish
        {
            get => _isLoadingEnglish;
            set => SetField(ref _isLoadingEnglish, value);
        }

        private string _englishStatus = string.Empty;
        public string EnglishStatus
        {
            get => _englishStatus;
            set => SetField(ref _englishStatus, value);
        }

        private bool _isLoadingLinux;
        public bool IsLoadingLinux
        {
            get => _isLoadingLinux;
            set => SetField(ref _isLoadingLinux, value);
        }

        private string _linuxStatus = string.Empty;
        public string LinuxStatus
        {
            get => _linuxStatus;
            set => SetField(ref _linuxStatus, value);
        }

        public ICommand ToggleThemeCommand { get; }
        public ICommand ChooseBackgroundCommand { get; }
        public ICommand ClearBackgroundCommand { get; }
        public ICommand CheckUpdatesCommand { get; }
        public ICommand LoadEnglishCommand { get; }
        public ICommand LoadLinuxCommand { get; }

        public SettingsViewModel()
        {
            App.SettingsService.ThemeChanged += (_, __) => OnPropertyChanged(nameof(Theme));
            App.SettingsService.BackgroundChanged += (_, __) => OnPropertyChanged(nameof(BackgroundPath));

            ToggleThemeCommand = new RelayCommand(async _ =>
            {
                if (Application.Current is App app)
                    await App.SettingsService.ToggleThemeAsync(app).ConfigureAwait(true);
            });

            ChooseBackgroundCommand = new RelayCommand(_ => ChooseBackground());

            ClearBackgroundCommand = new RelayCommand(async _ =>
            {
                await App.SettingsService.SetBackgroundAsync(null).ConfigureAwait(true);
                RefreshMainWindowBackground();
            });

            CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
            LoadEnglishCommand  = new RelayCommand(async _ => await LoadEnglishAsync());
            LoadLinuxCommand    = new RelayCommand(async _ => await LoadLinuxAsync());
        }

        private void ChooseBackground()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Выберите фоновое изображение",
            };

            if (dlg.ShowDialog() == true)
            {
                App.SettingsService.SetBackgroundAsync(dlg.FileName, opacity: 0.35)
                    .GetAwaiter().GetResult();
                RefreshMainWindowBackground();
            }
        }

        private static void RefreshMainWindowBackground()
        {
            if (Application.Current?.MainWindow is EgeTracker.Views.MainWindow mw)
                mw.RefreshBackground();
        }

        private async Task CheckUpdatesAsync()
        {
            if (IsCheckingUpdates) return;
            IsCheckingUpdates = true;
            UpdateStatus = "Проверка ФИПИ…";

            try
            {
                var progress = new Progress<string>(msg => UpdateStatus = msg);
                var results = await _updateChecker
                    .CheckAndUpdateAllAsync(progress, CancellationToken.None)
                    .ConfigureAwait(true);

                int totalNew = results.Sum(r => r.NewTasks);
                var errors = results.Where(r => !string.IsNullOrEmpty(r.ErrorMessage)).ToList();

                string message;
                if (errors.Count == results.Count)
                    message = "Не удалось подключиться к ФИПИ. Проверьте интернет.";
                else if (totalNew > 0)
                    message = $"Загружено {totalNew} новых заданий. Перезапустите приложение.";
                else
                    message = "Обновлений нет.";

                UpdateStatus = message;
                MessageBox.Show(message, "ФИПИ",
                    MessageBoxButton.OK, totalNew > 0 ? MessageBoxImage.Information : MessageBoxImage.Asterisk);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                UpdateStatus = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsCheckingUpdates = false; }
        }

        private async Task LoadEnglishAsync()
        {
            if (IsLoadingEnglish) return;
            IsLoadingEnglish = true;
            EnglishStatus = "Скачиваю English…";

            try
            {
                var progress = new Progress<string>(msg => EnglishStatus = msg);
                int count = await _englishBank
                    .FetchAllAsync(progress, CancellationToken.None)
                    .ConfigureAwait(true);

                string message = count > 0
                    ? $"Загружено {count} English-заданий."
                    : "Не удалось скачать English.";

                EnglishStatus = message;
                MessageBox.Show(message, "English",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                EnglishStatus = $"Ошибка: {ex.Message}";
            }
            finally { IsLoadingEnglish = false; }
        }

        private async Task LoadLinuxAsync()
        {
            if (IsLoadingLinux) return;
            IsLoadingLinux = true;
            LinuxStatus = "Скачиваю Linux…";

            try
            {
                var progress = new Progress<string>(msg => LinuxStatus = msg);
                int count = await _linuxBank
                    .FetchAllAsync(progress, CancellationToken.None)
                    .ConfigureAwait(true);

                string message = count > 0
                    ? $"Загружено {count} Linux-заданий."
                    : "Не удалось скачать Linux.";

                LinuxStatus = message;
                MessageBox.Show(message, "Linux",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                LinuxStatus = $"Ошибка: {ex.Message}";
            }
            finally { IsLoadingLinux = false; }
        }
    }
}