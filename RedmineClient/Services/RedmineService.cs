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

            // URLの形式を正規化
            var normalizedUrl = baseUrl.TrimEnd('/');
            if (!normalizedUrl.StartsWith("http://") && !normalizedUrl.StartsWith("https://"))
            {
                normalizedUrl = "https://" + normalizedUrl;
            }

            var builder = new RedmineManagerOptionsBuilder();
            builder.WithHost(normalizedUrl);
            builder.WithApiKeyAuthentication(apiKey);
            _redmineManager = new RedmineManager(builder);
        }

        /// <summary>
        /// プロジェクト一覧を取得
        /// </summary>
        public List<Project> GetProjects()
        {
            try
            {
                var projects = _redmineManager.Get<Project>();
                return projects ?? new List<Project>();
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
                
                // QueryStringプロパティを明示的に初期化
                options.QueryString = new NameValueCollection();
                
                // プロジェクトIDをクエリパラメータに追加
                options.QueryString.Add("project_id", projectId.ToString());
                
                // 親子関係を含めて取得
                options.QueryString.Add("include", "relations,children");
                
                if (limit.HasValue)
                    options.QueryString.Add("limit", limit.Value.ToString());
                if (offset.HasValue)
                    options.QueryString.Add("offset", offset.Value.ToString());

                var issues = _redmineManager.Get<Issue>(options);
                
                if (issues == null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetIssues: プロジェクトID {projectId} のチケットがnullでした");
                    return new List<RedmineIssue>();
                }
                
                System.Diagnostics.Debug.WriteLine($"GetIssues: プロジェクトID {projectId} から {issues.Count} 件のチケットを取得しました");
                
                return issues.Select(i => new RedmineIssue
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
                    ParentId       = null, // 親チケットのID（一時的にnull）
                    CreatedOn      = i.CreatedOn,
                    UpdatedOn      = i.UpdatedOn
                }).ToList();
            }
            catch (Exception ex)
            {
                // より詳細なエラー情報を提供
                var errorMessage = $"プロジェクトID {projectId} のチケット一覧の取得に失敗しました。";
                if (ex is RedmineApiException redmineEx)
                {
                    errorMessage += $" Redmine API エラー: {redmineEx.Message}";
                }
                else
                {
                    errorMessage += $" エラー: {ex.Message}";
                }
                
                throw new RedmineApiException(errorMessage, ex);
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
                        {"id", issueId.ToString()},
                        {"include", "journals"},
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
                    ParentId       = null, // 親チケットのID（一時的にnull）
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
                
                // 正しいエンドポイントを使用して現在のユーザー情報を取得
                var user = _redmineManager.Get<User>("current");
                if (user == null)
                {
                    return null;
                }

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

                if (allIssues == null || allIssues.Count == 0)
                {
                    return new List<RedmineIssue>();
                }

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
                // より詳細なエラー情報を提供
                var errorMessage = $"プロジェクトID {projectId} のチケット階層の取得に失敗しました。";
                if (ex is RedmineApiException redmineEx)
                {
                    errorMessage += $" Redmine API エラー: {redmineEx.Message}";
                }
                else
                {
                    errorMessage += $" エラー: {ex.Message}";
                }
                
                throw new RedmineApiException(errorMessage, ex);
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
                // 方法1: 現在のユーザー情報を取得
                try
                {
                    var user = GetCurrentUser();
                    if (user != null)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // ユーザー情報取得でエラーが発生した場合の詳細ログ
                    if (ex is RedmineApiException redmineEx)
                    {
                        // Redmine API固有のエラーの場合は詳細情報を記録
                        throw new RedmineApiException($"ユーザー情報取得でエラー: {redmineEx.Message}", redmineEx);
                    }
                }

                // 方法2: プロジェクト一覧を取得して接続をテスト
                try
                {
                    var projects = GetProjects();
                    if (projects != null && projects.Count > 0)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // プロジェクト一覧取得でエラーが発生した場合の詳細ログ
                    if (ex is RedmineApiException redmineEx)
                    {
                        // Redmine API固有のエラーの場合は詳細情報を記録
                        throw new RedmineApiException($"プロジェクト一覧取得でエラー: {redmineEx.Message}", redmineEx);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // 予期しないエラーの場合は再スロー
                throw new RedmineApiException($"接続テストで予期しないエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットを作成
        /// </summary>
        public int CreateIssue(RedmineIssue issue)
        {
            try
            {
                var newIssue = new Issue
                {
                    Subject = issue.Subject,
                    Description = issue.Description,
                    StartDate = issue.StartDate,
                    DueDate = issue.DueDate,
                    DoneRatio = 0
                };

                // プロパティをリフレクションで設定（読み取り専用プロパティのため）
                var projectProperty = typeof(Issue).GetProperty("Project");
                if (projectProperty?.CanWrite == true)
                {
                    var project = new IdentifiableName();
                    var projectIdProperty = typeof(IdentifiableName).GetProperty("Id");
                    if (projectIdProperty?.CanWrite == true)
                    {
                        projectIdProperty.SetValue(project, issue.ProjectId);
                    }
                    projectProperty.SetValue(newIssue, project);
                }

                var trackerProperty = typeof(Issue).GetProperty("Tracker");
                if (trackerProperty?.CanWrite == true)
                {
                    var tracker = new IdentifiableName();
                    var trackerIdProperty = typeof(IdentifiableName).GetProperty("Id");
                    if (trackerIdProperty?.CanWrite == true)
                    {
                        trackerIdProperty.SetValue(tracker, 1); // デフォルトのトラッカーID
                    }
                    trackerProperty.SetValue(newIssue, tracker);
                }

                var statusProperty = typeof(Issue).GetProperty("Status");
                if (statusProperty?.CanWrite == true)
                {
                    var status = new IdentifiableName();
                    var statusIdProperty = typeof(IdentifiableName).GetProperty("Id");
                    if (statusIdProperty?.CanWrite == true)
                    {
                        statusIdProperty.SetValue(status, 1); // デフォルトのステータスID（新規）
                    }
                    statusProperty.SetValue(newIssue, status);
                }

                var priorityProperty = typeof(Issue).GetProperty("Priority");
                if (priorityProperty?.CanWrite == true)
                {
                    var priority = new IdentifiableName();
                    var priorityIdProperty = typeof(IdentifiableName).GetProperty("Id");
                    if (priorityIdProperty?.CanWrite == true)
                    {
                        priorityIdProperty.SetValue(priority, 2); // デフォルトの優先度ID（中）
                    }
                    priorityProperty.SetValue(newIssue, priority);
                }

                var estimatedHoursProperty = typeof(Issue).GetProperty("EstimatedHours");
                if (estimatedHoursProperty?.CanWrite == true)
                {
                    estimatedHoursProperty.SetValue(newIssue, (float?)issue.EstimatedHours);
                }

                // チケットを作成（Redmine.Net.Apiの新しいメソッドを使用）
#pragma warning disable CS0618 // 旧形式のAPIを使用（互換性のため）
                var createdIssue = _redmineManager.CreateObject<Issue>(newIssue);
#pragma warning restore CS0618
                return createdIssue.Id;
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケットの作成に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットを更新
        /// </summary>
        public void UpdateIssue(RedmineIssue issue)
        {
            try
            {
                var existingIssue = _redmineManager.Get<Issue>(issue.Id.ToString());
                if (existingIssue == null)
                {
                    throw new RedmineApiException($"チケットID {issue.Id} が見つかりません。");
                }

                existingIssue.Subject = issue.Subject;
                existingIssue.Description = issue.Description;
                existingIssue.StartDate = issue.StartDate;
                existingIssue.DueDate = issue.DueDate;
                
                // EstimatedHoursプロパティをリフレクションで設定
                var estimatedHoursProperty = typeof(Issue).GetProperty("EstimatedHours");
                if (estimatedHoursProperty?.CanWrite == true)
                {
                    estimatedHoursProperty.SetValue(existingIssue, (float?)issue.EstimatedHours);
                }

                // DoneRatioプロパティをリフレクションで設定
                var doneRatioProperty = typeof(Issue).GetProperty("DoneRatio");
                if (doneRatioProperty?.CanWrite == true)
                {
                    doneRatioProperty.SetValue(existingIssue, (float?)issue.DoneRatio);
                }

#pragma warning disable CS0618 // 旧形式のAPIを使用（互換性のため）
                _redmineManager.UpdateObject<Issue>(existingIssue.Id.ToString(), existingIssue);
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケットの更新に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットを削除
        /// </summary>
        public void DeleteIssue(int issueId)
        {
            try
            {
#pragma warning disable CS0618 // 旧形式のAPIを使用（互換性のため）
                _redmineManager.DeleteObject<Issue>(issueId.ToString());
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケットの削除に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// トラッカー一覧を取得
        /// </summary>
        public List<Tracker> GetTrackers()
        {
            try
            {
                var trackers = _redmineManager.Get<Tracker>();
                return trackers ?? new List<Tracker>();
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"トラッカー一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ステータス一覧を取得
        /// </summary>
        public List<IssueStatus> GetIssueStatuses()
        {
            try
            {
                var statuses = _redmineManager.Get<IssueStatus>();
                return statuses ?? new List<IssueStatus>();
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"ステータス一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 優先度一覧を取得
        /// </summary>
        public List<IssuePriority> GetIssuePriorities()
        {
            try
            {
                var priorities = _redmineManager.Get<IssuePriority>();
                return priorities ?? new List<IssuePriority>();
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"優先度一覧の取得に失敗しました: {ex.Message}", ex);
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
