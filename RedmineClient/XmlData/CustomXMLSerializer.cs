using System.IO;
using System.Xml.Serialization;

namespace RedmineClient.XmlData
{
    internal class CustomXMLSerializer
    {
        public static T LoadXmlDataString<T>(string xml) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            T loadAry;
            using (var fs = new StringReader(xml))
            {
#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
                loadAry = (T)serializer.Deserialize(fs);
#pragma warning restore CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
            }
            return loadAry;
        }
    }
}
