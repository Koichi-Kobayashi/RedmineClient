using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Redmine.Net.Api.Types;
using RedmineClient.Services;
using System.Windows;
using System.Windows.Input;

namespace RedmineClient.ViewModels.Windows
{
    public partial class CreateIssueViewModel : ObservableObject
    {
        public ICommand CreateIssueCommand { get; }
        public ICommand CancelCommand { get; }

        public CreateIssueViewModel(RedmineService redmineService, Project selectedProject)
        {
            CreateIssueCommand = new RelayCommand(CreateIssue);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void CreateIssue()
        {
            MessageBox.Show("チケット作成機能は実装中です。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel()
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
    }
}
