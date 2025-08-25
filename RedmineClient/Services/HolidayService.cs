using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text; // Added for Encoding

namespace RedmineClient.Services
{
    /// <summary>
    /// 日本の祝日データを管理するサービス
    /// </summary>
    public class HolidayService
    {
        private static readonly Dictionary<int, HashSet<DateTime>> _holidayCache = new Dictionary<int, HashSet<DateTime>>();
        private static readonly object _lockObject = new object();
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromDays(1); // 1日間キャッシュ

        /// <summary>
        /// 指定された日付が祝日かどうかを判定する
        /// </summary>
        /// <param name="date">判定する日付</param>
        /// <returns>祝日の場合はtrue</returns>
        public static bool IsHoliday(DateTime date)
        {
            var year = date.Year;
            var holidays = GetHolidaysForYear(year);
            return holidays.Contains(date.Date);
        }

        /// <summary>
        /// 指定された年の祝日リストを取得する
        /// </summary>
        /// <param name="year">年</param>
        /// <returns>祝日の日付セット</returns>
        private static HashSet<DateTime> GetHolidaysForYear(int year)
        {
            lock (_lockObject)
            {
                // キャッシュが有効で、かつ該当年のデータがある場合はキャッシュから返す
                if (DateTime.Now - _lastUpdate < _cacheExpiration && _holidayCache.ContainsKey(year))
                {
                    return _holidayCache[year];
                }

                // キャッシュが期限切れまたは該当年のデータがない場合は更新
                if (DateTime.Now - _lastUpdate >= _cacheExpiration)
                {
                    _ = UpdateHolidayDataAsync();
                }

                // 該当年のデータがない場合は空のセットを返す
                if (!_holidayCache.ContainsKey(year))
                {
                    return new HashSet<DateTime>();
                }

                return _holidayCache[year];
            }
        }

        /// <summary>
        /// 祝日データを更新する
        /// </summary>
        private static async Task UpdateHolidayDataAsync()
        {
            try
            {
                var success = await LoadHolidayDataFromAssetsAsync();
                
                // 祝日データの更新完了
            }
            catch (Exception ex)
            {
                // 祝日データの更新に失敗
            }
        }

        /// <summary>
        /// Assetsフォルダーから祝日データを読み込む
        /// </summary>
        /// <returns>祝日データの読み込みが成功したかどうか</returns>
        private static async Task<bool> LoadHolidayDataFromAssetsAsync()
        {
            try
            {
                // 実行ファイルのディレクトリからAssetsフォルダーを検索
                var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var assetsPath = Path.Combine(exeDir, "Assets", "syukujitsu.csv");

                // プロジェクトルートのAssetsフォルダーもフォールバックとして検索
                if (!File.Exists(assetsPath))
                {
                    var projectDir = Directory.GetCurrentDirectory();
                    assetsPath = Path.Combine(projectDir, "Assets", "syukujitsu.csv");
                }

                if (!File.Exists(assetsPath))
                {
                    return false;
                }

                // UTF-8（BOMなし）でファイルを読み込み
                var csvContent = await File.ReadAllTextAsync(assetsPath, Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(csvContent))
                {
                    return false;
                }

                // CSVデータを解析
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1) // ヘッダー行のみの場合
                {
                    return false;
                }

                lock (_lockObject)
                {
                    _holidayCache.Clear();
                    
                    // ヘッダー行をスキップして2行目から処理
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        var columns = line.Split(',');
                        if (columns.Length >= 2)
                        {
                            var dateStr = columns[0].Trim().Trim('"');
                            var holidayName = columns[1].Trim().Trim('"');

                            if (DateTime.TryParseExact(dateStr, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None, out var holidayDate))
                            {
                                var year = holidayDate.Year;
                                if (!_holidayCache.ContainsKey(year))
                                {
                                    _holidayCache[year] = new HashSet<DateTime>();
                                }
                                _holidayCache[year].Add(holidayDate);
                            }
                        }
                    }

                    _lastUpdate = DateTime.Now;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



        /// <summary>
        /// 祝日データを手動で更新する
        /// </summary>
        public static async Task ForceUpdateAsync()
        {
            await UpdateHolidayDataAsync();
        }
    }
}
