using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RedmineClient.Models
{
    public class WbsItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _title = string.Empty;
        private string _description = string.Empty;
        private DateTime _startDate = DateTime.Today;
        private DateTime _endDate = DateTime.Today.AddDays(1);
        private double _progress = 0.0;
        private string _status = "未着手";
        private string _priority = "中";
        private string _assignee = string.Empty;
        private bool _isExpanded = true;
        private bool _isSelected = false;
        private int _redmineIssueId = 0;
        private string _redmineUrl = string.Empty;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, Math.Max(0, Math.Min(100, value)));
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public string Assignee
        {
            get => _assignee;
            set => SetProperty(ref _assignee, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int RedmineIssueId
        {
            get => _redmineIssueId;
            set => SetProperty(ref _redmineIssueId, value);
        }

        public string RedmineUrl
        {
            get => _redmineUrl;
            set => SetProperty(ref _redmineUrl, value);
        }

        public ObservableCollection<WbsItem> Children { get; set; } = new();
        public WbsItem? Parent { get; set; }

        public int Level
        {
            get
            {
                int level = 0;
                var current = Parent;
                while (current != null)
                {
                    level++;
                    current = current.Parent;
                }
                return level;
            }
        }

        public bool HasChildren => Children.Count > 0;

        public double TotalProgress
        {
            get
            {
                if (!HasChildren)
                    return Progress;

                if (Children.Count == 0)
                    return 0;

                double totalProgress = Children.Sum(child => child.TotalProgress);
                return totalProgress / Children.Count;
            }
        }

        public TimeSpan Duration => EndDate - StartDate;

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

        public void AddChild(WbsItem child)
        {
            child.Parent = this;
            Children.Add(child);
            OnPropertyChanged(nameof(HasChildren));
        }

        public void RemoveChild(WbsItem child)
        {
            if (Children.Remove(child))
            {
                child.Parent = null;
                OnPropertyChanged(nameof(HasChildren));
            }
        }

        public WbsItem Clone()
        {
            return new WbsItem
            {
                Id = Id,
                Title = Title,
                Description = Description,
                StartDate = StartDate,
                EndDate = EndDate,
                Progress = Progress,
                Status = Status,
                Priority = Priority,
                Assignee = Assignee,
                RedmineIssueId = RedmineIssueId,
                RedmineUrl = RedmineUrl
            };
        }
    }
}
