using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RedmineClient.Algorithms;
using RedmineClient.Models;
using RedmineClient.Services;
using Redmine.Net.Api.Types;

namespace RedmineClient.ViewModels.Pages
{
    public class WbsV2ViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<WbsSampleTask> Tasks { get; } = new();
        public ObservableCollection<Project> AvailableProjects { get; } = new();

        private double _dayWidth = 30.0;
        public double DayWidth { get => _dayWidth; set { _dayWidth = value; OnPropertyChanged(); UpdateTimelineSize(); } }

        private DateTime _viewStart = DateTime.Today;
        public DateTime ViewStart { get => _viewStart; set { _viewStart = value; OnPropertyChanged(); } }

        private double _timelineWidth = 2000.0;
        public double TimelineWidth { get => _timelineWidth; set { _timelineWidth = value; OnPropertyChanged(); } }

        private double _timelineHeight = 1000.0;
        public double TimelineHeight { get => _timelineHeight; set { _timelineHeight = value; OnPropertyChanged(); } }

        private bool _showScheduleColumns = false;
        public bool ShowScheduleColumns { get => _showScheduleColumns; set { _showScheduleColumns = value; OnPropertyChanged(); } }

        private Project? _selectedProject;
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (!Equals(_selectedProject, value))
                {
                    _selectedProject = value;
                    OnPropertyChanged();
                    if (value != null)
                    {
                        _ = LoadRedmineDataAsync(value);
                    }
                }
            }
        }

        public WbsV2ViewModel()
        {
            _ = LoadProjectsAsync();
        }

        private void ReindexRows()
        {
            int idx = 0;
            foreach (var t in Tasks) t.RowIndex = idx++;
        }

        public void Recalculate()
        {
            var order = TopologicalSort.Run(Tasks.Select(t => t.WbsNo), edge: (u, v) =>
                Tasks.Any(x => x.WbsNo == v && x.Preds.Any(p => p.PredId == u)));

            var res = Cpm.Run(Tasks, order);
            foreach (var t in Tasks)
            {
                t.ES = res.ES[t.WbsNo];
                t.EF = res.EF[t.WbsNo];
                t.LS = res.LS[t.WbsNo];
                t.LF = res.LF[t.WbsNo];
                t.Slack = t.LS - t.ES;
                t.IsCritical = t.Slack == 0;
            }

            OnPropertyChanged(nameof(Tasks));
            UpdateTimelineSize();
        }

        /// <summary>
        /// predecessorId -> successorId の辺を追加すると循環が発生するか判定
        /// </summary>
        private bool WouldCreateCycle(string predecessorId, string successorId)
        {
            if (string.IsNullOrWhiteSpace(predecessorId) || string.IsNullOrWhiteSpace(successorId)) return true;
            if (predecessorId == successorId) return true;

            // 後続マップを構築: 各ノードから到達できる後続ノード一覧
            var successorMap = new Dictionary<string, List<string>>();
            foreach (var t in Tasks)
            {
                if (!successorMap.ContainsKey(t.WbsNo)) successorMap[t.WbsNo] = new List<string>();
            }
            foreach (var t in Tasks)
            {
                foreach (var p in t.Preds)
                {
                    if (!successorMap.TryGetValue(p.PredId, out var list))
                    {
                        list = new List<string>();
                        successorMap[p.PredId] = list;
                    }
                    if (!list.Contains(t.WbsNo)) list.Add(t.WbsNo);
                }
            }

            // 追加したい辺: predecessorId -> successorId
            // もし既に successorId から predecessorId へ辿れるなら、辺追加で閉路ができる
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(successorId);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                if (!visited.Add(cur)) continue;
                if (cur == predecessorId) return true;
                if (successorMap.TryGetValue(cur, out var succs))
                {
                    foreach (var nxt in succs)
                    {
                        stack.Push(nxt);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// UIからの事前チェック用: この設定が可能か（循環しないか）
        /// </summary>
        public bool CanSetPredecessor(WbsSampleTask source, WbsSampleTask target)
        {
            if (source == null || target == null) return false;
            if (ReferenceEquals(source, target)) return false;
            // 仕様: target を先行、source を後続にする
            return !WouldCreateCycle(target.WbsNo, source.WbsNo);
        }

        /// <summary>
        /// 先行関係を設定し、Redmineにも作成
        /// </summary>
        public async Task SetPredecessorAsync(WbsSampleTask source, WbsSampleTask target)
        {
            if (source == null || target == null) return;
            if (ReferenceEquals(source, target)) return;
            if (WouldCreateCycle(target.WbsNo, source.WbsNo))
            {
                // 循環する場合は何もしない
                return;
            }

            // 意図: 「ドロップ元(source)」の先行に「ドロップ先(target)」を追加
            var link = new DependencyLink { PredId = target.WbsNo, LagDays = 0, Type = LinkType.FS };

            // 現在の先行関係をバックアップ（失敗時にロールバックするため）
            var backup = source.Preds.ToList();

            try
            {
                // 既に同じ先行があれば追加しない（重複回避）
                if (!source.Preds.Any(p => p.PredId == link.PredId))
                {
                    source.Preds.Add(link);
                }
                OnPropertyChanged(nameof(Tasks));

                // Redmineの依存関係を作成（target precedes source）
                if (!int.TryParse(target.WbsNo, out var predId)) return;   // 先行
                if (!int.TryParse(source.WbsNo, out var succId)) return;   // 後続
                if (string.IsNullOrEmpty(AppConfig.RedmineHost) || string.IsNullOrEmpty(AppConfig.ApiKey)) return;

                using var svc = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
                await svc.CreateIssueRelationAsync(predId, succId, Redmine.Net.Api.Types.IssueRelationType.Precedes);
            }
            catch (Exception ex)
            {
                // サーバー同期に失敗してもUI上の設定は保持し、ユーザーに通知する
                throw new RedmineClient.Services.RedmineApiException(
                    "先行タスクの設定に失敗しました。ネットワーク、APIキー、権限、Redmineの状態を確認してください。",
                    ex);
            }
        }

        public void ApplyStartConstraint(WbsSampleTask task, int newEs)
        {
            task.StartMin = newEs < 0 ? 0 : newEs;
            Recalculate();
        }

        public async Task LoadProjectsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(AppConfig.RedmineHost) || string.IsNullOrEmpty(AppConfig.ApiKey)) return;
                using var svc = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
                var projects = await svc.GetProjectsAsync();
                AvailableProjects.Clear();
                foreach (var p in projects.OrderBy(p => p.Name)) AvailableProjects.Add(p);
            }
            catch
            {
                // ignore
            }
        }

        public async Task LoadRedmineDataAsync(Project project)
        {
            try
            {
                if (project == null) return;
                if (string.IsNullOrEmpty(AppConfig.RedmineHost) || string.IsNullOrEmpty(AppConfig.ApiKey)) return;

                using var svc = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
                // V1同等: サーバー側の取得時点でID昇順を指定
                var issues = await svc.GetIssuesAsync(project.Id, limit: 1000, offset: 0, sort: "id:asc");

                // 期間が設定されたIssueのみ対象
                var dated = issues.Where(i => i.StartDate.HasValue && i.DueDate.HasValue)
                                  // サーバー側でid:asc指定済みだが、念のためクライアントでも安定化
                                  .OrderBy(i => i.Id)
                                  .ToList();
                if (dated.Count == 0)
                {
                    Tasks.Clear();
                    OnPropertyChanged(nameof(Tasks));
                    UpdateTimelineSize();
                    return;
                }

                var earliest = dated.Min(i => i.StartDate)!.Value.Date;
                // 表示開始は月初に丸め
                var viewStart = new DateTime(earliest.Year, earliest.Month, 1);
                ViewStart = viewStart;

                Tasks.Clear();
                int row = 0;
                foreach (var i in dated)
                {
                    var sd = i.StartDate!.Value.Date;
                    var dd = i.DueDate!.Value.Date;
                    if (dd < sd) dd = sd;
                    var duration = (int)(dd - sd).TotalDays + 1;
                    var es = (int)(sd - ViewStart).TotalDays;

                    var t = new WbsSampleTask
                    {
                        WbsNo = i.Id.ToString(),
                        Name = i.Subject ?? $"Issue {i.Id}",
                        Level = 0,
                        Duration = duration,
                        RowIndex = row++,
                        BaseDate = ViewStart,
                        StartDate = sd,
                        DueDate = dd,
                    };
                    t.StartMin = es < 0 ? 0 : es;
                    Tasks.Add(t);
                }

                // 既存のRedmineリレーションから先行タスクを復元
                try
                {
                    BuildPredecessorsFromRelations(dated);
                }
                catch (Exception)
                {
                    // 関係復元に失敗しても処理継続
                }

                Recalculate();
                UpdateTimelineSize();
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Redmineのチケット日付（開始/期限）を更新
        /// </summary>
        public async Task UpdateIssueDatesAsync(WbsSampleTask task)
        {
            if (task == null) return;
            if (!int.TryParse(task.WbsNo, out var issueId)) return;
            if (string.IsNullOrEmpty(AppConfig.RedmineHost) || string.IsNullOrEmpty(AppConfig.ApiKey)) return;

            using var svc = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
            var issue = await svc.GetIssueAsync(issueId);
            if (issue == null) return;

            // ES/Duration から日付を逆算（StartDate/DueDate優先があればそれを使う）
            var start = task.StartDate ?? task.BaseDate.AddDays(task.ES);
            var due = task.DueDate ?? start.AddDays(Math.Max(1, task.Duration) - 1);

            issue.StartDate = start;
            issue.DueDate = due;

            await svc.UpdateIssueAsync(issue);
        }

        private void UpdateTimelineSize()
        {
            // タイムラインのサイズを更新（タスク数と日数に基づく）
            if (Tasks.Count > 0)
            {
                TimelineHeight = Math.Max(1000, Tasks.Count * 28 + 100); // タスク数 * 行高(28) + 余白
                var maxEndDate = Tasks.Max(t => t.ES + t.Duration);
                TimelineWidth = Math.Max(2000, maxEndDate * DayWidth + 200); // 最大終了日 * 日幅 + 余白
            }
            else
            {
                // no-op
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Redmine Issue の relations から V2 タスクの先行関係を復元
        /// </summary>
        private void BuildPredecessorsFromRelations(List<Issue> issues)
        {
            if (issues == null || issues.Count == 0 || Tasks.Count == 0) return;

            // ID -> Task の辞書
            var taskById = new Dictionary<int, WbsSampleTask>();
            foreach (var t in Tasks)
            {
                if (int.TryParse(t.WbsNo, out var id))
                {
                    taskById[id] = t;
                }
            }

            // 各Issueの関係を読み取り、先行タスクIDをタスクに設定
            foreach (var issue in issues)
            {
                var relations = TryGetRelations(issue);
                if (relations == null) continue;

                foreach (var (issueToId, relationType) in relations)
                {
                    var type = (relationType ?? string.Empty).ToLowerInvariant();

                    // precedes/blocks: 現在のIssueが関連Issueの先行
                    if (type == "precedes" || type == "blocks")
                    {
                        var predecessorId = issue.Id;
                        var successorId = issueToId;

                        if (taskById.TryGetValue(successorId, out var succTask))
                        {
                            var predIdStr = predecessorId.ToString();
                            if (!succTask.Preds.Any(p => p.PredId == predIdStr))
                            {
                                succTask.Preds.Add(new DependencyLink { PredId = predIdStr, LagDays = 0, Type = LinkType.FS });
                            }
                        }
                    }
                    // follows/blocked_by: 関連Issueが現在のIssueの先行
                    else if (type == "follows" || type == "blocked_by")
                    {
                        var predecessorId = issueToId;
                        var successorId = issue.Id;

                        if (taskById.TryGetValue(successorId, out var succTask))
                        {
                            var predIdStr = predecessorId.ToString();
                            if (!succTask.Preds.Any(p => p.PredId == predIdStr))
                            {
                                succTask.Preds.Add(new DependencyLink { PredId = predIdStr, LagDays = 0, Type = LinkType.FS });
                            }
                        }
                    }
                }
            }

            // 先行IDの表示更新
            OnPropertyChanged(nameof(Tasks));
        }

        /// <summary>
        /// Issueから relation の (IssueToId, Type) の列挙を安全に取得
        /// </summary>
        private IEnumerable<(int IssueToId, string Type)>? TryGetRelations(Issue issue)
        {
            var results = new List<(int IssueToId, string Type)>();
            try
            {
                var propertyNames = new[] { "Relations", "relations", "IssueRelations", "issue_relations" };
                foreach (var name in propertyNames)
                {
                    var prop = issue.GetType().GetProperty(name);
                    if (prop == null) continue;

                    var value = prop.GetValue(issue);
                    if (value is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var rel in enumerable)
                        {
                            if (rel == null) continue;
                            // redmine-net-api の IssueRelation 型を優先
                            if (rel is Redmine.Net.Api.Types.IssueRelation ir)
                            {
                                results.Add((ir.IssueToId, ir.Type.ToString()));
                            }
                            else
                            {
                                // リフレクションで IssueToId / Type を読む
                                var toProp = rel.GetType().GetProperty("IssueToId") ?? rel.GetType().GetProperty("issue_to_id");
                                var typeProp = rel.GetType().GetProperty("Type") ?? rel.GetType().GetProperty("relation_type");
                                if (toProp != null && typeProp != null)
                                {
                                    var toVal = toProp.GetValue(rel);
                                    var typeVal = typeProp.GetValue(rel);
                                    if (toVal is int tid && typeVal != null)
                                    {
                                        results.Add((tid, typeVal.ToString() ?? string.Empty));
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
            catch
            {
                // 読み取り失敗は無視
            }
            return results;
        }
    }
}



