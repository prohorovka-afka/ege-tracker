using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace EgeTracker.Views
{
    public partial class SplashWindow : Window
    {
        private bool _finished;

        public SplashWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Анимация прогресс-бара от 0 до 100 за ~2.4 сек.
            var progressAnim = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(2.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Progress.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, progressAnim);

            // Пульсация логотипа.
            var pulse = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(1.0),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };
            LogoScaler.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pulse);
            LogoScaler.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pulse);

            // Статусные сообщения (без таймеров — через Storyboard.Completed)
            var statusAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                BeginTime = TimeSpan.FromSeconds(0.5)
            };
            StatusText.BeginAnimation(OpacityProperty, statusAnim);
            StatusText.Text = "Загрузка заданий…";
        }

        /// <summary>
        /// Завершение: плавное затухание и закрытие через Dispatcher.
        /// </summary>
        public Task FinishAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            if (_finished)
            {
                tcs.SetResult(true);
                return tcs.Task;
            }
            _finished = true;

            var fade = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4)
            };
            fade.Completed += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try { Close(); } catch (Exception ex) { App.LogError(ex); }
                    tcs.SetResult(true);
                }));
            };
            BeginAnimation(OpacityProperty, fade);
            return tcs.Task;
        }
    }
}