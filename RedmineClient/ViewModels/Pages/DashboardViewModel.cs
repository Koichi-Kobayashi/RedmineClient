using System.Collections.Specialized;
using Redmine.Net.Api;
using Redmine.Net.Api.Net;
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
            // データ読み込みを実行
            await Loaded();
        }

        /// <summary>
        /// Load時の呼ばれる処理
        /// </summary>
        /// <returns></returns>
        private async Task Loaded()
        {
            if (String.IsNullOrEmpty(AppConfig.RedmineHost)) return;
            
            try
            {
                RedmineManagerOptionsBuilder builder = new RedmineManagerOptionsBuilder();
                builder.WithHost(AppConfig.RedmineHost);
                builder.WithApiKeyAuthentication(AppConfig.ApiKey);
                manager = new RedmineManager(builder);

                // 非同期でプロジェクト一覧を取得
                var projects = await Task.Run(() => manager.Get<Project>());
                if (projects != null)
                {
                    Projects = projects;
                    System.Diagnostics.Debug.WriteLine($"Dashboard: {projects.Count}件のプロジェクトを取得しました");
                }
                else
                {
                    Projects = new List<Project>();
                    System.Diagnostics.Debug.WriteLine("Dashboard: プロジェクトが取得できませんでした");
                }

                // プロジェクトが選択されている場合は、そのプロジェクトのチケットを取得
                if (ProjectSelectedIndex >= 0 && ProjectSelectedIndex < Projects.Count)
                {
                    var selectedProject = Projects[ProjectSelectedIndex];
                    await LoadIssuesForProject(selectedProject.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard: エラー - {ex.Message}");
                                    Projects = new List<Project>();
                    Issues = new List<Issue>();
            }
        }

        /// <summary>
        /// 指定されたプロジェクトのチケットを読み込む
        /// </summary>
        private async Task LoadIssuesForProject(int projectId)
        {
            try
            {
                var options = new RequestOptions();
                options.QueryString = new NameValueCollection();
                options.QueryString.Add("project_id", projectId.ToString());
                options.QueryString.Add("limit", "100");
                options.QueryString.Add("offset", "0");

                var issues = await Task.Run(() => manager.Get<Issue>(options));
                if (issues != null)
                {
                    Issues = issues;
                    System.Diagnostics.Debug.WriteLine($"Dashboard: プロジェクトID {projectId} から {issues.Count} 件のチケットを取得しました");
                }
                else
                {
                    Issues = new List<Issue>();
                    System.Diagnostics.Debug.WriteLine($"Dashboard: プロジェクトID {projectId} のチケットが取得できませんでした");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard: チケット読み込みエラー - {ex.Message}");
                Issues = new List<Issue>();
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

        [RelayCommand]
        private void OnShowWindow(DashboardPage page)
        {
            //windowsProviderService.Show<IssueWindow>();
        }

        [RelayCommand]
        private void OnItemClick(Issue issue)
        {
            if (issue == null) return;
            RedmineManagerOptionsBuilder builder = new RedmineManagerOptionsBuilder();
            builder.WithHost(AppConfig.RedmineHost);
            builder.WithApiKeyAuthentication(AppConfig.ApiKey);
            manager = new RedmineManager(builder);

            try
            {
                var viewModel = new IssueWindowViewModel();
                viewModel.Issue = manager.Get<Issue>(issue.Id.ToString(), new RequestOptions()
                {
                    QueryString = new NameValueCollection()
                    {
                        {RedmineKeys.ID, issue.Id.ToString()},
                        {RedmineKeys.INCLUDE, RedmineKeys.JOURNALS}
                    }
                });
                var issueWindow = factory.Create<IssueWindow>(viewModel);
                issueWindow.Show();
            }
            catch (Exception)
            {
                // エラー処理：必要に応じて実装
            }
        }
    }
}
