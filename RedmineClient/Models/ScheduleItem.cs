using System;
using System.Windows.Media;

namespace RedmineClient.Models
{
    /// <summary>
    /// スケジュール表の各日付のアイテム
    /// </summary>
    public class ScheduleItem
    {
        /// <summary>
        /// 日付
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 週番号
        /// </summary>
        public int WeekNumber => GetWeekNumber(Date);

        /// <summary>
        /// 曜日
        /// </summary>
        public string DayOfWeek => Date.ToString("ddd");

        /// <summary>
        /// タスクタイトル
        /// </summary>
        public string TaskTitle { get; set; } = string.Empty;

        /// <summary>
        /// 背景色
        /// </summary>
        public Brush BackgroundColor
        {
            get
            {
                // 土曜日は青色、日曜日・祝日はピンク
                if (Date.DayOfWeek == System.DayOfWeek.Saturday)
                    return Brushes.LightBlue;
                else if (Date.DayOfWeek == System.DayOfWeek.Sunday)
                    return Brushes.LightPink;
                else
                    return Brushes.White;
            }
        }

        /// <summary>
        /// 前景色
        /// </summary>
        public Brush ForegroundColor
        {
            get
            {
                // 土曜日・日曜日は黒、平日は黒
                if (Date.DayOfWeek == System.DayOfWeek.Saturday || Date.DayOfWeek == System.DayOfWeek.Sunday)
                    return Brushes.Black;
                else
                    return Brushes.Black;
            }
        }

        /// <summary>
        /// 非稼働日かどうか
        /// </summary>
        public bool IsNonWorkingDay => Date.DayOfWeek == System.DayOfWeek.Saturday || Date.DayOfWeek == System.DayOfWeek.Sunday;

        /// <summary>
        /// 週番号を取得する
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>週番号</returns>
        private static int GetWeekNumber(DateTime date)
        {
            var calendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            return calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
        }
    }
}
