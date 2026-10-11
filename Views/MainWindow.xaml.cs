using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using EgeTracker.ViewModels;
using EgeTracker.Services;

namespace EgeTracker.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new MainViewModel();
        private Storyboard? _currentTransition;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            _vm.AttachHost(ContentHost);
            _vm.Navigation.Navigated += OnNavigated;

            PreviewKeyDown += OnPreviewKeyDown;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyBackground();
                _vm.RestoreLastTab();
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            try
            {
                _vm.Navigation.Navigated -= OnNavigated;
                if (App.AppStateService != null)
                    App.AppStateService.SaveAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        public void RefreshBackground() => ApplyBackground();

        private void ApplyBackground()
        {
            try
            {
                var path = App.SettingsService.Settings.BackgroundImagePath;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    BackgroundImage.Source = null;
                    BackgroundImage.Visibility = Visibility.Collapsed;
                    return;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                BackgroundImage.Source = bmp;
                BackgroundImage.Opacity = App.SettingsService.Settings.BackgroundOpacity;
                BackgroundImage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                App.LogError(ex);
                BackgroundImage.Source = null;
                BackgroundImage.Visibility = Visibility.Collapsed;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F11)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    // Esc — выход из концентрации (если активна)
                    if (_vm.IsConcentrationMode)
                    {
                        _vm.IsConcentrationMode = false;
                        e.Handled = true;
                    }
                    // Esc — выход из полного экрана
                    else if (WindowStyle == WindowStyle.None)
                    {
                        ExitFullscreen();
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex) { App.LogError(ex); }
        }

        private void ToggleFullscreen()
        {
            if (WindowStyle == WindowStyle.None) ExitFullscreen();
            else EnterFullscreen();
        }

        private void EnterFullscreen()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }

        private void ExitFullscreen()
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
        }

        private void OnNavigated(object? sender, NavigationEventArgs e) => PlayTransition();

        private void PlayTransition()
        {
            try
            {
                _currentTransition?.Stop();
                ContentTranslate.X = 50;
                ContentHost.Opacity = 0;

                var sb = new Storyboard();
                var xAnim = new DoubleAnimation
                {
                    From = 50, To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                };
                Storyboard.SetTarget(xAnim, ContentTranslate);
                Storyboard.SetTargetProperty(xAnim, new PropertyPath(TranslateTransform.XProperty));
                sb.Children.Add(xAnim);

                var opAnim = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.3) };
                Storyboard.SetTarget(opAnim, ContentHost);
                Storyboard.SetTargetProperty(opAnim, new PropertyPath(OpacityProperty));
                sb.Children.Add(opAnim);

                _currentTransition = sb;
                sb.Begin();
            }
            catch (Exception ex) { App.LogError(ex); }
        }
    }
}