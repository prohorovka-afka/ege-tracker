using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EgeTracker.Services;
using EgeTracker.Views;
using System.Linq;

namespace EgeTracker.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly NavigationService _nav = new NavigationService();
        private object? _currentView;
        private string _activeTab = "home";

        public ObservableCollection<NavItem> Tabs { get; } = new();

        public object? CurrentView
        {
            get => _currentView;
            set => SetField(ref _currentView, value);
        }

        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                if (SetField(ref _activeTab, value))
                {
                    UpdateActiveStates();

                    if (App.AppStateService != null)
                    {
                        App.AppStateService.State.LastTab = value;
                        App.AppStateService.SaveDebounced();
                    }
                }
            }
        }

        private bool _isConcentrationMode;
        public bool IsConcentrationMode
        {
            get => _isConcentrationMode;
            set
            {
                if (SetField(ref _isConcentrationMode, value))
                    OnPropertyChanged(nameof(SidebarWidth));
            }
        }

        public GridLength SidebarWidth => IsConcentrationMode
            ? new GridLength(0)
            : new GridLength(240);

        public ICommand NavigateCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ExamModeCommand { get; }
        public ICommand ToggleConcentrationCommand { get; }

        public NavigationService Navigation => _nav;

        public MainViewModel()
        {
            Tabs.Add(new NavItem("home",       "🏠", "Главная"));
            Tabs.Add(new NavItem("tracker",    "📊", "Трекер прогресса"));
            Tabs.Add(new NavItem("prototypes", "📝", "Прототипы"));
            Tabs.Add(new NavItem("variants",   "📚", "Варианты"));
            Tabs.Add(new NavItem("builder",    "🧩", "Составитель"));
            Tabs.Add(new NavItem("reference",  "📖", "Справочник"));
            Tabs.Add(new NavItem("notes",      "🗒", "Заметки"));
            Tabs.Add(new NavItem("settings",   "⚙", "Настройки"));

            NavigateCommand = new RelayCommand(p => Navigate(p as string));

            ToggleThemeCommand = new RelayCommand(async _ =>
            {
                try
                {
                    if (Application.Current is App app)
                        await App.SettingsService.ToggleThemeAsync(app).ConfigureAwait(true);
                }
                catch (Exception ex) { App.LogError(ex); }
            });

            ExamModeCommand = new RelayCommand(_ => StartExamMode());
            ToggleConcentrationCommand = new RelayCommand(_ => IsConcentrationMode = !IsConcentrationMode);

            UpdateActiveStates();
        }

        public void RestoreLastTab()
        {
            var lastTab = App.AppStateService?.State.LastTab;
            if (!string.IsNullOrEmpty(lastTab) && Tabs.Any(t => t.Key == lastTab))
                Navigate(lastTab);
            else
                Navigate("home");
        }

        private void StartExamMode()
        {
            try
            {
                var exam = new ExamWindow();
                if (Application.Current?.MainWindow is Window main) main.Hide();
                exam.ShowDialog();
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                MessageBox.Show("Не удалось запустить режим ЕГЭ.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (Application.Current?.MainWindow is Window main) main.Show();
            }
        }

        private void UpdateActiveStates()
        {
            foreach (var tab in Tabs)
                tab.IsActive = tab.Key == ActiveTab;
        }

        public void AttachHost(ContentControl host) => _nav.AttachHost(host);

        public void Navigate(string? tab)
        {
            if (string.IsNullOrEmpty(tab)) return;
            ActiveTab = tab;

            object view = tab switch
            {
                "home"       => new HomePageView    { DataContext = new HomePageViewModel() },
                "tracker"    => new TrackerView     { DataContext = new TrackerViewModel() },
                "prototypes" => new PrototypesView  { DataContext = new PrototypesViewModel() },
                "variants"   => new VariantListView { DataContext = new VariantListViewModel() },
                "builder"    => new VariantBuilderView { DataContext = new VariantBuilderViewModel() },
                "reference"  => new ReferenceView   { DataContext = new ReferenceViewModel() },
                "notes"      => new NotesView       { DataContext = new NotesViewModel() },
                "settings"   => new SettingsView    { DataContext = new SettingsViewModel() },
                _            => new HomePageView    { DataContext = new HomePageViewModel() }
            };

            _nav.NavigateTo(view);
            CurrentView = view;
        }

        public void NavigateToVariantsWith(VariantViewModel variantVm)
        {
            ActiveTab = "variants";
            var view = new VariantView { DataContext = variantVm };
            _nav.NavigateTo(view);
            CurrentView = view;
        }
    }

    /// <summary>
    /// Элемент навигации с иконкой и текстом.
    /// </summary>
    public class NavItem : INotifyPropertyChanged
    {
        private bool _isActive;

        public string Key { get; }
        public string Icon { get; }
        public string Title { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            }
        }

        public NavItem(string key, string icon, string title)
        {
            Key = key;
            Icon = icon;
            Title = title;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}