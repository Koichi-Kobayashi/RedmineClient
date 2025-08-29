using System.Collections.Specialized;
using Redmine.Net.Api;
using Redmine.Net.Api.Net;
using Redmine.Net.Api.Types;

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

            // SSL証明書の検証を無効化（RedmineServiceレベルでも設定）
            try
            {
                // 警告は出るが、Redmine.Net.Apiライブラリの動作に必要
                #pragma warning disable SYSLIB0014
                System.Net.ServicePointManager.ServerCertificateValidationCallback += 
                    (sender, cert, chain, sslPolicyErrors) => true;
                
                System.Net.ServicePointManager.SecurityProtocol = 
                    System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
                #pragma warning restore SYSLIB0014
                

            }
            catch (Exception)
            {
                // SSL証明書検証の無効化に失敗
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
                    return new List<Issue>();
                }
                
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
        /// IssueオブジェクトからParentIdを取得（リフレクション使用）
        /// </summary>
        /// <param name="issue">Issueオブジェクト</param>
        /// <returns>ParentId（取得できない場合はnull）</returns>
        private int? GetParentIdFromIssue(Issue issue)
        {
            try
            {
                // 複数のプロパティ名を試行
                var propertyNames = new[] { "ParentId", "parent_id", "Parent", "parent" };

                foreach (var propertyName in propertyNames)
                {
                    var property = issue.GetType().GetProperty(propertyName);
                    if (property != null)
                    {
                        var value = property.GetValue(issue);
                        if (value is int intValue)
                            return intValue;
                        // 修正: int?型の値はobjectとして返されるため、直接キャストしてnull判定
                        if (property.PropertyType == typeof(int?))
                            return (int?)value;
                        if (value is IdentifiableName identifiableName && identifiableName.Id > 0)
                            return identifiableName.Id;
                    }
                }

                return null;
            }
            catch
            {
                return null;
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
                // リフレクションでParentIdを取得
                var parentId = GetParentIdFromIssue(issue.Issue);
                
                if (parentId.HasValue && parentId.Value > 0)
                {
                    if (issueDict.ContainsKey(parentId.Value))
                    {
                        var parent = issueDict[parentId.Value];
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
            catch (RedmineApiException)
            {
                // RedmineApiExceptionはそのまま再スロー
                throw;
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
        /// ステータス一覧を取得（非同期版）
        /// </summary>
        public async Task<List<IssueStatus>> GetIssueStatusesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var statuses = await Task.Run(() => _redmineManager.Get<IssueStatus>(), cts.Token);
                return statuses ?? new List<IssueStatus>();
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"ステータス一覧の取得がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"ステータス一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 優先度一覧を取得（非同期版）
        /// </summary>
        public async Task<List<IssuePriority>> GetIssuePrioritiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var priorities = await Task.Run(() => _redmineManager.Get<IssuePriority>(), cts.Token);
                return priorities ?? new List<IssuePriority>();
            }
            catch (OperationCanceledException)
            {
                throw new RedmineApiException($"優先度一覧の取得がタイムアウトしました（{_timeoutSeconds}秒）");
            }
            catch (Exception ex)
            {
                throw new RedmineApiException($"優先度一覧の取得に失敗しました: {ex.Message}", ex);
            }
        }

        // 修正: RedmineManagerにはCreateIssueメソッドが存在しないため、Create<T>を使用する
        public async Task<int> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                // RedmineManager.Create<Issue>(issue) を使用
                var createdIssue = await Task.Run(() => _redmineManager.Create<Issue>(issue), cts.Token);
                return createdIssue?.Id ?? 0;
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

                // 修正: RedmineManager.Update<T> を使用
                await Task.Run(() => _redmineManager.Update<Issue>(issue.Id.ToString(), issue), cts.Token);
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

                // 修正: RedmineManager.Delete<T> を使用
                await Task.Run(() => _redmineManager.Delete<Issue>(issueId.ToString()), cts.Token);
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
