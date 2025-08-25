using System.Collections.Specialized;
using Redmine.Net.Api;
using Redmine.Net.Api.Net;
using Redmine.Net.Api.Types;

namespace RedmineClient.Services
{
    public class RedmineService : IDisposable
    {
        private readonly RedmineManager _redmineManager;

        public RedmineService(string baseUrl, string apiKey)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("baseUrl cannot be null or empty", nameof(baseUrl));
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("apiKey cannot be null or empty", nameof(apiKey));

            System.Diagnostics.Debug.WriteLine($"RedmineService: 接続設定 - Host: {baseUrl}, API Key: {apiKey.Substring(0, Math.Min(8, apiKey.Length))}...");

            var builder = new RedmineManagerOptionsBuilder();
            builder.WithHost(baseUrl.TrimEnd('/'));
            builder.WithApiKeyAuthentication(apiKey);
            _redmineManager = new RedmineManager(builder);

            System.Diagnostics.Debug.WriteLine($"RedmineService: RedmineManager初期化完了");
        }

        /// <summary>
        /// プロジェクト一覧を取得
        /// </summary>
        public List<RedmineProject> GetProjects()
        {
            try
            {
                var projects = _redmineManager.Get<Project>(new RequestOptions());
                return projects.Select(p => new RedmineProject
                {
                    Id          = p.Id,
                    Name        = p.Name        ?? string.Empty,
                    Identifier  = p.Identifier  ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    CreatedOn   = p.CreatedOn,
                    UpdatedOn   = p.UpdatedOn
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"プロジェクト一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 指定されたプロジェクトのチケット一覧を取得
        /// </summary>
        public List<RedmineIssue> GetIssues(int projectId, int? limit = null, int? offset = null)
        {
            try
            {
                var options = new RequestOptions();
                if (limit.HasValue)
                    options.QueryString.Add(RedmineKeys.LIMIT, limit.Value.ToString());
                if (offset.HasValue)
                    options.QueryString.Add(RedmineKeys.OFFSET, offset.Value.ToString());

                var issues = _redmineManager.Get<Issue>(options);
                return issues.Where(i => i.Project?.Id == projectId)
                           .Select(i => new RedmineIssue
                           {
                               Id             = i.Id,
                               Subject        = i.Subject          ?? string.Empty,
                               Description    = i.Description      ?? string.Empty,
                               Status         = i.Status?.Name     ?? string.Empty,
                               Priority       = i.Priority?.Name   ?? string.Empty,
                               Author         = i.Author?.Name     ?? string.Empty,
                               AssignedTo     = i.AssignedTo?.Name ?? string.Empty,
                               ProjectId      = i.Project?.Id      ?? 0,
                               ProjectName    = i.Project?.Name    ?? string.Empty,
                               Tracker        = i.Tracker?.Name    ?? string.Empty,
                               StartDate      = i.StartDate,
                               DueDate        = i.DueDate,
                               DoneRatio      = i.DoneRatio        ?? 0,
                               EstimatedHours = i.EstimatedHours,
                               ParentId       = null, // TODO: 階層構造の取得方法を調査
                               CreatedOn      = i.CreatedOn,
                               UpdatedOn      = i.UpdatedOn
                           }).ToList();
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケット一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 指定されたチケットの詳細情報を取得
        /// </summary>
        public RedmineIssue? GetIssue(int issueId)
        {
            try
            {
                var issue = _redmineManager.Get<Issue>(issueId.ToString(), new RequestOptions()
                {
                    QueryString = new NameValueCollection()
                    {
                        {RedmineKeys.ID, issueId.ToString()},
                        {RedmineKeys.INCLUDE, RedmineKeys.JOURNALS},
                    }
                });
                if (issue == null) return null;

                return new RedmineIssue
                {
                    Id             = issue.Id,
                    Subject        = issue.Subject          ?? string.Empty,
                    Description    = issue.Description      ?? string.Empty,
                    Status         = issue.Status?.Name     ?? string.Empty,
                    Priority       = issue.Priority?.Name   ?? string.Empty,
                    Author         = issue.Author?.Name     ?? string.Empty,
                    AssignedTo     = issue.AssignedTo?.Name ?? string.Empty,
                    ProjectId      = issue.Project?.Id      ?? 0,
                    ProjectName    = issue.Project?.Name    ?? string.Empty,
                    Tracker        = issue.Tracker?.Name    ?? string.Empty,
                    StartDate      = issue.StartDate,
                    DueDate        = issue.DueDate,
                    DoneRatio      = issue.DoneRatio        ?? 0,
                    EstimatedHours = issue.EstimatedHours,
                    ParentId       = null, // TODO: 階層構造の取得方法を調査
                    CreatedOn      = issue.CreatedOn,
                    UpdatedOn      = issue.UpdatedOn
                };
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケット詳細の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ユーザー情報を取得（接続テスト用）
        /// </summary>
        public RedmineUser? GetCurrentUser()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RedmineService: 現在のユーザー情報取得開始");
                var user = _redmineManager.Get<User>(RedmineKeys.CURRENT_USER);
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("RedmineService: ユーザー情報がnull");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"RedmineService: ユーザー情報取得成功 - ID: {user.Id}, Login: {user.Login}");
                return new RedmineUser
                {
                    Id          = user.Id,
                    Login       = user.Login     ?? string.Empty,
                    FirstName   = user.FirstName ?? string.Empty,
                    LastName    = user.LastName  ?? string.Empty,
                    Email       = user.Email     ?? string.Empty,
                    CreatedOn   = user.CreatedOn,
                    LastLoginOn = user.LastLoginOn
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RedmineService: ユーザー情報取得失敗 - エラー: {ex.GetType().Name}: {ex.Message}");
                throw new RedmineApiException($"ユーザー情報の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットの階層構造を取得
        /// </summary>
        public List<RedmineIssue> GetIssuesWithHierarchy(int projectId)
        {
            try
            {
                // 全チケットを取得
                var allIssues = GetIssues(projectId, 1000, 0);

                // 親チケットを取得
                var parentIssues = allIssues.Where(i => i.ParentId == null).ToList();

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
            var children = allIssues.Where(i => i.ParentId == parent.Id).ToList();
            parent.Children = children;

            foreach (var child in children)
            {
                BuildHierarchy(child, allIssues);
            }
        }

        /// <summary>
        /// 接続テストを実行
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RedmineService: 接続テスト開始");
                var user = GetCurrentUser();
                var result = user != null;
                System.Diagnostics.Debug.WriteLine($"RedmineService: 接続テスト結果 - 成功: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RedmineService: 接続テスト失敗 - エラー: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            // RedmineManagerはIDisposableではないため、何もしない
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



    // Redmineデータモデル
    public class RedmineProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class RedmineIssue
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Tracker { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public double DoneRatio { get; set; }
        public double? EstimatedHours { get; set; }
        public int? ParentId { get; set; }
        public RedmineIssue Parent { get; set; }
        public List<RedmineIssue> Children { get; set; } = new();
    }



    public class RedmineUser
    {
        public int Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? CreatedOn { get; set; }
        public DateTime? LastLoginOn { get; set; }
    }
}
