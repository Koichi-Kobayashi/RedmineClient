namespace RedmineClient.XmlData
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("issues")]
    public class Issues
    {
        [XmlAttribute("total_count")]
        public int TotalCount { get; set; }

        [XmlAttribute("offset")]
        public int Offset { get; set; }

        [XmlAttribute("limit")]
        public int Limit { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlElement("issue")]
        public List<Issue> IssueList { get; set; }
    }

    public class Issue
    {
        /// <summary>
        /// ID
        /// </summary>
        [XmlElement("id")]
        public int Id { get; set; }

        /// <summary>
        /// プロジェクト
        /// </summary>
        [XmlElement("project")]
        public IssueProject Project { get; set; }

        /// <summary>
        /// トラッカー
        /// </summary>
        [XmlElement("tracker")]
        public IssueTracker Tracker { get; set; }

        /// <summary>
        /// ステータス
        /// </summary>
        [XmlElement("status")]
        public IssueStatus Status { get; set; }

        /// <summary>
        /// 優先度
        /// </summary>
        [XmlElement("priority")]
        public IssuePriority Priority { get; set; }

        /// <summary>
        /// 作成者
        /// </summary>
        [XmlElement("author")]
        public IssueAuthor Author { get; set; }

        /// <summary>
        /// 担当者
        /// </summary>
        [XmlElement("assigned_to")]
        public IssueAssignedTo AssignedTo { get; set; }

        /// <summary>
        /// 題名
        /// </summary>
        [XmlElement("subject")]
        public string Subject { get; set; }

        /// <summary>
        /// 説明
        /// </summary>
        [XmlElement("description")]
        public string Description { get; set; }

        /// <summary>
        /// 開始日
        /// </summary>
        [XmlElement("start_date")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 期日
        /// </summary>
        [XmlElement("due_date")]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// 進捗率
        /// </summary>
        [XmlElement("done_ratio")]
        public int DoneRatio { get; set; }

        /// <summary>
        /// プライベートかどうか
        /// </summary>
        [XmlElement("is_private")]
        public bool IsPrivate { get; set; }

        /// <summary>
        /// 予定工数
        /// </summary>
        [XmlElement("estimated_hours")]
        public double EstimatedHours { get; set; }

        /// <summary>
        /// 予定工数合計
        /// </summary>
        [XmlElement("total_estimated_hours")]
        public double TotalEstimatedHours { get; set; }

        /// <summary>
        /// 作業時間
        /// </summary>
        [XmlElement("spent_hours")]
        public double SpentHours { get; set; }

        /// <summary>
        /// 作業時間合計
        /// </summary>
        [XmlElement("total_spent_hours")]
        public double TotalSpentHours { get; set; }

        /// <summary>
        /// カスタムフィールド
        /// </summary>
        [XmlArray("custom_fields")]
        [XmlArrayItem("custom_field")]
        public List<IssueCustomField> CustomFields { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        [XmlElement("created_on")]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        [XmlElement("updated_on")]
        public DateTime UpdatedOn { get; set; }

        /// <summary>
        /// 終了日時
        /// </summary>
        [XmlElement("closed_on")]
        public DateTime? ClosedOn { get; set; } // Nullable DateTime for optional element
    }

    /// <summary>
    /// 
    /// </summary>
    public class IssueProject
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class IssueTracker
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class IssueStatus
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }


        [XmlAttribute("is_closed")]
        public bool IsClosed { get; set; }
    }

    public class IssuePriority
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class IssueAuthor
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class IssueAssignedTo
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }
    }

    public class IssueCustomField
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }
    }
}
