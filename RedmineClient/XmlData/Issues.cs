namespace RedmineClient.XmlData
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("issues")]
    public class Issues
    {
        [XmlElement("issue")]
        public List<Issues> IssueList { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("count")]
        public int Count { get; set; }
    }

    public class Issue
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("project")]
        public Project Project { get; set; }

        [XmlElement("tracker")]
        public Tracker Tracker { get; set; }

        [XmlElement("status")]
        public Status Status { get; set; }

        [XmlElement("priority")]
        public Priority Priority { get; set; }

        [XmlElement("author")]
        public Author Author { get; set; }

        [XmlElement("category")]
        public Category Category { get; set; }

        [XmlElement("subject")]
        public string Subject { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("start_date")]
        public DateTime StartDate { get; set; }

        [XmlElement("due_date")]
        public string DueDate { get; set; }

        [XmlElement("done_ratio")]
        public int DoneRatio { get; set; }

        [XmlElement("estimated_hours")]
        public string EstimatedHours { get; set; }

        [XmlArray("custom_fields")]
        [XmlArrayItem("custom_field")]
        public List<CustomField> CustomFields { get; set; }

        [XmlElement("created_on")]
        public DateTime CreatedOn { get; set; }

        [XmlElement("updated_on")]
        public DateTime UpdatedOn { get; set; }
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

    public class CustomField
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
