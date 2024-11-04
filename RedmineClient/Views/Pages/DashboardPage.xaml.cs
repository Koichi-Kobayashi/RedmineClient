using RedmineClient.ViewModels.Pages;
using Wpf.Ui.Controls;

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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as FluentWindow;
            if (window != null)
            {
                var progressRing = window.FindName("ProgressRing") as ProgressRing;
                if (progressRing != null)
                {
                    progressRing.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
