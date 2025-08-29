using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace RedmineClient.Models
{
    public class StatusItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private bool _isDefault;
        private bool _isClosed;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        public bool IsClosed
        {
            get => _isClosed;
            set => SetProperty(ref _isClosed, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Redmine.Net.Api.Types.IssueStatusからの変換コンストラクタ
        public StatusItem(Redmine.Net.Api.Types.IssueStatus status)
        {
            Id = status.Id;
            Name = status.Name ?? string.Empty;
            IsDefault = status.IsDefault;
            IsClosed = status.IsClosed;
        }

        // デフォルトコンストラクタ
        public StatusItem()
        {
        }

        // 等価性の比較（IDベース）
        public override bool Equals(object? obj)
        {
            if (obj is StatusItem other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
