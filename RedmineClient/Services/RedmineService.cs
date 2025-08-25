using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using RedmineClient.Models;

namespace RedmineClient.Services
{
    public class RedmineService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedmineService(string baseUrl, string apiKey)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("baseUrl cannot be null or empty", nameof(baseUrl));
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("apiKey cannot be null or empty", nameof(apiKey));

            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// プロジェクト一覧を取得
        /// </summary>
        public async Task<List<RedmineProject>> GetProjectsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/projects.json");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var projectsResponse = JsonSerializer.Deserialize<RedmineProjectsResponse>(content, _jsonOptions);
                
                return projectsResponse?.Projects ?? new List<RedmineProject>();
            }
            catch (HttpRequestException ex)
            {
                throw new RedmineApiException($"プロジェクト一覧の取得に失敗しました: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new RedmineApiException($"プロジェクト一覧のJSON解析に失敗しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"プロジェクト一覧の取得中に予期しないエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 指定されたプロジェクトのチケット一覧を取得
        /// </summary>
        public async Task<List<RedmineIssue>> GetIssuesAsync(int projectId, int? limit = null, int? offset = null)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"project_id={projectId}",
                    "status_id=*" // 全ステータス
                };

                if (limit.HasValue)
                    queryParams.Add($"limit={limit.Value}");
                if (offset.HasValue)
                    queryParams.Add($"offset={offset.Value}");

                var queryString = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"{_baseUrl}/issues.json?{queryString}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var issuesResponse = JsonSerializer.Deserialize<RedmineIssuesResponse>(content, _jsonOptions);
                
                return issuesResponse?.Issues ?? new List<RedmineIssue>();
            }
            catch (HttpRequestException ex)
            {
                throw new RedmineApiException($"チケット一覧の取得に失敗しました: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new RedmineApiException($"チケット一覧のJSON解析に失敗しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケット一覧の取得中に予期しないエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 指定されたチケットの詳細情報を取得
        /// </summary>
        public async Task<RedmineIssue> GetIssueAsync(int issueId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/issues/{issueId}.json");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var issueResponse = JsonSerializer.Deserialize<RedmineIssueResponse>(content, _jsonOptions);
                
                return issueResponse?.Issue;
            }
            catch (HttpRequestException ex)
            {
                throw new RedmineApiException($"チケット詳細の取得に失敗しました: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new RedmineApiException($"チケット詳細のJSON解析に失敗しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケット詳細の取得中に予期しないエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ユーザー情報を取得（接続テスト用）
        /// </summary>
        public async Task<RedmineUser> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/users/current.json");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<RedmineUserResponse>(content, _jsonOptions);
                
                return userResponse?.User;
            }
            catch (HttpRequestException ex)
            {
                throw new RedmineApiException($"ユーザー情報の取得に失敗しました: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new RedmineApiException($"ユーザー情報のJSON解析に失敗しました: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"ユーザー情報の取得中に予期しないエラーが発生しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットの階層構造を取得
        /// </summary>
        public async Task<List<RedmineIssue>> GetIssuesWithHierarchyAsync(int projectId)
        {
            try
            {
                // 全チケットを取得
                var allIssues = await GetIssuesAsync(projectId, 1000, 0);
                
                // 親チケットを取得
                var parentIssues = allIssues.Where(i => i.Parent == null).ToList();
                
                // 階層構造を構築
                foreach (var parent in parentIssues)
                {
                    BuildHierarchy(parent, allIssues);
                }
                
                return parentIssues;
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケット階層の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 階層構造を構築
        /// </summary>
        private void BuildHierarchy(RedmineIssue parent, List<RedmineIssue> allIssues)
        {
            var children = allIssues.Where(i => i.Parent?.Id == parent.Id).ToList();
            parent.Children = children;
            
            foreach (var child in children)
            {
                BuildHierarchy(child, allIssues);
            }
        }

        /// <summary>
        /// 接続テストを実行
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Redmine API専用の例外クラス
    /// </summary>
    public class RedmineApiException : Exception
    {
        public RedmineApiException(string message) : base(message) { }
        public RedmineApiException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Redmine APIレスポンス用のクラス
    public class RedmineProjectsResponse
    {
        public List<RedmineProject> Projects { get; set; } = new();
        public int TotalCount { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
    }

    public class RedmineIssuesResponse
    {
        public List<RedmineIssue> Issues { get; set; } = new();
        public int TotalCount { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
    }

    public class RedmineIssueResponse
    {
        public RedmineIssue Issue { get; set; }
    }

    public class RedmineUserResponse
    {
        public RedmineUser User { get; set; }
    }

    // Redmineデータモデル
    public class RedmineProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class RedmineIssue
    {
        public int Id { get; set; }
        public RedmineProject Project { get; set; }
        public RedmineTracker Tracker { get; set; }
        public RedmineStatus Status { get; set; }
        public RedminePriority Priority { get; set; }
        public RedmineAuthor Author { get; set; }
        public RedmineAssignee AssignedTo { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public double DoneRatio { get; set; }
        public RedmineIssue Parent { get; set; }
        public List<RedmineIssue> Children { get; set; } = new();
        public List<RedmineCustomField> CustomFields { get; set; } = new();
    }

    public class RedmineTracker
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RedmineStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RedminePriority
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RedmineAuthor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RedmineAssignee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RedmineCustomField
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class RedmineUser
    {
        public int Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public DateTime LastLoginOn { get; set; }
    }
}
