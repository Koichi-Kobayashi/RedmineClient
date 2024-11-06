namespace RedmineClient.Helpers
{
    public static class DateTimeExtensions
    {
        public static string ToYYYYMMDD(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd");
        }
    }
}
