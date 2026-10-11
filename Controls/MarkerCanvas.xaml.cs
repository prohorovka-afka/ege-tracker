using System.Windows;
using System.Windows.Controls;
using EgeTracker.Services;

namespace EgeTracker.Controls
{
    public partial class MarkerCanvas : UserControl
    {
        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(MarkerCanvas),
                new PropertyMetadata(null, OnInnerContentChanged));

        public object? InnerContent
        {
            get => GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public MarkerCanvas()
        {
            InitializeComponent();
        }

        private static void OnInnerContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkerCanvas mc) mc.ContentHost.Content = e.NewValue;
        }

        public void EnableInk() => InkToolService.SetInk(Canvas);
        public void EnableEraser() => InkToolService.SetEraser(Canvas);
        public void Disable() => InkToolService.Disable(Canvas);
        public void Clear() => InkToolService.Clear(Canvas);

        private void OnInk(object sender, RoutedEventArgs e) => EnableInk();
        private void OnEraser(object sender, RoutedEventArgs e) => EnableEraser();
        private void OnClear(object sender, RoutedEventArgs e) => Clear();
        private void OnDisable(object sender, RoutedEventArgs e) => Disable();
    }
}