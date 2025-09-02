using System.Windows;
using Wpf.Ui.Abstractions.Controls;
using RedmineClient.ViewModels.Pages;
using System.ComponentModel;

namespace RedmineClient.Views.Pages
{
    public partial class WbsSamplePage : INavigableView<WbsSampleViewModel>
    {
        public WbsSampleViewModel ViewModel { get; }

        public WbsSamplePage(WbsSampleViewModel viewModel)
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
            if (e.PropertyName == nameof(WbsSampleViewModel.ShowScheduleColumns))
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
    }
}


