using System.Xml.Serialization;

namespace RedmineClient.XmlData
{
    [XmlRoot("projects")]
    public class Project
    {
        [XmlElement("project")]
        public XMLData xmlData;
    }

    public class XMLData
    {
        [XmlElement("id")]
        public string id;

        [XmlElement("name")]
        public string name;

        [XmlElement("identifier")]
        public string identifier;

        [XmlElement("description")]
        public string description;

        [XmlElement("homepage")]
        public string homepage;

        [XmlElement("status")]
        public string status;

        [XmlElement("is_public")]
        public string is_public;

        [XmlElement("inherit_members")]
        public string inherit_members;

        [XmlElement("created_on")]
        public string created_on;

        [XmlElement("updated_on")]
        public string updated_on;
    }
}
