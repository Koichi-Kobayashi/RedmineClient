using RedmineClient.ViewModels.Windows;
using System.Windows;

namespace RedmineClient.Views.Windows
{
    /// <summary>
    /// CreateIssueWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CreateIssueWindow : Window
    {
        public CreateIssueWindow(CreateIssueViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
