using System.Collections.ObjectModel;
using System.Windows.Controls;
using Cysharp.Text;
using RedmineClient.XmlData;

namespace RedmineClient.ViewModels.Windows
{
    public partial class IssueWindowViewModel : BaseViewModel
    {
        public Issue Issue { get; set; }

        [ObservableProperty]
        private ObservableCollection<RowDefinition> _rowDefinitions;

        public string Title
        {
            get => ZString.Concat(Issue?.Tracker?.Name, " #", Issue?.Id);
        }

        [RelayCommand]
        private void OnLoaded()
        {

        }
    }
}
