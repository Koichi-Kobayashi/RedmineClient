using System.Net.Http;
using Cysharp.Text;
using RedmineClient.Models;
using RedmineClient.XmlData;

namespace RedmineClient
{
    internal class RedmineApi
    {
        private string apiBase;

        public RedmineApi()
        {
            apiBase = ZString.Concat(AppConfig.RedmineHost, "/", "{0}", ".xml?key=", AppConfig.ApiKey);
        }

        public async Task GetProjects()
        {
            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage(RestApiName.Projects);

            // レスポンスの内容を表示
            string responseBody = await response.Content.ReadAsStringAsync();
            var xml = CustomXMLSerializer.LoadXmlDataString<Projects>(responseBody);
            Console.WriteLine(responseBody);
        }

        public async Task GetIssues()
        {
            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage(RestApiName.Issues);
        }

        private async Task<HttpResponseMessage> GetHttpResponseMessage(string api)
        {
            HttpClient client = new HttpClient();

            // 非同期でGETリクエストを送信
            return await client.GetAsync(ZString.Format(apiBase, api));
        }
    }

    internal static class RestApiName
    {
        public static string Projects { get; set; } = "projects";
        public static string Issues { get; set; } = "issues";
    }
}
