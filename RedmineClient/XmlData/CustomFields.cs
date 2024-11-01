using System.Xml.Serialization;

namespace RedmineClient.XmlData
{
    [XmlRoot("custom_fields")]
    public class CustomFields
    {
        [XmlElement("custom_field")]
        public List<CustomField> CustomFieldList { get; set; }
    }

    public class CustomField
    {
        [XmlElement("id")]
        public int Id { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("customized_type")]
        public string CustomizedType { get; set; }
        [XmlElement("field_format")]
        public string FieldFormat { get; set; }
        public string Regexp { get; set; }
        public string MinLength { get; set; }
        public string MaxLength { get; set; }
        [XmlElement("is_required")]
        public bool IsRequired { get; set; }
        [XmlElement("is_filter")]
        public bool IsFilter { get; set; }
        public bool Searchable { get; set; }
        public bool Multiple { get; set; }
        [XmlElement("default_value")]
        public string DefaultValue { get; set; }
        public bool Visible { get; set; }
        [XmlArray("possible_values")]
        [XmlArrayItem("possible_value")]
        public List<PossibleValue> PossibleValues { get; set; }
    }

    public class PossibleValue
    {
        [XmlElement("value")]
        public string Value { get; set; }
    }
}
