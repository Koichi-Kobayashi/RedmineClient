using System.Net.Http;
using RedmineClient.XmlData;

namespace RedmineClient.Api
{
    internal class Project : RedmineApi
    {
        private Projects projects = new Projects();

        public Projects Projects { get => projects; }

        public Project() { }

        public async Task<Projects> GetProjects()
        {
            // 非同期でGETリクエストを送信
            HttpResponseMessage response = await GetHttpResponseMessage(RestApiName.Projects);
            string responseBody = await response.Content.ReadAsStringAsync();
            projects = CustomXMLSerializer.LoadXmlDataString<Projects>(responseBody);

            return projects;
        }

    }
}
