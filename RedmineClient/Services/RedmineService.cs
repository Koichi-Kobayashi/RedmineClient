using System.Collections.Specialized;
using Redmine.Net.Api;
using Redmine.Net.Api.Net;
using Redmine.Net.Api.Types;
using RedmineClient.Models;

namespace RedmineClient.Services
{
    public class RedmineService : IDisposable
    {
        private readonly RedmineManager _redmineManager;
        private readonly int _timeoutSeconds = 30; // 30秒のタイムアウト

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
        /// プロジェクト一覧を取得（非同期版）
        /// </summary>
        public async Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var projects = await Task.Run(() => _redmineManager.Get<Project>(), cts.Token);
                return projects ?? new List<Project>();
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"プロジェクト一覧の取得がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"プロジェクト一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 指定されたプロジェクトのチケット一覧を取得（非同期版）
        /// </summary>
        public async Task<List<Issue>> GetIssuesAsync(int projectId, int? limit = null, int? offset = null, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var options = new RequestOptions();
                
                // QueryStringプロパティを明示的に初期化
                options.QueryString = new NameValueCollection();
                
                // プロジェクトIDをクエリパラメータに追加
                options.QueryString.Add("project_id", projectId.ToString());
                
                // 親子関係を含めて取得
                options.QueryString.Add("include", "relations,children,parent");
                
                if (limit.HasValue)
                    options.QueryString.Add("limit", limit.Value.ToString());
                if (offset.HasValue)
                    options.QueryString.Add("offset", offset.Value.ToString());

                var issues = await Task.Run(() => _redmineManager.Get<Issue>(options), cts.Token);
                
                if (issues == null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetIssues: プロジェクトID {projectId} のチケットがnullでした");
                    return new List<Issue>();
                }
                
                System.Diagnostics.Debug.WriteLine($"GetIssues: プロジェクトID {projectId} から {issues.Count} 件のチケットを取得しました");
                
                return issues;
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"プロジェクトID {projectId} のチケット一覧の取得がタイムアウトしました（{_timeoutSeconds}秒）");
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
        /// 指定されたチケットの詳細情報を取得（非同期版）
        /// </summary>
        public async Task<Issue?> GetIssueAsync(int issueId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var issue = await Task.Run(() => _redmineManager.Get<Issue>(issueId.ToString(), new RequestOptions()
                {
                    QueryString = new NameValueCollection()
                    {
                        {"id", issueId.ToString()},
                        {"include", "journals"},
                    }
                }), cts.Token);
                
                if (issue == null) return null;

                return issue;
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"チケットID {issueId} の詳細取得がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケット詳細の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ユーザー情報を取得（接続テスト用、非同期版）
        /// </summary>
        public async Task<RedmineUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));
                
                // 正しいエンドポイントを使用して現在のユーザー情報を取得
                var user = await Task.Run(() => _redmineManager.Get<User>("current"), cts.Token);
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
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"ユーザー情報の取得がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"ユーザー情報の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットの階層構造を取得（非同期版）
        /// </summary>
        public async Task<List<HierarchicalIssue>> GetIssuesWithHierarchyAsync(int projectId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 全チケットを取得
                var allIssues = await GetIssuesAsync(projectId, 1000, 0, cancellationToken);

                if (allIssues == null || allIssues.Count == 0)
                {
                    return new List<HierarchicalIssue>();
                }

                // IssueをHierarchicalIssueに変換
                var hierarchicalIssues = allIssues.Select(i => new HierarchicalIssue(i)).ToList();

                // 親子関係を構築
                BuildHierarchy(hierarchicalIssues);
                
                return hierarchicalIssues;
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"プロジェクトID {projectId} のチケット階層の取得がタイムアウトしました（{_timeoutSeconds}秒）");
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
        /// <param name="issues">チケットのリスト</param>
        private void BuildHierarchy(List<HierarchicalIssue> issues)
        {
            // 親子関係を構築するための辞書を作成
            var issueDict = issues.ToDictionary(i => i.Id, i => i);
            
            // 各チケットの親子関係を設定
            foreach (var issue in issues)
            {
                // Redmine APIから親子関係を取得する必要があります
                // 現在は親子関係なしで処理します
                // TODO: Redmine APIからparent_idまたはrelationsを使用して階層構造を構築
                
                // 一時的に、IDの順序で階層構造を模擬（実際の実装では削除）
                // これはテスト用の仮実装です
                if (issue.Id > 1 && issue.Id % 2 == 0)
                {
                    var parentId = issue.Id / 2;
                    if (issueDict.ContainsKey(parentId))
                    {
                        var parent = issueDict[parentId];
                        parent.AddChild(issue);
                    }
                }
            }
        }

