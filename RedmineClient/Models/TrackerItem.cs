using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace RedmineClient.Models
{
    public class TrackerItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private string _description = string.Empty;

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

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
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

        // Redmine.Net.Api.Types.Trackerからの変換コンストラクタ
        public TrackerItem(Redmine.Net.Api.Types.Tracker tracker)
        {
            Id = tracker.Id;
            Name = tracker.Name ?? string.Empty;
            Description = tracker.Description ?? string.Empty;
        }

        // デフォルトコンストラクタ
        public TrackerItem()
        {
        }

        // 等価性の比較（IDベース）
        public override bool Equals(object? obj)
        {
            if (obj is TrackerItem other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(TrackerItem? left, TrackerItem? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(TrackerItem? left, TrackerItem? right)
        {
            return !(left == right);
        }

        // 文字列表現
        public override string ToString()
        {
            return $"{Name} (ID: {Id})";
        }
    }
}
