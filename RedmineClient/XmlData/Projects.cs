using System.Xml.Serialization;

namespace RedmineClient.XmlData
{
    [XmlRoot("projects")]
    public class Projects
    {
        [XmlElement("project")]
        public List<Project> ProjectList { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }

    public class Project
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("identifier")]
        public string Identifier { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("created_on")]
        public DateTime CreatedOn { get; set; }

        [XmlElement("updated_on")]
        public DateTime UpdatedOn { get; set; }

        [XmlElement("is_public")]
        public bool IsPublic { get; set; }
    }

}
