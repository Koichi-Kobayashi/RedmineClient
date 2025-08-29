using Redmine.Net.Api.Types;

namespace RedmineClient.Models
{
    /// <summary>
    /// 階層構造を持つIssueクラス
    /// Redmine.Net.Api.Types.Issueを包含してChildrenプロパティを追加
    /// </summary>
    public class HierarchicalIssue
    {

        /// <summary>
        /// 元のIssueオブジェクト
        /// </summary>
        public Issue Issue { get; set; }

        /// <summary>
        /// 子チケットのリスト
        /// </summary>
        public List<HierarchicalIssue> Children { get; set; } = new List<HierarchicalIssue>();

        // Issueのプロパティにアクセスするためのプロパティ
        public int Id => Issue.Id;
        public string? Subject => Issue.Subject;
        public string? Description => Issue.Description;
        public IdentifiableName? Status => Issue.Status;
        public IdentifiableName? Priority => Issue.Priority;
        public IdentifiableName? Author => Issue.Author;
        public IdentifiableName? AssignedTo => Issue.AssignedTo;
        public IdentifiableName? Project => Issue.Project;
        public IdentifiableName? Tracker => Issue.Tracker;
        public DateTime? StartDate => Issue.StartDate;
        public DateTime? DueDate => Issue.DueDate;
        public float? DoneRatio => Issue.DoneRatio;
        public float? EstimatedHours => Issue.EstimatedHours;
        public DateTime? CreatedOn => Issue.CreatedOn;
        public DateTime? UpdatedOn => Issue.UpdatedOn;

        /// <summary>
        /// 親チケットのID（リフレクションで取得）
        /// </summary>
        public int? ParentId
        {
            get
            {
                try
                {
                    var parentIdProperty = Issue.GetType().GetProperty("ParentId");
                    if (parentIdProperty != null)
                    {
                        var value = parentIdProperty.GetValue(Issue);
                        if (value is int intValue)
                            return intValue;
                        if (value is int nullableIntValue)
                            return nullableIntValue;
                        if (value is int?)
                            return (int?)value;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="issue">元のIssueオブジェクト</param>
        public HierarchicalIssue(Issue issue)
        {
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));
        }

        /// <summary>
        /// 親チケットへの参照
        /// </summary>
        public HierarchicalIssue? Parent { get; set; }

        /// <summary>
        /// 子チケットを持っているかどうか
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// 親チケットを持っているかどうか
        /// </summary>
        public bool HasParent => Parent != null;

        /// <summary>
        /// 階層の深さ（ルートが0）
        /// </summary>
        public int Depth
        {
            get
            {
                var depth = 0;
                var current = Parent;
                while (current != null)
                {
                    depth++;
                    current = current.Parent;
                }
                return depth;
            }
        }

        /// <summary>
        /// 子チケットを追加
        /// </summary>
        /// <param name="child">追加する子チケット</param>
        public void AddChild(HierarchicalIssue child)
        {
            if (child != null)
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        /// <summary>
        /// 子チケットを削除
        /// </summary>
        /// <param name="child">削除する子チケット</param>
        /// <returns>削除に成功した場合はtrue</returns>
        public bool RemoveChild(HierarchicalIssue child)
        {
            if (child != null)
            {
                child.Parent = null;
                return Children.Remove(child);
            }
            return false;
        }

        /// <summary>
        /// すべての子チケットを再帰的に取得
        /// </summary>
        /// <returns>平坦化された子チケットのリスト</returns>
        public List<HierarchicalIssue> GetAllChildren()
        {
            var allChildren = new List<HierarchicalIssue>();
            foreach (var child in Children)
            {
                allChildren.Add(child);
                allChildren.AddRange(child.GetAllChildren());
            }
            return allChildren;
        }

        /// <summary>
        /// ルートチケットを取得
        /// </summary>
        /// <returns>ルートチケット</returns>
        public HierarchicalIssue GetRoot()
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }

        /// <summary>
        /// 指定された深さの子チケットのみを取得
        /// </summary>
        /// <param name="maxDepth">最大深さ</param>
        /// <returns>指定された深さまでの子チケットのリスト</returns>
        public List<HierarchicalIssue> GetChildrenUpToDepth(int maxDepth)
        {
            var result = new List<HierarchicalIssue>();
            if (maxDepth <= 0) return result;

            foreach (var child in Children)
            {
                result.Add(child);
                if (maxDepth > 1)
                {
                    result.AddRange(child.GetChildrenUpToDepth(maxDepth - 1));
                }
            }
            return result;
        }
    }
}
