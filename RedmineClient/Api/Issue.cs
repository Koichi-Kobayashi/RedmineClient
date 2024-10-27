using System.Net.Http;
using Cysharp.Text;
using RedmineClient.XmlData;

namespace RedmineClient.Api
{
    internal class Issue : RedmineApi
    {
        private string apiBase;

        public Issue() { }

        public async Task<Issues> GetIssues()
        {
            apiBase = base.ApiBase;

            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage(RestApiName.Issues);

            // レスポンスの内容を取得
            string responseBody = await response.Content.ReadAsStringAsync();
            return CustomXMLSerializer.LoadXmlDataString<Issues>(responseBody);
        }

        public async Task<Issues> GetIssues(int offset, int limit)
        {
            apiBase = ZString.Format(ZString.Concat(base.ApiBase, "&offset={1}&limit={2}"), RestApiName.Issues, offset, limit);

            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage(RestApiName.Issues);

            // レスポンスの内容を取得
            string responseBody = await response.Content.ReadAsStringAsync();
            var xml = CustomXMLSerializer.LoadXmlDataString<Issues>(responseBody);
            return xml;
        }

        protected override async Task<HttpResponseMessage> GetHttpResponseMessage(string api)
        {
            HttpClient client = new HttpClient();

            // 非同期でGETリクエストを送信
            return await client.GetAsync(ZString.Format(apiBase, api));
        }
    }
}
