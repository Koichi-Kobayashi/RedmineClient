using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RedmineClient.Models
{
    public class WbsSampleTask : INotifyPropertyChanged
    {
        public string WbsNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Duration { get; set; }

        public List<DependencyLink> Preds { get; } = new();

        public int? StartMin { get; set; }

        private int _es; public int ES { get => _es; set { _es = value; OnPropertyChanged(); } }
        private int _ef; public int EF { get => _ef; set { _ef = value; OnPropertyChanged(); } }
        private int _ls; public int LS { get => _ls; set { _ls = value; OnPropertyChanged(); } }
        private int _lf; public int LF { get => _lf; set { _lf = value; OnPropertyChanged(); } }
        private int _slack; public int Slack { get => _slack; set { _slack = value; OnPropertyChanged(); } }

        private bool _isCritical; public bool IsCritical { get => _isCritical; set { _isCritical = value; OnPropertyChanged(); } }

        public int RowIndex { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}


