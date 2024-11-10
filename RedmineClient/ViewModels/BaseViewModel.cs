namespace RedmineClient.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [RelayCommand]
        private void OnSizeChanged((double width, double height) size)
        {

        }
    }
}
