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

            // WBSページに近い落ち着いた配色
            var gridLight = Color.FromRgb(220, 220, 220);
            var gridBold  = Color.FromRgb(200, 200, 200);
            var txt       = Color.FromRgb(60, 60, 60);
            var penLight = new Pen(new SolidColorBrush(gridLight), 1);
            var penBold  = new Pen(new SolidColorBrush(gridBold), 1.25);
            var textBrush = new SolidColorBrush(txt);

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
                    var ymCulture = System.Globalization.CultureInfo.CurrentUICulture;
                    var ymText = new FormattedText(d.ToString("yyyy年MM月", ymCulture), ymCulture,
                        FlowDirection.LeftToRight, ft, 12, textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    // 月初（1日）の縦線位置を左端として表示
                    dc.DrawText(ymText, new Point(x + 2, 2));
                }
                // 曜日（日本語略称などカルチャに依存）
                var culture = System.Globalization.CultureInfo.CurrentUICulture;
                var dowStr = d.ToString("ddd", culture);
                var sat = Color.FromRgb(70, 120, 200);
                var sun = Color.FromRgb(200, 80, 80);
                Brush dowBrush = (d.DayOfWeek == DayOfWeek.Saturday) ? new SolidColorBrush(sat) :
                                 (d.DayOfWeek == DayOfWeek.Sunday) ? new SolidColorBrush(sun) : textBrush;
                // 中央寄せ描画
                double cellX = x;
                double centerX = cellX + DayWidth / 2.0;
                var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                // 日付（数値）を中段、曜日を下段に（1行下へシフト）
                var domText = new FormattedText(d.ToString("dd"), culture,
                        FlowDirection.LeftToRight, ft, 12, textBrush, dpi);
                // 年月との間隔を詰める
                dc.DrawText(domText, new Point(centerX - domText.Width / 2.0, 24));

                var dowText = new FormattedText(dowStr, culture,
                        FlowDirection.LeftToRight, ft, 11, dowBrush, dpi);
                // 下端に余白を確保
                dc.DrawText(dowText, new Point(centerX - dowText.Width / 2.0, 46));

                d = d.AddDays(1);
            }

            dc.DrawLine(penBold, new Point(0, h-0.5), new Point(w, h-0.5));
        }
    }
}


