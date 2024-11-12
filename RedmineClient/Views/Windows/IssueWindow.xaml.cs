using RedmineClient.ViewModels.Windows;
using Wpf.Ui.Controls;

namespace RedmineClient.Views.Windows
{
    /// <summary>
    /// IssueWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class IssueWindow : FluentWindow
    {
        public IssueWindowViewModel ViewModel { get; }

        public IssueWindow(IssueWindowViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
