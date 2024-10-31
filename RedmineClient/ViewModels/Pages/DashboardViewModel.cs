using RedmineClient.XmlData;

namespace RedmineClient.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private List<Project> _projects = new List<Project>();

        [ObservableProperty]
        private int _projectSelectedIndex = 0;

        public DashboardViewModel() { }

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
    }
}
