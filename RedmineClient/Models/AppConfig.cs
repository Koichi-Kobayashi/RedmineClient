using System.Configuration;
using Wpf.Ui.Appearance;

namespace RedmineClient.Models
{
    public class AppConfig
    {
        public static string RedmineHost { get; set; }
        public static string Login { get; set; }
        public static string Password { get; set; }
        public static string ApiKey { get; set; }

        /// <summary>
        /// 設定情報の保存
        /// </summary>
        public static void Save()
        {
            // app.configの読み込み
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // 暗号化するセクションの取得
            var section = config.GetSection("appSettings") as AppSettingsSection;
            if (section != null && section.SectionInformation.IsProtected == false)
            {
                // DPAPIによる暗号化  
                section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
            }
            SetSettingsItem(config, "RedmineHost", RedmineHost);
            SetSettingsItem(config, "Login", Login);
            SetSettingsItem(config, "Password", Password);
            SetSettingsItem(config, "ApiKey", ApiKey);
            SetSettingsItem(config, "ApplicationTheme", ApplicationThemeManager.GetAppTheme().ToString());
            config.Save();
        }

        /// <summary>
        /// AppSettings
        /// </summary>
        /// <param name="config"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private static void SetSettingsItem(Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }
        }

        /// <summary>
        /// 設定情報の読み込み
        /// </summary>
        public static void Load()
        {
            // app.configの読み込み
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // 暗号化するセクションの取得
            var section = config.GetSection("appSettings") as AppSettingsSection;
            if (section != null && section.SectionInformation.IsProtected == true)
            {
                // セクションの復号化
                section.SectionInformation.UnprotectSection();
            }

            var redmineHost = ConfigurationManager.AppSettings["RedmineHost"];
            RedmineHost = string.IsNullOrEmpty(redmineHost) != true ? redmineHost : "";

            var apiKey = ConfigurationManager.AppSettings["ApiKey"];
            ApiKey = string.IsNullOrEmpty(apiKey) != true ? apiKey : "";

            var login = ConfigurationManager.AppSettings["Login"];
            Login = string.IsNullOrEmpty(login) != true ? login : "";

            var password = ConfigurationManager.AppSettings["Password"];
            Password = string.IsNullOrEmpty(password) != true ? password : "";

            var applicationTheme = ConfigurationManager.AppSettings["ApplicationTheme"];
            applicationTheme = string.IsNullOrEmpty(applicationTheme) != true ? applicationTheme : "";

            var currentTheme = (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), applicationTheme);
            ApplicationThemeManager.Apply(currentTheme);
        }
    }
}
