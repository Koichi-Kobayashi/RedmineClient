using System.Configuration;
using Wpf.Ui.Appearance;

namespace RedmineClient.Models
{
    public class AppConfig
    {
        private static bool _isLoaded = false;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();
        
        public static string RedmineHost { get; set; }
        public static string ApiKey { get; set; }
        public static double WindowWidth { get; set; } = 1100;
        public static double WindowHeight { get; set; } = 650;
        public static double WindowLeft { get; set; } = 100;
        public static double WindowTop { get; set; } = 100;
        public static string WindowState { get; set; } = "Normal";
        public static double TaskDetailWidth { get; set; } = 400;
        public static ApplicationTheme ApplicationTheme { get; set; } = ApplicationTheme.Light;
        private static string _scheduleStartYearMonth = "";
        private static int? _selectedProjectId = null;
        private static int _defaultTrackerId = 1;
        private static int _defaultStatusId = 1;
        private static bool _showTodayLine = true;
        private static int _currentUserId = 0;
        private static List<TrackerItem> _availableTrackers = new();
        private static List<StatusItem> _availableStatuses = new();

        public static string Theme
        {
            get => GetSetting("Theme", "Light");
            set => SetSetting("Theme", value);
        }

        public static string ScheduleStartYearMonth
        {
            get => _scheduleStartYearMonth;
            set
            {
                _scheduleStartYearMonth = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SetSetting("ScheduleStartYearMonth", value);
                }
            }
        }

