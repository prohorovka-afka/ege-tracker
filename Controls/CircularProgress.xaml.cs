using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace EgeTracker.Controls
{
    public partial class CircularProgress : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularProgress),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(CircularProgress),
                new PropertyMetadata(string.Empty, OnCaptionChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        private double _displayed;

        public CircularProgress()
        {
            InitializeComponent();
            SizeChanged += (_, __) => Redraw(Value);
            Loaded += (_, __) => Redraw(Value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp)
            {
                double from = cp._displayed;
                double to = (double)e.NewValue;
                var anim = new DoubleAnimation
                {
                    From = from,
                    To = to,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                anim.Completed += (_, __) => cp._displayed = to;
                cp._displayed = to;
                cp.BeginAnimation(DisplayedProperty, anim);
            }
        }

        private static readonly DependencyProperty DisplayedProperty =
            DependencyProperty.Register("Displayed", typeof(double), typeof(CircularProgress),
                new PropertyMetadata(0.0, OnDisplayedChanged));

        private double Displayed
        {
            get => (double)GetValue(DisplayedProperty);
            set => SetValue(DisplayedProperty, value);
        }

        private static void OnDisplayedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp) cp.Redraw((double)e.NewValue);
        }

        private static void OnCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp && cp.CaptionText != null)
                cp.CaptionText.Text = (string)e.NewValue;
        }

        private void Redraw(double percent)
        {
            try
            {
                if (ActualWidth <= 0 || ActualHeight <= 0) return;
                double size = Math.Min(ActualWidth, ActualHeight);
                double thickness = 12;
                double radius = (size - thickness) / 2.0;
                var center = new Point(size / 2.0, size / 2.0);

                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;

                double angle = percent * 3.6;
                double rad = (angle - 90) * Math.PI / 180.0;

                var start = new Point(center.X, center.Y - radius);
                var end = new Point(center.X + radius * Math.Cos(rad),
                                    center.Y + radius * Math.Sin(rad));

                var fig = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
                fig.Segments.Add(new ArcSegment
                {
                    Point = end,
                    Size = new Size(radius, radius),
                    IsLargeArc = angle > 180,
                    SweepDirection = SweepDirection.Clockwise
                });
                var geo = new PathGeometry();
                geo.Figures.Add(fig);
                ArcPath.Data = geo;

                if (PercentText != null) PercentText.Text = $"{percent:F0}%";
            }
            catch (Exception ex)
            {
                App.LogError(ex);
            }
        }
    }
}