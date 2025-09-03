using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        public List<DependencyLink> Preds { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


