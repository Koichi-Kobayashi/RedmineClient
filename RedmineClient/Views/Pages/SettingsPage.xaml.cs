﻿using RedmineClient.Models;
using RedmineClient.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RedmineClient.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            Load();
        }

        private void Load()
        {
            ViewModel.RedmineHost = AppConfig.RedmineHost;
            ViewModel.ApiKey = AppConfig.ApiKey;
        }

        private void 保存ボタン_Click(object sender, RoutedEventArgs e)
        {
            AppConfig.RedmineHost = ViewModel.RedmineHost;
            AppConfig.ApiKey = ViewModel.ApiKey;
            AppConfig.Save();
        }
    }
}
