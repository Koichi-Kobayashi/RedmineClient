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
        [RedmineHeader("ID")]
        [XmlElement("id")]
        public int Id { get; set; }

        [RedmineHeader("プロジェクト")]
        [XmlElement("project")]
        public IssueProject Project { get; set; }

        [RedmineHeader("トラッカー")]
        [XmlElement("tracker")]
        public Tracker Tracker { get; set; }

        [RedmineHeader("ステータス")]
        [XmlElement("status")]
        public Status Status { get; set; }

        [RedmineHeader("優先度")]
        [XmlElement("priority")]
        public Priority Priority { get; set; }

        [RedmineHeader("作成者")]
        [XmlElement("author")]
        public Author Author { get; set; }

        [RedmineHeader("カテゴリー")]
        [XmlElement("category")]
        public Category Category { get; set; }

        [RedmineHeader("題名")]
        [XmlElement("subject")]
        public string Subject { get; set; }

        [RedmineHeader("説明")]
        [XmlElement("description")]
        public string Description { get; set; }

        [RedmineHeader("開始日")]
        [XmlElement("start_date")]
        public DateTime StartDate { get; set; }

        [RedmineHeader("期日")]
        [XmlElement("due_date")]
        public string DueDate { get; set; }

        [RedmineHeader("進捗率")]
        [XmlElement("done_ratio")]
        public int DoneRatio { get; set; }

        [RedmineHeader("予定工数")]
        [XmlElement("estimated_hours")]
        public string EstimatedHours { get; set; }

        [XmlArray("custom_fields")]
        [XmlArrayItem("custom_field")]
        public List<IssueCustomField> CustomFields { get; set; }

        [RedmineHeader("作成日時")]
        [XmlElement("created_on")]
        public DateTime CreatedOn { get; set; }

        [RedmineHeader("更新日時")]
        [XmlElement("updated_on")]
        public DateTime UpdatedOn { get; set; }
    }

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