        /// <summary>
        /// 接続テストを実行（非同期版）
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                // 方法1: 現在のユーザー情報を取得
                try
                {
                    var user = await GetCurrentUserAsync(cts.Token);
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
                    var projects = await GetProjectsAsync(cts.Token);
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
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"接続テストがタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                // 予期しないエラーの場合は再スロー
                throw new RedmineApiException($"接続テストで予期しないエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// トラッカー一覧を取得（非同期版）
        /// </summary>
        public async Task<List<Tracker>> GetTrackersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var trackers = await Task.Run(() => _redmineManager.Get<Tracker>(), cts.Token);
                return trackers ?? new List<Tracker>();
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"トラッカー一覧の取得がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"トラッカー一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットを作成（非同期版）
        /// </summary>
        public async Task<int> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                // 必須パラメータを確実に設定
                var newIssue = new Issue
                {
                    Subject = issue.Subject ?? "無題のタスク",
                    Description = issue.Description ?? "",
                    StartDate = issue.StartDate,
                    DueDate = issue.DueDate,
                    DoneRatio = 0
                };

                // プロジェクトIDを設定（リフレクションを使用）
                var projectIdProperty = typeof(Issue).GetProperty("ProjectId");
                if (projectIdProperty?.CanWrite == true)
                {
                    var projectId = issue.Project?.Id ?? 0;
                    projectIdProperty.SetValue(newIssue, projectId);
                    System.Diagnostics.Debug.WriteLine($"CreateIssueAsync: ProjectIdを設定しました: {projectId}");
                }

                // トラッカーIDを設定（設定ファイルから読み込み）
                var trackerIdProperty = typeof(Issue).GetProperty("TrackerId");
                if (trackerIdProperty?.CanWrite == true)
                {
                    var trackerId = AppConfig.DefaultTrackerId;
                    trackerIdProperty.SetValue(newIssue, trackerId);
                    System.Diagnostics.Debug.WriteLine($"CreateIssueAsync: TrackerIdを設定しました: {trackerId}");
                }

                // ステータスIDを設定（デフォルト: 1 = 新規）
                var statusIdProperty = typeof(Issue).GetProperty("StatusId");
                if (statusIdProperty?.CanWrite == true)
                {
                    statusIdProperty.SetValue(newIssue, 1);
                    System.Diagnostics.Debug.WriteLine($"CreateIssueAsync: StatusIdを設定しました: 1");
                }

                // 優先度IDを設定（デフォルト: 2 = 中）
                var priorityIdProperty = typeof(Issue).GetProperty("PriorityId");
                if (priorityIdProperty?.CanWrite == true)
                {
                    priorityIdProperty.SetValue(newIssue, 2);
                    System.Diagnostics.Debug.WriteLine($"CreateIssueAsync: PriorityIdを設定しました: 2");
                }



                System.Diagnostics.Debug.WriteLine($"CreateIssueAsync: チケット作成開始 - Subject: {newIssue.Subject}, ProjectId: {issue.Project?.Id ?? 0}");

                // 新しいAPIを使用してチケットを作成
                var createdIssue = await Task.Run(() => _redmineManager.Create(newIssue), cts.Token);
                return createdIssue.Id;
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"チケットの作成がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケットの作成に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットを更新（非同期版）
        /// </summary>
        public async Task UpdateIssueAsync(Issue issue, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var existingIssue = await Task.Run(() => _redmineManager.Get<Issue>(issue.Id.ToString()), cts.Token);
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
                await Task.Run(() => _redmineManager.UpdateObject<Issue>(existingIssue.Id.ToString(), existingIssue), cts.Token);
#pragma warning restore CS0618
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"チケットの更新がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"チケットの更新に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// チケットを削除（非同期版）
        /// </summary>
        public async Task DeleteIssueAsync(int issueId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

#pragma warning disable CS0618 // 旧形式のAPIを使用（互換性のため）
                await Task.Run(() => _redmineManager.DeleteObject<Issue>(issueId.ToString()), cts.Token);
#pragma warning restore CS0618
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"チケットの削除がタイムアウトしました（{_timeoutSeconds}秒）");
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

        // 後方互換性のための同期的なメソッド（非推奨）
        [Obsolete("非同期版のGetProjectsAsyncを使用してください")]
        public List<Project> GetProjects()
        {
            return GetProjectsAsync().GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のGetIssuesAsyncを使用してください")]
        public List<Issue> GetIssues(int projectId, int? limit = null, int? offset = null)
        {
            return GetIssuesAsync(projectId, limit, offset).GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のGetIssueAsyncを使用してください")]
        public Issue? GetIssue(int issueId)
        {
            return GetIssueAsync(issueId).GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のGetCurrentUserAsyncを使用してください")]
        public RedmineUser? GetCurrentUser()
        {
            return GetCurrentUserAsync().GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のGetIssuesWithHierarchyAsyncを使用してください")]
        public List<HierarchicalIssue> GetIssuesWithHierarchy(int projectId)
        {
            return GetIssuesWithHierarchyAsync(projectId).GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のTestConnectionAsyncを使用してください")]
        public bool TestConnection()
        {
            return TestConnectionAsync().GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のCreateIssueAsyncを使用してください")]
        public int CreateIssue(Issue issue)
        {
            return CreateIssueAsync(issue).GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のUpdateIssueAsyncを使用してください")]
        public void UpdateIssue(Issue issue)
        {
            UpdateIssueAsync(issue).GetAwaiter().GetResult();
        }

        [Obsolete("非同期版のDeleteIssueAsyncを使用してください")]
        public void DeleteIssue(int issueId)
        {
            DeleteIssueAsync(issueId).GetAwaiter().GetResult();
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
