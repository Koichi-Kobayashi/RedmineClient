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
        private int? _redmineIssueId = null;
        private string _redmineUrl = string.Empty;
        private int? _redmineProjectId = null;
        private string _redmineProjectName = string.Empty;
        private string _redmineTracker = string.Empty;
        private string _redmineAuthor = string.Empty;
        private DateTime _redmineCreatedOn = DateTime.MinValue;
        private DateTime _redmineUpdatedOn = DateTime.MinValue;
        private bool _isNew = false;
        private bool _hasUnsavedChanges = false;

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
            set 
            {
                if (SetProperty(ref _startDate, value))
                {
                    // 日付変更イベントを発火
                    DateChanged?.Invoke(this, new DateChangedEventArgs
                    {
                        OldStartDate = _startDate,
                        OldEndDate = _endDate,
                        NewStartDate = value,
                        NewEndDate = _endDate
                    });
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set 
            {
                if (SetProperty(ref _endDate, value))
                {
                    // 日付変更イベントを発火
                    DateChanged?.Invoke(this, new DateChangedEventArgs
                    {
                        OldStartDate = _startDate,
                        OldEndDate = _endDate,
                        NewStartDate = _startDate,
                        NewEndDate = value
                    });
                }
            }
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, Math.Max(0, Math.Min(100, value)));
        }

        public string Status
        {
            get => _status;
            set 
            {
                if (SetProperty(ref _status, value))
                {
                    // ステータス変更イベントを発火
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs
                    {
                        OldStatus = _status,
                        NewStatus = value
                    });
                }
            }
        }

        /// <summary>
        /// ステータス変更イベント
        /// </summary>
        public event EventHandler<StatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// ステータス変更イベントの引数
        /// </summary>
        public class StatusChangedEventArgs : EventArgs
        {
            public string OldStatus { get; set; } = string.Empty;
            public string NewStatus { get; set; } = string.Empty;
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

        public int? RedmineIssueId
        {
            get => _redmineIssueId;
            set => SetProperty(ref _redmineIssueId, value);
        }

        /// <summary>
        /// 日付変更イベント
        /// </summary>
        public event EventHandler<DateChangedEventArgs>? DateChanged;

        /// <summary>
        /// 日付変更イベントの引数
        /// </summary>
        public class DateChangedEventArgs : EventArgs
        {
            public DateTime OldStartDate { get; set; }
            public DateTime OldEndDate { get; set; }
            public DateTime NewStartDate { get; set; }
            public DateTime NewEndDate { get; set; }
        }

        /// <summary>
        /// 開始日を設定し、変更イベントを発火する
        /// </summary>
        /// <param name="value">新しい開始日</param>
        public void SetStartDate(DateTime value)
        {
            var oldStartDate = _startDate;
            var oldEndDate = _endDate;
            
            SetProperty(ref _startDate, value);
            
            // 日付変更イベントを発火
            DateChanged?.Invoke(this, new DateChangedEventArgs
            {
                OldStartDate = oldStartDate,
                OldEndDate = oldEndDate,
                NewStartDate = value,
                NewEndDate = _endDate
            });
        }

        /// <summary>
        /// 終了日を設定し、変更イベントを発火する
        /// </summary>
        /// <param name="value">新しい終了日</param>
        public void SetEndDate(DateTime value)
        {
            var oldStartDate = _startDate;
            var oldEndDate = _endDate;
            
            SetProperty(ref _endDate, value);
            
            // 日付変更イベントを発火
            DateChanged?.Invoke(this, new DateChangedEventArgs
            {
                OldStartDate = oldStartDate,
                OldEndDate = oldEndDate,
                NewStartDate = _startDate,
                NewEndDate = value
            });
        }

        public string RedmineUrl
        {
            get => _redmineUrl;
            set => SetProperty(ref _redmineUrl, value);
        }

        public int? RedmineProjectId
        {
            get => _redmineProjectId;
            set => SetProperty(ref _redmineProjectId, value);
        }

        public string RedmineProjectName
        {
            get => _redmineProjectName;
            set => SetProperty(ref _redmineProjectName, value);
        }

        public string RedmineTracker
        {
            get => _redmineTracker;
            set => SetProperty(ref _redmineTracker, value);
        }

        public string RedmineAuthor
        {
            get => _redmineAuthor;
            set => SetProperty(ref _redmineAuthor, value);
        }

        public DateTime RedmineCreatedOn
        {
            get => _redmineCreatedOn;
            set => SetProperty(ref _redmineCreatedOn, value);
        }

        public DateTime RedmineUpdatedOn
        {
            get => _redmineUpdatedOn;
            set => SetProperty(ref _redmineUpdatedOn, value);
        }

        /// <summary>
        /// 新しく追加されたWBSアイテムかどうか
        /// </summary>
        public bool IsNew
        {
            get => _isNew;
            set => SetProperty(ref _isNew, value);
        }

        /// <summary>
        /// このアイテムが親タスク（子タスクを持てるタスク）かどうか
        /// </summary>
        public bool IsParentTask
        {
            get
            {
                // ルートアイテム（親がない）または第2階層のタスクは親タスク
                if (Parent == null || (Parent != null && Parent.Parent == null))
                    return true;
                
                // 既に子タスクを持っている場合は親タスク
                if (HasChildren)
                    return true;
                
                // プロジェクト名を含む場合は親タスク
                if (Title.Contains("プロジェクト") || Title.Contains("開発") || Title.Contains("設計"))
                    return true;
                
                return false;
            }
        }

        private ObservableCollection<WbsItem> _children = new();
        public ObservableCollection<WbsItem> Children 
        { 
            get => _children;
            set => SetProperty(ref _children, value);
        }
        public WbsItem? Parent { get; set; }

        // 先行・後続の関係性
        private ObservableCollection<WbsItem> _predecessors = new();
        public ObservableCollection<WbsItem> Predecessors
        {
            get => _predecessors;
            set => SetProperty(ref _predecessors, value);
        }

        private ObservableCollection<WbsItem> _successors = new();
        public ObservableCollection<WbsItem> Successors
        {
            get => _successors;
            set => SetProperty(ref _successors, value);
        }

        /// <summary>
        /// 先行タスクがあるかどうか
        /// </summary>
        public bool HasPredecessors => Predecessors.Count > 0;

        /// <summary>
        /// 後続タスクがあるかどうか
        /// </summary>
        public bool HasSuccessors => Successors.Count > 0;

        /// <summary>
        /// 先行タスクの数を取得
        /// </summary>
        public int PredecessorCount => Predecessors.Count;

        /// <summary>
        /// 後続タスクの数を取得
        /// </summary>
        public int SuccessorCount => Successors.Count;

        /// <summary>
        /// 先行タスクの詳細情報を取得（表示用）
        /// </summary>
        public string PredecessorDetails
        {
            get
            {
                if (!HasPredecessors) return string.Empty;
                
                var details = Predecessors.Select(p => $"{p.Title} (ID: {p.Id})");
                return string.Join("\n", details);
            }
        }

        /// <summary>
        /// 後続タスクの詳細情報を取得（表示用）
        /// </summary>
        public string SuccessorDetails
        {
            get
            {
                if (!HasSuccessors) return string.Empty;
                
                var details = Successors.Select(s => $"{s.Title} (ID: {s.Id})");
                return string.Join("\n", details);
            }
        }

        /// <summary>
        /// 先行タスクがある場合の表示用テキスト
        /// </summary>
        public string PredecessorDisplayText
        {
            get
            {
                if (!HasPredecessors) return string.Empty;
                return $"先行: {PredecessorCount}件";
            }
        }

        /// <summary>
        /// 後続タスクがある場合の表示用テキスト
        /// </summary>
        public string SuccessorDisplayText
        {
            get
            {
                if (!HasSuccessors) return string.Empty;
                return $"後続: {SuccessorCount}件";
            }
        }

        /// <summary>
        /// 先行タスクがすべて完了しているかどうか
        /// </summary>
        public bool ArePredecessorsCompleted
        {
            get
            {
                if (!HasPredecessors) return true;
                return Predecessors.All(p => p.Status == "完了");
            }
        }

        /// <summary>
        /// 先行タスクの完了を待っているかどうか
        /// </summary>
        public bool IsWaitingForPredecessors => HasPredecessors && !ArePredecessorsCompleted;

        /// <summary>
        /// 先行タスクのチケット番号を表示用テキストで取得
        /// </summary>
        public string PredecessorTicketNumbers
        {
            get
            {
                if (!HasPredecessors) return string.Empty;
                var ticketNumbers = Predecessors
                    .Where(p => !string.IsNullOrEmpty(p.Id))
                    .Select(p => p.Id)
                    .Distinct();
                return string.Join(", ", ticketNumbers);
            }
        }

        /// <summary>
        /// 先行タスクの詳細情報（ツールチップ用）
        /// </summary>
        public string PredecessorTooltipText
        {
            get
            {
                if (!HasPredecessors) return "先行タスクなし";
                
                var details = Predecessors.Select(p => 
                {
                    var status = p.Status == "完了" ? "✓" : "⏳";
                    return $"{status} {p.Title} (ID: {p.Id}) - {p.Status}";
                });
                return $"先行タスク:\n{string.Join("\n", details)}";
            }
        }

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
            // 重複チェック：同じIDの子アイテムが既に存在する場合は追加しない
            if (Children.Any(existingChild => existingChild.Id == child.Id))
            {
                return;
            }
            
            child.Parent = this;
            Children.Add(child);
            OnPropertyChanged(nameof(HasChildren));
            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(TotalProgress));
            
            // 子アイテムが追加された後、親タスクを展開状態にする
            IsExpanded = true;
        }

        public void RemoveChild(WbsItem child)
        {
            if (Children.Remove(child))
            {
                child.Parent = null;
                OnPropertyChanged(nameof(HasChildren));
                OnPropertyChanged(nameof(Children));
                OnPropertyChanged(nameof(TotalProgress));
            }
        }

        /// <summary>
        /// 先行タスクを追加
        /// </summary>
        /// <param name="predecessor">先行タスク</param>
        /// <returns>依存関係が追加された場合はtrue、既に存在するか循環参照の場合はfalse</returns>
        public bool AddPredecessor(WbsItem predecessor)
        {
            if (predecessor == null || predecessor == this) return false;

            // デバッグログ：既存の先行タスクかどうかをチェック
            bool isAlreadyPredecessor = Predecessors.Contains(predecessor);
            System.Diagnostics.Debug.WriteLine($"AddPredecessor: {Title} -> {predecessor.Title}, Already exists: {isAlreadyPredecessor}");

            // 既存の先行タスクの場合は何もしない
            if (isAlreadyPredecessor)
            {
                System.Diagnostics.Debug.WriteLine($"AddPredecessor: Skipping already existing predecessor {predecessor.Title} for {Title}");
                return false;
            }

            // 循環参照をチェック
            System.Diagnostics.Debug.WriteLine($"AddPredecessor: Checking circular dependency for {Title} -> {predecessor.Title}");
            var cycleInfo = GetCircularDependencyInfo(predecessor);
            if (cycleInfo.HasCycle)
            {
                System.Diagnostics.Debug.WriteLine($"AddPredecessor: Circular dependency detected: {cycleInfo.GetDescription()}");
                throw new InvalidOperationException($"先行タスク「{predecessor.Title}」を追加すると循環参照が発生します。\n{cycleInfo.GetDescription()}");
            }

            System.Diagnostics.Debug.WriteLine($"AddPredecessor: Adding predecessor {predecessor.Title} to {Title}");
            Predecessors.Add(predecessor);
            
            predecessor.AddSuccessor(this);
            
            OnPropertyChanged(nameof(HasPredecessors));
            OnPropertyChanged(nameof(PredecessorCount));
            OnPropertyChanged(nameof(PredecessorDetails));
            OnPropertyChanged(nameof(PredecessorDisplayText));
            
            return true;
        }

        /// <summary>
        /// 先行タスクを削除
        /// </summary>
        /// <param name="predecessor">削除する先行タスク</param>
        public void RemovePredecessor(WbsItem predecessor)
        {
            if (Predecessors.Remove(predecessor))
            {
                predecessor.RemoveSuccessor(this);
                OnPropertyChanged(nameof(HasPredecessors));
                OnPropertyChanged(nameof(PredecessorCount));
                OnPropertyChanged(nameof(PredecessorDetails));
                OnPropertyChanged(nameof(PredecessorDisplayText));
            }
        }

        /// <summary>
        /// 後続タスクを追加
        /// </summary>
        /// <param name="successor">後続タスク</param>
        /// <returns>依存関係が追加された場合はtrue、既に存在するか循環参照の場合はfalse</returns>
        public bool AddSuccessor(WbsItem successor)
        {
            if (successor == null || successor == this) return false;

            // デバッグログ：既存の後続タスクかどうかをチェック
            bool isAlreadySuccessor = Successors.Contains(successor);
            System.Diagnostics.Debug.WriteLine($"AddSuccessor: {Title} -> {successor.Title}, Already exists: {isAlreadySuccessor}");

            // 既存の後続タスクの場合は何もしない
            if (isAlreadySuccessor)
            {
                System.Diagnostics.Debug.WriteLine($"AddSuccessor: Skipping already existing successor {successor.Title} for {Title}");
                return false;
            }

            // 循環参照をチェック
            System.Diagnostics.Debug.WriteLine($"AddSuccessor: Checking circular dependency for {Title} -> {successor.Title}");
            var cycleInfo = GetCircularDependencyInfo(successor);
            if (cycleInfo.HasCycle)
            {
                System.Diagnostics.Debug.WriteLine($"AddSuccessor: Circular dependency detected: {cycleInfo.GetDescription()}");
                throw new InvalidOperationException($"後続タスク「{successor.Title}」を追加すると循環参照が発生します。\n{cycleInfo.GetDescription()}");
            }

            System.Diagnostics.Debug.WriteLine($"AddSuccessor: Adding successor {successor.Title} to {Title}");
            Successors.Add(successor);
            successor.AddPredecessor(this);
            OnPropertyChanged(nameof(HasSuccessors));
            OnPropertyChanged(nameof(SuccessorCount));
            OnPropertyChanged(nameof(SuccessorDetails));
            OnPropertyChanged(nameof(SuccessorDisplayText));
            
            return true;
        }

        /// <summary>
        /// 後続タスクを削除
        /// </summary>
        /// <param name="successor">削除する後続タスク</param>
        public void RemoveSuccessor(WbsItem successor)
        {
            if (Successors.Remove(successor))
            {
                successor.RemovePredecessor(this);
                OnPropertyChanged(nameof(HasSuccessors));
                OnPropertyChanged(nameof(SuccessorCount));
                OnPropertyChanged(nameof(SuccessorDetails));
                OnPropertyChanged(nameof(SuccessorDisplayText));
            }
        }

        /// <summary>
        /// 循環参照が発生するかチェック
        /// </summary>
        /// <param name="newDependency">新しく追加しようとしている依存関係</param>
        /// <returns>循環参照が発生する場合はtrue</returns>
        private bool WouldCreateCycle(WbsItem newDependency)
        {
            if (newDependency == null) return false;
            
            var visited = new HashSet<WbsItem>();
            var recursionStack = new HashSet<WbsItem>();
            
            // 新しく追加しようとしている依存関係から開始して循環参照をチェック
            return HasCycleFromDependency(newDependency, visited, recursionStack);
        }

        /// <summary>
        /// 特定の依存関係から開始して循環参照をチェック（深さ優先探索）
        /// </summary>
        /// <param name="current">現在チェック中のアイテム</param>
        /// <param name="visited">既に訪問済みのアイテム</param>
        /// <param name="recursionStack">現在の再帰スタック</param>
        /// <returns>循環参照が存在する場合はtrue</returns>
        private bool HasCycleFromDependency(WbsItem current, HashSet<WbsItem> visited, HashSet<WbsItem> recursionStack)
        {
            if (recursionStack.Contains(current))
                return true; // 循環参照を検出

            if (visited.Contains(current))
                return false; // 既に訪問済み

            visited.Add(current);
            recursionStack.Add(current);

            try
            {
                // 後続タスクをチェック
                foreach (var successor in current.Successors)
                {
                    if (HasCycleFromDependency(successor, visited, recursionStack))
                        return true;
                }

                // 先行タスクもチェック（双方向の循環参照を検出するため）
                foreach (var predecessor in current.Predecessors)
                {
                    if (HasCycleFromDependency(predecessor, visited, recursionStack))
                        return true;
                }

                return false;
            }
            finally
            {
                recursionStack.Remove(current);
            }
        }

        /// <summary>
        /// 循環参照の存在をチェック（深さ優先探索）
        /// </summary>
        /// <param name="current">現在チェック中のアイテム</param>
        /// <param name="visited">既に訪問済みのアイテム</param>
        /// <returns>循環参照が存在する場合はtrue</returns>
        private bool HasCycle(WbsItem current, HashSet<WbsItem> visited)
        {
            if (visited.Contains(current))
                return true;

            visited.Add(current);

            foreach (var successor in current.Successors)
            {
                if (HasCycle(successor, visited))
                    return true;
            }

            visited.Remove(current);
            return false;
        }

        /// <summary>
        /// 循環参照の詳細情報を取得
        /// </summary>
        /// <param name="newDependency">新しく追加しようとしている依存関係</param>
        /// <returns>循環参照の詳細情報</returns>
        public CircularDependencyInfo GetCircularDependencyInfo(WbsItem newDependency)
        {
            if (newDependency == null) 
                return new CircularDependencyInfo { HasCycle = false, CyclePath = new List<WbsItem>() };

            var visited = new HashSet<WbsItem>();
            var recursionStack = new HashSet<WbsItem>();
            var cyclePath = new List<WbsItem>();
            
            var hasCycle = FindCyclePath(newDependency, visited, recursionStack, cyclePath);
            
            return new CircularDependencyInfo 
            { 
                HasCycle = hasCycle, 
                CyclePath = cyclePath 
            };
        }

        /// <summary>
        /// 循環参照のパスを検索
        /// </summary>
        /// <param name="current">現在チェック中のアイテム</param>
        /// <param name="visited">既に訪問済みのアイテム</param>
        /// <param name="recursionStack">現在の再帰スタック</param>
        /// <param name="cyclePath">循環参照のパス</param>
        /// <returns>循環参照が存在する場合はtrue</returns>
        private bool FindCyclePath(WbsItem current, HashSet<WbsItem> visited, HashSet<WbsItem> recursionStack, List<WbsItem> cyclePath)
        {
            if (recursionStack.Contains(current))
            {
                // 循環参照のパスを構築
                var cycleStartIndex = cyclePath.IndexOf(current);
                if (cycleStartIndex >= 0)
                {
                    cyclePath.RemoveRange(0, cycleStartIndex);
                }
                cyclePath.Add(current);
                return true;
            }

            if (visited.Contains(current))
                return false;

            visited.Add(current);
            recursionStack.Add(current);
            cyclePath.Add(current);

            try
            {
                // 後続タスクをチェック
                foreach (var successor in current.Successors)
                {
                    if (FindCyclePath(successor, visited, recursionStack, cyclePath))
                        return true;
                }

                // 先行タスクもチェック
                foreach (var predecessor in current.Predecessors)
                {
                    if (FindCyclePath(predecessor, visited, recursionStack, cyclePath))
                        return true;
                }

                cyclePath.RemoveAt(cyclePath.Count - 1);
                return false;
            }
            finally
            {
                recursionStack.Remove(current);
            }
        }

        /// <summary>
        /// 循環参照の詳細情報を表すクラス
        /// </summary>
        public class CircularDependencyInfo
        {
            /// <summary>
            /// 循環参照が存在するかどうか
            /// </summary>
            public bool HasCycle { get; set; }
            
            /// <summary>
            /// 循環参照のパス（循環参照が存在する場合）
            /// </summary>
            public List<WbsItem> CyclePath { get; set; } = new List<WbsItem>();
            
            /// <summary>
            /// 循環参照の説明メッセージ
            /// </summary>
            public string GetDescription()
            {
                if (!HasCycle) return "循環参照はありません。";
                
                var pathDescription = string.Join(" → ", CyclePath.Select(item => $"「{item.Title}」"));
                return $"循環参照が検出されました: {pathDescription}";
            }
        }

        public WbsItem Clone()
        {
            var cloned = new WbsItem
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
                RedmineUrl = RedmineUrl,
                RedmineProjectId = RedmineProjectId,
                RedmineProjectName = RedmineProjectName,
                RedmineTracker = RedmineTracker,
                RedmineAuthor = RedmineAuthor,
                RedmineCreatedOn = RedmineCreatedOn,
                RedmineUpdatedOn = RedmineUpdatedOn
            };

            // 先行・後続の関係性はコピーしない（新しいインスタンスなので）
            // 必要に応じて後で設定する

            return cloned;
        }

        /// <summary>
        /// スケジュール表示用の日付リスト（2か月分）
        /// </summary>
        public List<DateTime> ScheduleDates
        {
            get
            {
                var dates = new List<DateTime>();
                var startDate = DateTime.Today;
                var endDate = DateTime.Today.AddMonths(2);
                
                var currentDate = startDate;
                while (currentDate <= endDate)
                {
                    dates.Add(currentDate);
                    currentDate = currentDate.AddDays(1);
                }
                
                return dates;
            }
        }

        /// <summary>
        /// 開始日の日を取得
        /// </summary>
        public int Day => StartDate.Day;

        /// <summary>
        /// 未保存の変更があるかどうか
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        /// <summary>
        /// 依存関係の変更通知を強制的に発火させる
        /// </summary>
        public void NotifyDependencyChanged()
        {
            OnPropertyChanged(nameof(HasPredecessors));
            OnPropertyChanged(nameof(HasSuccessors));
            OnPropertyChanged(nameof(PredecessorCount));
            OnPropertyChanged(nameof(SuccessorCount));
            OnPropertyChanged(nameof(PredecessorDetails));
            OnPropertyChanged(nameof(PredecessorDisplayText));
            OnPropertyChanged(nameof(SuccessorDetails));
            OnPropertyChanged(nameof(SuccessorDisplayText));
        }

        /// <summary>
        /// オブジェクトの等価性を判定
        /// </summary>
        /// <param name="obj">比較対象のオブジェクト</param>
        /// <returns>等しい場合はtrue</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is WbsItem other)
            {
                // IDが同じ場合は同じタスクとみなす
                return Id == other.Id;
            }
            return false;
        }

        /// <summary>
        /// ハッシュコードを取得
        /// </summary>
        /// <returns>ハッシュコード</returns>
        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 等価演算子のオーバーライド
        /// </summary>
        /// <param name="left">左辺</param>
        /// <param name="right">右辺</param>
        /// <returns>等しい場合はtrue</returns>
        public static bool operator ==(WbsItem? left, WbsItem? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// 不等価演算子のオーバーライド
        /// </summary>
        /// <param name="left">左辺</param>
        /// <param name="right">右辺</param>
        /// <returns>等しくない場合はtrue</returns>
        public static bool operator !=(WbsItem? left, WbsItem? right)
        {
            return !(left == right);
        }
    }
}
