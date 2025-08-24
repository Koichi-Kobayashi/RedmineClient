using System.Configuration;
using Wpf.Ui.Appearance;

namespace RedmineClient.Models
{
    public class AppConfig
    {
        private static bool _isLoaded = false;
        private static readonly object _lockObject = new object();
        
        public static string RedmineHost { get; set; }
        public static string ApiKey { get; set; }
        public static double WindowWidth { get; set; } = 1100;
        public static double WindowHeight { get; set; } = 650;
        public static double TaskDetailWidth { get; set; } = 400;
        public static ApplicationTheme ApplicationTheme { get; set; } = ApplicationTheme.Light;

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
            SetSettingsItem(config, "ApiKey", ApiKey);
            SetSettingsItem(config, "WindowWidth", WindowWidth.ToString());
            SetSettingsItem(config, "WindowHeight", WindowHeight.ToString());
            SetSettingsItem(config, "TaskDetailWidth", TaskDetailWidth.ToString());
            SetSettingsItem(config, "ApplicationTheme", ApplicationTheme.ToString());
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
            lock (_lockObject)
            {
                if (_isLoaded) return; // 既に読み込み済みの場合はスキップ
                
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

            var windowWidth = ConfigurationManager.AppSettings["WindowWidth"];
            if (double.TryParse(windowWidth, out double width))
            {
                WindowWidth = width;
            }

            var windowHeight = ConfigurationManager.AppSettings["WindowHeight"];
            if (double.TryParse(windowHeight, out double height))
            {
                WindowHeight = height;
            }

            var taskDetailWidth = ConfigurationManager.AppSettings["TaskDetailWidth"];
            if (double.TryParse(taskDetailWidth, out double detailWidth))
            {
                TaskDetailWidth = detailWidth;
            }

            var applicationTheme = ConfigurationManager.AppSettings["ApplicationTheme"];
            if (string.IsNullOrEmpty(applicationTheme) != true && Enum.TryParse<ApplicationTheme>(applicationTheme, out var theme))
            {
                ApplicationTheme = theme;
            }
            else
            {
                ApplicationTheme = ApplicationTheme.Light;
            }
                
                _isLoaded = true;
            }
        }

        /// <summary>
        /// テーマ設定を適用
        /// </summary>
        public static void ApplyTheme()
        {
            try
            {
                ApplicationThemeManager.Apply(ApplicationTheme);
            }
            catch
            {
                // デフォルトはライトテーマ
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
            }
        }
    }
}
