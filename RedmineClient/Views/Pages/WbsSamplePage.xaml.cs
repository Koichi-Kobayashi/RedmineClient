using Wpf.Ui.Abstractions.Controls;
using RedmineClient.ViewModels.Pages;

namespace RedmineClient.Views.Pages
{
    public partial class WbsSamplePage : INavigableView<WbsViewModel>
    {
        public WbsViewModel ViewModel { get; }

        public WbsSamplePage(WbsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}


