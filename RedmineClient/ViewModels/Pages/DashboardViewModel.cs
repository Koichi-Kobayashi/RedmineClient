using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using RedmineClient.Models;
using RedmineClient.Services;
using RedmineClient.ViewModels.Windows;
using RedmineClient.Views.Pages;
using RedmineClient.Views.Windows;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace RedmineClient.ViewModels.Pages
{
    public partial class DashboardViewModel(IWindowFactory factory) : BaseViewModel, INavigationAware
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

        private RedmineManager manager;

        //[ObservableProperty]
        //private double _gridHeight = 0;
        #endregion

        public override async void OnNavigatedTo()
        {
            await Loaded();
        }

        /// <summary>
        /// Load時の呼ばれる処理
        /// </summary>
        /// <returns></returns>
        private async Task Loaded()
        {
            RedmineManagerOptionsBuilder builder = new RedmineManagerOptionsBuilder();
            builder.WithHost(AppConfig.RedmineHost);
            builder.WithApiKeyAuthentication(AppConfig.ApiKey);
            manager = new RedmineManager(builder);

            Issues = await manager.GetAsync<Issue>();
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

        [RelayCommand]
        private void OnShowWindow(DashboardPage page)
        {
            //windowsProviderService.Show<IssueWindow>();
        }

        [RelayCommand]
        private void OnItemClick(Issue issue)
        {
            var viewModel = new IssueWindowViewModel();
            viewModel.Issue = issue;
            var issueWindow = factory.Create<IssueWindow>(viewModel);
            issueWindow.Show();
        }
    }
}
