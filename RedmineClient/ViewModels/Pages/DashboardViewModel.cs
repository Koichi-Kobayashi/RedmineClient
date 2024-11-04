using RedmineClient.Models;
using RedmineClient.XmlData;

namespace RedmineClient.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        #region コマンド
        public IAsyncRelayCommand LoadedCommand { get; }
        #endregion

        #region プロパティ
        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private List<Project> _projects = new List<Project>();

        [ObservableProperty]
        private int _projectSelectedIndex = 0;

        [ObservableProperty]
        private List<Issue> _issues = new List<Issue>();
        #endregion

        public DashboardViewModel()
        {
            LoadedCommand = new AsyncRelayCommand(Loaded);
        }

        /// <summary>
        /// Load時の呼ばれる処理
        /// </summary>
        /// <returns></returns>
        private async Task Loaded()
        {
            Dashboard dashboard = new();
            var projectResult = await dashboard.GetProjects();
            if (projectResult != null)
            {
                Projects = projectResult.ProjectList;
                ProjectSelectedIndex = 0;
            }

            var issuesResult = await dashboard.GetIssues();
            if (issuesResult != null)
            {
                Issues = issuesResult.IssueList;
            }
        }

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
    }
}
