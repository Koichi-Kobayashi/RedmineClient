﻿using System.Collections.Specialized;
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
            await Loaded();
        }

        /// <summary>
        /// Load時の呼ばれる処理
        /// </summary>
        /// <returns></returns>
        private async Task Loaded()
        {
            if (String.IsNullOrEmpty(AppConfig.RedmineHost)) return;
            RedmineManagerOptionsBuilder builder = new RedmineManagerOptionsBuilder();
            builder.WithHost(AppConfig.RedmineHost);
            builder.WithApiKeyAuthentication(AppConfig.ApiKey);
            manager = new RedmineManager(builder);

            try
            {
                var opotions = new RequestOptions()
                {
                    QueryString = new NameValueCollection()
                    {
                        {RedmineKeys.INCLUDE, RedmineKeys.JOURNALS},
                    }
                };
                Issues = await manager.GetAsync<Issue>(opotions);
            }
            catch (Exception ex)
            {
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
        private async Task OnItemClick(Issue issue)
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
            catch (Exception ex)
            {
            }
        }
    }
}
