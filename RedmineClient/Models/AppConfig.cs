using System.Configuration;
using Wpf.Ui.Appearance;

namespace RedmineClient.Models
{
    public class AppConfig
    {
        public static string RedmineHost { get; set; }

        public static string ApiKey { get; set; }

        /// <summary>
        /// 設定情報の保存
        /// </summary>
        public static void Save()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["RedmineHost"].Value = RedmineHost;
            config.AppSettings.Settings["ApiKey"].Value = ApiKey;
            config.AppSettings.Settings["ApplicationTheme"].Value = ApplicationThemeManager.GetAppTheme().ToString();
            config.Save();
        }

        /// <summary>
        /// 設定情報の読み込み
        /// </summary>
        public static void Load()
        {
            var redmineHost = ConfigurationManager.AppSettings["RedmineHost"];
            RedmineHost = string.IsNullOrEmpty(redmineHost) != true ? redmineHost : "";
            var apiKey = ConfigurationManager.AppSettings["ApiKey"];
            ApiKey = string.IsNullOrEmpty(apiKey) != true ? apiKey : "";
            var applicationTheme = ConfigurationManager.AppSettings["ApplicationTheme"];
            applicationTheme = string.IsNullOrEmpty(applicationTheme) != true ? applicationTheme : "";
            var currentTheme = (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), applicationTheme);
            ApplicationThemeManager.Apply(currentTheme);
        }
    }
}
