using System.Windows;
using Wpf.Ui.Abstractions.Controls;
using RedmineClient.ViewModels.Pages;

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
        }

        private void Recalc_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Recalculate();
        }
    }
}


