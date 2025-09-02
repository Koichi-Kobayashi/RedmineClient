using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RedmineClient.Views.Controls
{
    public partial class TimelineHeader : UserControl
    {
        public static readonly DependencyProperty DayWidthProperty = DependencyProperty.Register(
            nameof(DayWidth), typeof(double), typeof(TimelineHeader), new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public double DayWidth { get => (double)GetValue(DayWidthProperty); set => SetValue(DayWidthProperty, value); }

        public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register(
            nameof(StartDate), typeof(DateTime), typeof(TimelineHeader), new FrameworkPropertyMetadata(DateTime.Today, FrameworkPropertyMetadataOptions.AffectsRender));
        public DateTime StartDate { get => (DateTime)GetValue(StartDateProperty); set => SetValue(StartDateProperty, value); }

        public TimelineHeader()
        {
            InitializeComponent();
            SizeChanged += (_, __) => InvalidateVisual();
            Loaded += (_, __) => InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth; if (w <= 0 || DayWidth <= 0) return;
            var h = ActualHeight; if (h <= 0) return;

            var penLight = new Pen(new SolidColorBrush(Color.FromRgb(220,220,220)), 1);
            var penBold  = new Pen(new SolidColorBrush(Color.FromRgb(180,180,180)), 1.5);
            var textBrush = Brushes.Black;

            int days = (int)Math.Ceiling(w / DayWidth) + 1;
            var ft = new Typeface("Segoe UI");

            DateTime d = StartDate.Date;
            for (int i = 0; i < days; i++)
            {
                double x = i * DayWidth;
                bool isWeek = d.DayOfWeek == DayOfWeek.Monday;
                bool isMonth = d.Day == 1;
                var pen = isMonth ? penBold : (isWeek ? penBold : penLight);

                dc.DrawLine(pen, new Point(x, 0), new Point(x, h));

                if (isMonth)
                {
                    var s = new FormattedText($"{d:yyyy MMM}", System.Globalization.CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight, ft, 12, textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    dc.DrawText(s, new Point(x + 4, 2));
                }
                // 曜日（日本語略称などカルチャに依存）
                var culture = System.Globalization.CultureInfo.CurrentUICulture;
                var dowStr = d.ToString("ddd", culture);
                Brush dowBrush = (d.DayOfWeek == DayOfWeek.Saturday) ? Brushes.Blue :
                                 (d.DayOfWeek == DayOfWeek.Sunday) ? Brushes.Red : textBrush;
                var dowText = new FormattedText(dowStr, culture,
                        FlowDirection.LeftToRight, ft, 11, dowBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(dowText, new Point(x + 4, 20));

                // 日付（数値）
                var domText = new FormattedText(d.ToString("dd"), culture,
                        FlowDirection.LeftToRight, ft, 12, textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(domText, new Point(x + 4, 36));

                d = d.AddDays(1);
            }

            dc.DrawLine(penBold, new Point(0, h-0.5), new Point(w, h-0.5));
        }
    }
}


