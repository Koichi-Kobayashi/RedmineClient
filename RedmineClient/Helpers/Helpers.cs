namespace RedmineClient.Helpers
{
    /// <summary>
    /// ヘルパークラスのインスタンスを管理するクラス
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// 日付を背景色に変換するコンバーターのインスタンス
        /// </summary>
        public static DateToBackgroundColorConverter DateToBackgroundColorConverter { get; } = new DateToBackgroundColorConverter();
    }
}
