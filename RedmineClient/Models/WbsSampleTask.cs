using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace RedmineClient.Models
{
    public class WbsSampleTask : INotifyPropertyChanged
    {
        private string _wbsNo = string.Empty;
        public string WbsNo { get => _wbsNo; set { _wbsNo = value; OnPropertyChanged(); } }

        private string _name = string.Empty;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private int _level;
        public int Level { get => _level; set { _level = value; OnPropertyChanged(); } }

        private int _duration = 1;
        public int Duration { get => _duration; set { _duration = Math.Max(1, value); OnPropertyChanged(); } }

        private int _rowIndex;
        public int RowIndex { get => _rowIndex; set { _rowIndex = value; OnPropertyChanged(); } }

        private int _es;
        public int ES { get => _es; set { _es = value; OnPropertyChanged(); } }

        private int _ef;
        public int EF { get => _ef; set { _ef = value; OnPropertyChanged(); } }

        private int _ls;
        public int LS { get => _ls; set { _ls = value; OnPropertyChanged(); } }

        private int _lf;
        public int LF { get => _lf; set { _lf = value; OnPropertyChanged(); } }

        private int _slack;
        public int Slack { get => _slack; set { _slack = value; OnPropertyChanged(); } }

        private bool _isCritical;
        public bool IsCritical { get => _isCritical; set { _isCritical = value; OnPropertyChanged(); } }

        private int _startMin;
        public int StartMin { get => _startMin; set { _startMin = Math.Max(0, value); OnPropertyChanged(); } }

        private string _priority = "中";
        public string Priority { get => _priority; set { _priority = value; OnPropertyChanged(); } }

        private readonly ObservableCollection<DependencyLink> _preds = new();
        public ObservableCollection<DependencyLink> Preds => _preds;

        // 表示用: 先行タスクIDをカンマ区切りで返す（空なら空文字）
        public string PredecessorIds => string.Join(",", _preds.Select(p => p.PredId));

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        // V2: Redmineと同期するための日付プロパティ
        private DateTime _baseDate = DateTime.Today;
        public DateTime BaseDate { get => _baseDate; set { _baseDate = value; OnPropertyChanged(); } }

        private DateTime? _startDate;
        public DateTime? StartDate { get => _startDate; set { _startDate = value; OnPropertyChanged(); } }

        private DateTime? _dueDate;
        public DateTime? DueDate { get => _dueDate; set { _dueDate = value; OnPropertyChanged(); } }

        public WbsSampleTask()
        {
            _preds.CollectionChanged += (_, __) => OnPropertyChanged(nameof(PredecessorIds));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}



