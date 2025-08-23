using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace RedmineClient.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            
            // テーマ設定を再適用
            ApplyCurrentTheme();
        }
        
        private void ApplyCurrentTheme()
        {
            try
            {
                ApplicationThemeManager.Apply(AppConfig.ApplicationTheme);
            }
            catch
            {
                // デフォルトはライトテーマ
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
            }
        }
    }
}
