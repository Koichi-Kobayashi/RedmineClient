using System.Windows;
using Wpf.Ui.Abstractions.Controls;
using RedmineClient.ViewModels.Pages;
using System.ComponentModel;
using System.Windows.Controls;
using RedmineClient.Models;

namespace RedmineClient.Views.Pages
{
    public partial class WbsPageV2 : INavigableView<WbsV2ViewModel>
    {
        public WbsV2ViewModel ViewModel { get; }

        public WbsPageV2(WbsV2ViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateScheduleColumnsVisibility(ViewModel.ShowScheduleColumns);
        }

        private void Recalc_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Recalculate();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WbsV2ViewModel.ShowScheduleColumns))
            {
                UpdateScheduleColumnsVisibility(ViewModel.ShowScheduleColumns);
            }
        }

        private void UpdateScheduleColumnsVisibility(bool show)
        {
            var vis = show ? Visibility.Visible : Visibility.Collapsed;
            if (ColES != null) ColES.Visibility = vis;
            if (ColEF != null) ColEF.Visibility = vis;
            if (ColLS != null) ColLS.Visibility = vis;
            if (ColLF != null) ColLF.Visibility = vis;
            if (ColSlack != null) ColSlack.Visibility = vis;
        }

        // LeftGridのセル編集確定でRedmine更新
        private async void LeftGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row?.Item is WbsSampleTask task)
            {
                try
                {
                    await ViewModel.UpdateIssueDatesAsync(task);
                }
                catch
                {
                    // 失敗は握りつぶす（UIは先に反映）
                }
            }
        }
    }
}

