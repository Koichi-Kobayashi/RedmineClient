using System;
using System.Windows;
using System.Windows.Media;
using RedmineClient.Services;

namespace RedmineClient.Views.Controls
{
    public class TimelineGridOverlay : FrameworkElement
    {
        public static readonly DependencyProperty DayWidthProperty = DependencyProperty.Register(
            nameof(DayWidth), typeof(double), typeof(TimelineGridOverlay), new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public double DayWidth { get => (double)GetValue(DayWidthProperty); set => SetValue(DayWidthProperty, value); }

        public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register(
            nameof(StartDate), typeof(DateTime), typeof(TimelineGridOverlay), new FrameworkPropertyMetadata(DateTime.Today, FrameworkPropertyMetadataOptions.AffectsRender));
        public DateTime StartDate { get => (DateTime)GetValue(StartDateProperty); set => SetValue(StartDateProperty, value); }

        public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
            nameof(LineBrush), typeof(Brush), typeof(TimelineGridOverlay), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), FrameworkPropertyMetadataOptions.AffectsRender));
        public Brush LineBrush { get => (Brush)GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            double w = ActualWidth, h = ActualHeight; if (w <= 0 || h <= 0 || DayWidth <= 0) return;

            var pen = new Pen(LineBrush, 1);
            // 背景色（薄く）
            var satBrush = new SolidColorBrush(Color.FromArgb(20, 70, 120, 200));
            var sunBrush = new SolidColorBrush(Color.FromArgb(20, 200, 80, 80));
            var holBrush = new SolidColorBrush(Color.FromArgb(28, 200, 50, 50));

            int days = (int)Math.Ceiling(w / DayWidth) + 1;
            DateTime d = StartDate.Date;

            for (int i = 0; i < days; i++)
            {
                double x = i * DayWidth;
                // 祝日/土日背景
                if (HolidayService.IsHoliday(d))
                {
                    dc.DrawRectangle(holBrush, null, new Rect(x, 0, DayWidth, h));
                }
                else if (d.DayOfWeek == DayOfWeek.Saturday)
                {
                    dc.DrawRectangle(satBrush, null, new Rect(x, 0, DayWidth, h));
                }
                else if (d.DayOfWeek == DayOfWeek.Sunday)
                {
                    dc.DrawRectangle(sunBrush, null, new Rect(x, 0, DayWidth, h));
                }

                dc.DrawLine(pen, new Point(x, 0), new Point(x, h));
                d = d.AddDays(1);
            }
        }
    }
}


