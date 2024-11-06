namespace RedmineClient.XmlData
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("issues")]
    public class Issues
    {
        [XmlElement("issue")]
        public List<Issue> IssueList { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("count")]
        public int Count { get; set; }
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
        public Tracker Tracker { get; set; }

        /// <summary>
        /// ステータス
        /// </summary>
        [XmlElement("status")]
        public Status Status { get; set; }

        /// <summary>
        /// 優先度
        /// </summary>
        [XmlElement("priority")]
        public Priority Priority { get; set; }

        /// <summary>
        /// 作成者
        /// </summary>
        [XmlElement("author")]
        public Author Author { get; set; }

        /// <summary>
        /// カテゴリー
        /// </summary>
        [XmlElement("category")]
        public Category Category { get; set; }

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
        public string DueDate { get; set; }

        /// <summary>
        /// 進捗率
        /// </summary>
        [XmlElement("done_ratio")]
        public int DoneRatio { get; set; }

        /// <summary>
        /// 予定工数
        /// </summary>
        [XmlElement("estimated_hours")]
        public string EstimatedHours { get; set; }

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
    }

    /// <summary>
    /// 
    /// </summary>
    public class IssueProject
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }
    }

    public class Tracker
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }
    }

    public class Status
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }
    }

    public class Priority
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }
    }

    public class Author
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }
    }

    public class Category
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }
    }

    public class IssueCustomField
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }
    }
}
