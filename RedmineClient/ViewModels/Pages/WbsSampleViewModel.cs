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

        private double _dayWidth = 18.0;
        public double DayWidth { get => _dayWidth; set { _dayWidth = value; OnPropertyChanged(); } }

        private DateTime _viewStart = DateTime.Today;
        public DateTime ViewStart { get => _viewStart; set { _viewStart = value; OnPropertyChanged(); } }

        public WbsSampleViewModel()
        {
            Tasks.Add(new WbsSampleTask { WbsNo = "1",   Level = 0, Name = "企画", Duration = 3 });
            Tasks.Add(new WbsSampleTask { WbsNo = "1.1", Level = 1, Name = "要件定義", Duration = 5, Preds = { ("1", 0) } });
            Tasks.Add(new WbsSampleTask { WbsNo = "1.2", Level = 1, Name = "基本設計", Duration = 7, Preds = { ("1.1", 0) } });
            Tasks.Add(new WbsSampleTask { WbsNo = "2",   Level = 0, Name = "実装", Duration = 10, Preds = { ("1.2", 0) } });
            Tasks.Add(new WbsSampleTask { WbsNo = "2.1", Level = 1, Name = "フロント実装", Duration = 6, Preds = { ("2", 0) } });
            Tasks.Add(new WbsSampleTask { WbsNo = "2.2", Level = 1, Name = "バックエンド実装", Duration = 8, Preds = { ("2", 0) } });
            Tasks.Add(new WbsSampleTask { WbsNo = "3",   Level = 0, Name = "総合テスト", Duration = 5, Preds = { ("2.1", 0), ("2.2", 0) } });

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
                Tasks.Any(x => x.WbsNo == v && x.Preds.Any(p => p.predId == u)));

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


