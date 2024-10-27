using System.Net.Http;
using RedmineClient.Models;
using RedmineClient.XmlData;

namespace RedmineClient
{
    internal class RedmineApi
    {
        public RedmineApi() { }

        public async Task GetProjects()
        {
            HttpClient client = new HttpClient();

            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage("projects");

            // レスポンスの内容を表示
            string responseBody = await response.Content.ReadAsStringAsync();
            Project xml = CustomXMLSerializer.LoadXmlDataString<Project>(responseBody);
            Console.WriteLine(responseBody);
        }

        private async Task<HttpResponseMessage> GetHttpResponseMessage(string api)
        {
            HttpClient client = new HttpClient();

            // 非同期でGETリクエストを送信
            return await client.GetAsync(AppConfig.RedmineHost + "/" + api + ".xml?key=" + AppConfig.ApiKey);

        }
    }
}
