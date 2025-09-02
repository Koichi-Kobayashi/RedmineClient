using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RedmineClient.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "RedmineClient - WBS";

        [ObservableProperty]
        private string _connectionStatus = "未接続";

        [ObservableProperty]
        private string _lastUpdateTime = "--";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "チケット一覧",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "WBS",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Diagram24 },
                TargetPageType = typeof(Views.Pages.WbsPage)
            },
            new NavigationViewItem()
            {
                Content = "WBS（サンプル）",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Timeline24 },
                TargetPageType = typeof(Views.Pages.WbsSamplePage)
            },

        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "設定",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "チケット一覧", Tag = "tray_home" }
        };

        [RelayCommand]
        private void SelectionChanged(object parameter)
        {

        }
    }
}
