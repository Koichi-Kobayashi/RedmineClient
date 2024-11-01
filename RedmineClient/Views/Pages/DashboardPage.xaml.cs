using RedmineClient.Models;
using RedmineClient.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RedmineClient.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeData();
        }

        private async Task InitializeData()
        {
            Dashboard dashboard = new Dashboard(ViewModel);
            var projectResult = await dashboard.GetProjects();
            if (projectResult != null)
            {
                ViewModel.Projects = projectResult.ProjectList;
                ViewModel.ProjectSelectedIndex = 0;
            }

            var issuesResult = await dashboard.GetIssues();
            if (issuesResult != null)
            {
                ViewModel.Issues = issuesResult.IssueList;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as FluentWindow;
            if (window != null)
            {
                var progressRing = window.FindName("ProgressRing") as ProgressRing;
                if (progressRing != null)
                {
                    progressRing.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
