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
            InitializeData();

            InitializeComponent();
        }

        private void InitializeData()
        {
            Dashboard dashboard = new Dashboard(ViewModel);
            var pro = dashboard.GetProjects();
            ViewModel.Projects = pro.Result.ProjectList;
        }
    }
}
