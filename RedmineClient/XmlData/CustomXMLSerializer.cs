using System.IO;
using System.Xml.Serialization;

namespace RedmineClient.XmlData
{
    internal class CustomXMLSerializer
    {
        public static T LoadXmlDataString<T>(string xml) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(xml);
            T loadAry;
            using (StringReader fs = new StringReader(xml))
            {
                loadAry = (T)serializer.Deserialize(fs);
            }
            return loadAry;
        }
    }
}
