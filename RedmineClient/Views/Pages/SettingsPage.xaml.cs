using RedmineClient.Models;
using RedmineClient.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

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

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TrackerItem selectedTracker)
            {
                ViewModel.OnTrackerSelectedCommand.Execute(selectedTracker);
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is StatusItem selectedStatus)
            {
                ViewModel.OnStatusSelectedCommand.Execute(selectedStatus);
            }
        }
    }
}