        /// <summary>
        /// 選択されたプロジェクトID
        /// </summary>
        public static int? SelectedProjectId
        {
            get => _selectedProjectId;
            set
            {
                _selectedProjectId = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SetSetting("SelectedProjectId", value.ToString());
                }
            }
        }

        /// <summary>
        /// デフォルトのトラッカーID
        /// </summary>
        public static int DefaultTrackerId
        {
            get => _defaultTrackerId;
            set
            {
                _defaultTrackerId = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SetSetting("DefaultTrackerId", value.ToString());
                }
            }
        }

        /// <summary>
        /// デフォルトのステータスID
        /// </summary>
        public static int DefaultStatusId
        {
            get => _defaultStatusId;
            set
            {
                _defaultStatusId = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SetSetting("DefaultStatusId", value.ToString());
                }
            }
        }

        /// <summary>
        /// 今日の日付ラインを表示するかどうか
        /// </summary>
        public static bool ShowTodayLine
        {
            get => _showTodayLine;
            set
            {
                _showTodayLine = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SetSetting("ShowTodayLine", value.ToString());
                }
            }
        }

        /// <summary>
        /// 現在のユーザーID
        /// </summary>
        public static int CurrentUserId
        {
            get => _currentUserId;
            set
            {
                _currentUserId = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SetSetting("CurrentUserId", value.ToString());
                }
            }
        }

        /// <summary>
        /// 利用可能なトラッカー一覧
        /// </summary>
        public static List<TrackerItem> AvailableTrackers
        {
            get => _availableTrackers;
            set
            {
                _availableTrackers = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SaveTrackers(_availableTrackers);
                }
            }
        }

        /// <summary>
        /// 利用可能なステータス一覧
        /// </summary>
        public static List<StatusItem> AvailableStatuses
        {
            get => _availableStatuses;
            set
            {
                _availableStatuses = value;
                // 初期化完了後にのみ保存を実行
                if (_isInitialized)
                {
                    SaveStatuses(_availableStatuses);
                }
            }
        }

        /// <summary>
        /// 初期化時にScheduleStartYearMonthの値を取得する（setアクセサーを呼び出さない）
        /// </summary>
        /// <returns>ScheduleStartYearMonthの値</returns>
        public static string GetScheduleStartYearMonthForInitialization()
        {
            return _scheduleStartYearMonth;
        }

        /// <summary>
        /// 設定値を取得
        /// </summary>
        private static string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                var value = ConfigurationManager.AppSettings[key];
                return string.IsNullOrEmpty(value) ? defaultValue : value;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 設定値を設定
        /// </summary>
        private static void SetSetting(string key, string value)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                SetSettingsItem(config, key, value);
                config.Save();
            }
            catch
            {
                // エラー処理は必要に応じて実装
            }
        }

        /// <summary>
        /// 設定情報の保存
        /// </summary>
        public static void Save()
        {
            try
            {
                // app.configの読み込み
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                
                if (config == null)
                {
                    return;
                }

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
                SetSettingsItem(config, "WindowLeft", WindowLeft.ToString());
                SetSettingsItem(config, "WindowTop", WindowTop.ToString());
                SetSettingsItem(config, "WindowState", WindowState);
                SetSettingsItem(config, "TaskDetailWidth", TaskDetailWidth.ToString());
                SetSettingsItem(config, "ApplicationTheme", ApplicationTheme.ToString());
                SetSettingsItem(config, "ScheduleStartYearMonth", _scheduleStartYearMonth);
                SetSettingsItem(config, "SelectedProjectId", _selectedProjectId?.ToString() ?? "");
                SetSettingsItem(config, "DefaultTrackerId", DefaultTrackerId.ToString());
                SetSettingsItem(config, "DefaultStatusId", DefaultStatusId.ToString());
                SetSettingsItem(config, "ShowTodayLine", ShowTodayLine.ToString());
                SetSettingsItem(config, "CurrentUserId", CurrentUserId.ToString());
                
                config.Save();
            }
            catch
            {
                // 設定保存中にエラー
            }
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
                
                try
                {
                    // app.configの読み込み
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    
                    if (config == null)
                    {
                        SetDefaultValues();
                        _isLoaded = true;
                        return;
                    }

                    // 暗号化するセクションの取得
                    var section = config.GetSection("appSettings") as AppSettingsSection;
                    
                    if (section != null)
                    {
                        if (section.SectionInformation.IsProtected == true)
                        {
                            try
                            {
                                // セクションの復号化
                                section.SectionInformation.UnprotectSection();
                            }
                            catch (Exception)
                            {
                                // セクション復号化失敗
                            }
                        }
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

                    var windowLeft = ConfigurationManager.AppSettings["WindowLeft"];
                    if (double.TryParse(windowLeft, out double left))
                    {
                        WindowLeft = left;
                    }

                    var windowTop = ConfigurationManager.AppSettings["WindowTop"];
                    if (double.TryParse(windowTop, out double top))
                    {
                        WindowTop = top;
                    }

                    var windowState = ConfigurationManager.AppSettings["WindowState"];
                    if (!string.IsNullOrEmpty(windowState))
                    {
                        WindowState = windowState;
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

                    var scheduleStartYearMonth = ConfigurationManager.AppSettings["ScheduleStartYearMonth"];
                    if (!string.IsNullOrEmpty(scheduleStartYearMonth))
                    {
                        _scheduleStartYearMonth = scheduleStartYearMonth;
                    }
                    else
                    {
                        _scheduleStartYearMonth = DateTime.Now.ToString("yyyy/MM"); // 無効な値の場合は現在の年月をデフォルトとする
                    }

                    var selectedProjectId = ConfigurationManager.AppSettings["SelectedProjectId"];
                    if (!string.IsNullOrEmpty(selectedProjectId) && int.TryParse(selectedProjectId, out int id))
                    {
                        _selectedProjectId = id;
                    }
                    else
                    {
                        _selectedProjectId = null; // デフォルト値
                    }

                    var defaultTrackerId = ConfigurationManager.AppSettings["DefaultTrackerId"];
                    if (!string.IsNullOrEmpty(defaultTrackerId) && int.TryParse(defaultTrackerId, out int trackerId))
                    {
                        _defaultTrackerId = trackerId;
                    }
                    else
                    {
                        _defaultTrackerId = 1; // デフォルト値
                    }

                    var defaultStatusId = ConfigurationManager.AppSettings["DefaultStatusId"];
                    if (!string.IsNullOrEmpty(defaultStatusId) && int.TryParse(defaultStatusId, out int statusId))
                    {
                        _defaultStatusId = statusId;
                    }
                    else
                    {
                        _defaultStatusId = 1; // デフォルト値
                    }

                    var showTodayLine = ConfigurationManager.AppSettings["ShowTodayLine"];
                    if (!string.IsNullOrEmpty(showTodayLine) && bool.TryParse(showTodayLine, out bool todayLine))
                    {
                        _showTodayLine = todayLine;
                    }
                    else
                    {
                        _showTodayLine = true; // デフォルト値
                    }

                    var currentUserId = ConfigurationManager.AppSettings["CurrentUserId"];
                    if (!string.IsNullOrEmpty(currentUserId) && int.TryParse(currentUserId, out int userId))
                    {
                        _currentUserId = userId;
                    }
                    else
                    {
                        _currentUserId = 0; // デフォルト値
                    }
                }
                catch
                {
                    // エラーが発生した場合はデフォルト値を使用
                    SetDefaultValues();
                }
                
                _isLoaded = true;
                _isInitialized = true;
            }
        }

        /// <summary>
        /// デフォルト値を設定する
        /// </summary>
        private static void SetDefaultValues()
        {
            RedmineHost = "";
            ApiKey = "";
            WindowWidth = 1100;
            WindowHeight = 650;
            WindowLeft = 100;
            WindowTop = 100;
            WindowState = "Normal";
            TaskDetailWidth = 400;
            ApplicationTheme = ApplicationTheme.Light;
            _defaultTrackerId = 1;
            _defaultStatusId = 1;
            _currentUserId = 0;
            _scheduleStartYearMonth = DateTime.Now.ToString("yyyy/MM");
            _selectedProjectId = null; // デフォルト値
        }

        /// <summary>
        /// トラッカー一覧を保存
        /// </summary>
        public static void SaveTrackers(List<TrackerItem> trackers)
        {
            try
            {
                _availableTrackers = trackers ?? new List<TrackerItem>();
            }
            catch
            {
                // トラッカー一覧の保存でエラー
            }
        }

        /// <summary>
        /// ステータス一覧を保存
        /// </summary>
        public static void SaveStatuses(List<StatusItem> statuses)
        {
            try
            {
                _availableStatuses = statuses ?? new List<StatusItem>();
            }
            catch
            {
                // ステータス一覧の保存でエラー
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
