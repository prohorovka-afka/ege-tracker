using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EgeTracker.Behaviors
{
    /// <summary>
    /// Каскадная анимация появления элементов списка (снизу вверх + fade-in).
    /// Применяется к ItemsControl. Отключается при большом количестве элементов.
    /// </summary>
    public static class CascadeListBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(CascadeListBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControl ic && (bool)e.NewValue)
            {
                ic.ItemContainerGenerator.StatusChanged += (_, __) => AnimateItems(ic);
                ic.Loaded += (_, __) => AnimateItems(ic);
            }
        }

        private static void AnimateItems(ItemsControl ic)
        {
            try
            {
                int count = ic.Items.Count;
                // Не анимируем слишком большие списки — иначе «долго».
                if (count == 0 || count > 60) return;

                for (int i = 0; i < count; i++)
                {
                    if (ic.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe)
                    {
                        var tt = fe.RenderTransform as TranslateTransform;
                        if (tt == null)
                        {
                            tt = new TranslateTransform();
                            fe.RenderTransform = tt;
                        }
                        fe.Opacity = 0;
                        tt.Y = 10;

                        var delay = TimeSpan.FromSeconds(i * 0.03);
                        var dur = TimeSpan.FromSeconds(0.25);

                        var fade = new DoubleAnimation
                        {
                            From = 0, To = 1,
                            Duration = dur,
                            BeginTime = delay
                        };
                        fe.BeginAnimation(UIElement.OpacityProperty, fade);

                        var move = new DoubleAnimation
                        {
                            From = 10, To = 0,
                            Duration = dur,
                            BeginTime = delay,
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        tt.BeginAnimation(TranslateTransform.YProperty, move);
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }
    }
}