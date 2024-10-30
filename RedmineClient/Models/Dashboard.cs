using RedmineClient.ViewModels.Pages;
using RedmineClient.XmlData;

namespace RedmineClient.Models
{
    public class Dashboard
    {
        private DashboardViewModel viewModel;

        public Dashboard() : this(new DashboardViewModel()) { }
        public Dashboard(DashboardViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public Task<Projects> GetProjects()
        {
            var api = new Api.Project();
            return (Task<Projects>)Task.Run(api.GetProjects);
        }
    }
}
