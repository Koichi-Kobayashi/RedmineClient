using RedmineClient.XmlData;

namespace RedmineClient.Models
{
    public class Dashboard
    {
        public Dashboard() { }

        public Task<Projects> GetProjects()
        {
            var api = new Api.Project();
            return (Task<Projects>)Task.Run(api.GetProjects);
        }

        public Task<Issues> GetIssues()
        {
            var api = new Api.Issue();
            return (Task<Issues>)Task.Run(api.GetIssues);
        }

    }
}
