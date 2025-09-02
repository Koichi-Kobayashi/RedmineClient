using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using RedmineClient.Algorithms;
using RedmineClient.Models;

namespace RedmineClient.ViewModels.Pages
{
    public class WbsSampleViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<WbsSampleTask> Tasks { get; } = new();

        private double _dayWidth = 30.0;
        public double DayWidth { get => _dayWidth; set { _dayWidth = value; OnPropertyChanged(); } }

        private DateTime _viewStart = DateTime.Today;
        public DateTime ViewStart { get => _viewStart; set { _viewStart = value; OnPropertyChanged(); } }

        private bool _showScheduleColumns = true;
        public bool ShowScheduleColumns { get => _showScheduleColumns; set { _showScheduleColumns = value; OnPropertyChanged(); } }

        public WbsSampleViewModel()
        {
            Tasks.Add(new WbsSampleTask { WbsNo = "1",   Level = 0, Name = "企画", Duration = 3 });
            var t = new WbsSampleTask { WbsNo = "1.1", Level = 1, Name = "要件定義", Duration = 5 }; t.Preds.Add(new DependencyLink{ PredId="1", Type=LinkType.FS }); Tasks.Add(t);
            t = new WbsSampleTask { WbsNo = "1.2", Level = 1, Name = "基本設計", Duration = 7 }; t.Preds.Add(new DependencyLink{ PredId="1.1", Type=LinkType.FS }); Tasks.Add(t);
            t = new WbsSampleTask { WbsNo = "2",   Level = 0, Name = "実装", Duration = 10 }; t.Preds.Add(new DependencyLink{ PredId="1.2", Type=LinkType.SS }); Tasks.Add(t);
            t = new WbsSampleTask { WbsNo = "2.1", Level = 1, Name = "フロント実装", Duration = 6 }; t.Preds.Add(new DependencyLink{ PredId="2", Type=LinkType.FS }); Tasks.Add(t);
            t = new WbsSampleTask { WbsNo = "2.2", Level = 1, Name = "バックエンド実装", Duration = 8 }; t.Preds.Add(new DependencyLink{ PredId="2", Type=LinkType.FS }); Tasks.Add(t);
            t = new WbsSampleTask { WbsNo = "3",   Level = 0, Name = "総合テスト", Duration = 5 }; t.Preds.Add(new DependencyLink{ PredId="2.1", Type=LinkType.FF }); t.Preds.Add(new DependencyLink{ PredId="2.2", Type=LinkType.FF }); Tasks.Add(t);

            ReindexRows();
            Recalculate();
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

        public void ApplyStartConstraint(WbsSampleTask task, int newEs)
        {
            task.StartMin = newEs < 0 ? 0 : newEs;
            Recalculate();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


