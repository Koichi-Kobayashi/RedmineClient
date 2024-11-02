using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cysharp.Text;
using RedmineClient.Models;

namespace RedmineClient.Api
{
    internal class RedmineApi
    {
        private string apiBase;
        private bool isApiAvailable;

        public bool IsApiAvailable { get => isApiAvailable; }

        protected string ApiBase { get => apiBase; set => apiBase = value; }

        public RedmineApi()
        {
            apiBase = ZString.Concat(AppConfig.RedmineHost, "/", "{0}", ".xml?key=", AppConfig.ApiKey);
            if (!string.IsNullOrEmpty(AppConfig.RedmineHost) && !string.IsNullOrEmpty(AppConfig.ApiKey))
            {
                isApiAvailable = true;
            }
        }

        public async Task<HttpResponseMessage> PostHttpResponseMessage(IssuePostData postData)
        {
            string jsonString = JsonSerializer.Serialize(postData);

            return await BaseHttpResponseMessage(postData.GetUrl(), jsonString);
        }

        private static async Task<HttpResponseMessage> BaseHttpResponseMessage(string url, string jsonString)
        {

            HttpClient client = new HttpClient();

            // ヘッダーを追加
            var request = new HttpRequestMessage(HttpMethod.Post, AppConfig.RedmineHost + url)
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-Redmine-API-Key", AppConfig.ApiKey);

            // 非同期でPOST送信
            try
            {
                return await client.SendAsync(request);
            }
            catch
            {
                return null;
            }
        }

        protected virtual async Task<HttpResponseMessage> GetHttpResponseMessage(string api)
        {
            HttpClient client = new HttpClient();

            // 非同期でGETリクエストを送信
            try
            {
                return await client.GetAsync(ZString.Format(apiBase, api));
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class RestApiName
    {
        public static string Projects { get; set; } = "projects";
        public static string Issues { get; set; } = "issues";
    }
}
