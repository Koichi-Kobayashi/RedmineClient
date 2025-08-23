using Microsoft.Xaml.Behaviors;
using RedmineClient.ViewModels.Pages;
using RedmineClient.Views.Pages;
using RedmineClient.Views.Windows;
using Wpf.Ui.Controls;

namespace RedmineClient.Helpers
{
    public class ResizeBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SizeChanged += OnSizeChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is DashboardPage page && page.DataContext is DashboardViewModel viewModel)
            {
                //viewModel.GridHeight = e.NewSize.Height;
            }
            if (sender is MainWindow mainWindow)
            {
                // MainWindowのサイズ変更処理（必要に応じて実装）
                var selectedItem = mainWindow.RootNavigation?.SelectedItem as NavigationViewItem;
                var targetPageType = selectedItem?.TargetPageType;
                // 現在は何も処理しない
            }
        }
    }
}
