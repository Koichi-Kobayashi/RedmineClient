using System.Net.Http;
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

        protected virtual async Task<HttpResponseMessage> GetHttpResponseMessage(string api)
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
