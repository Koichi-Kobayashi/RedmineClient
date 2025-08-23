using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using System.Windows;

namespace RedmineClient.Views.Pages
{
    /// <summary>
    /// WbsPage.xaml の相互作用ロジック
    /// </summary>
    public partial class WbsPage : INavigableView<WbsViewModel>
    {
        public WbsViewModel ViewModel { get; }

        public WbsPage(WbsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
