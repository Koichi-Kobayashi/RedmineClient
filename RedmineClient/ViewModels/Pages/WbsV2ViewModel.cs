using System;
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
        public double DayWidth { get => _dayWidth; set { _dayWidth = value; OnPropertyChanged(); } }

        private DateTime _viewStart = DateTime.Today;
        public DateTime ViewStart { get => _viewStart; set { _viewStart = value; OnPropertyChanged(); } }

        private bool _showScheduleColumns = true;
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
        }

        /// <summary>
        /// 先行関係を設定し、Redmineにも作成
        /// </summary>
        public async Task SetPredecessorAsync(WbsSampleTask source, WbsSampleTask target)
        {
            if (source == null || target == null) return;
            if (ReferenceEquals(source, target)) return;

            // UIモデル更新（単純に先頭に1つだけ表示）
            var link = new DependencyLink { PredId = source.WbsNo, LagDays = 0, Type = LinkType.FS };
            target.Preds.Clear();
            target.Preds.Add(link);
            OnPropertyChanged(nameof(Tasks));

            // Redmineの依存関係を作成（precedes）
            if (!int.TryParse(source.WbsNo, out var predId)) return;
            if (!int.TryParse(target.WbsNo, out var succId)) return;
            if (string.IsNullOrEmpty(AppConfig.RedmineHost) || string.IsNullOrEmpty(AppConfig.ApiKey)) return;

            using var svc = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
            await svc.CreateIssueRelationAsync(predId, succId, Redmine.Net.Api.Types.IssueRelationType.Precedes);
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
                var issues = await svc.GetIssuesAsync(project.Id, limit: 1000, offset: 0);

                // 期間が設定されたIssueのみ対象
                var dated = issues.Where(i => i.StartDate.HasValue && i.DueDate.HasValue)
                                  .OrderBy(i => i.StartDate!.Value)
                                  .ToList();
                if (dated.Count == 0)
                {
                    Tasks.Clear();
                    OnPropertyChanged(nameof(Tasks));
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

                Recalculate();
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}



