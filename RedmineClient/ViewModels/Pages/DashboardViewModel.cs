using RedmineClient.Models;
using RedmineClient.Views.Pages;
using RedmineClient.XmlData;
using Wpf.Ui.Controls;

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

        [RelayCommand]
        private void OnProgressRing(DashboardPage page)
        {
            var activeWindow = Application.Current.Windows
                                          .OfType<Window>()
                                          .SingleOrDefault(x => x.IsActive);
            if (activeWindow != null)
            {
                var progressRing = activeWindow.FindName("ProgressRing") as ProgressRing;
                if (progressRing != null)
                {
                    if (progressRing.Visibility == Visibility.Hidden)
                    {
                        progressRing.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        progressRing.Visibility = Visibility.Hidden;
                    }
                }
            }
        }
    }
}
