using System.Net.Http;
using RedmineClient.XmlData;

namespace RedmineClient.Api
{
    internal class Project : RedmineApi
    {
        public Project() { }

        public async Task GetProjects()
        {
            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage(RestApiName.Projects);

            // レスポンスの内容を表示
            string responseBody = await response.Content.ReadAsStringAsync();
            var xml = CustomXMLSerializer.LoadXmlDataString<Projects>(responseBody);
            Console.WriteLine(responseBody);
        }

    }
}
