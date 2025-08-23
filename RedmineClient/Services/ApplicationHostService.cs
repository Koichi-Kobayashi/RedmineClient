using Microsoft.Extensions.Hosting;
using RedmineClient.Views.Windows;
using Wpf.Ui;

namespace RedmineClient.Services
{
    /// <summary>
    /// Managed host of the application.
    /// </summary>
    public class ApplicationHostService(IServiceProvider serviceProvider) : IHostedService
    {
        private INavigationWindow? _navigationWindow;

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await HandleActivationAsync();
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates main window during activation.
        /// </summary>
        private Task HandleActivationAsync()
        {
            if (!Application.Current.Windows.OfType<MainWindow>().Any())
            {
                // ウィンドウの作成と表示（UIスレッドで実行）
                _navigationWindow = (serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow)!;
                _navigationWindow!.ShowWindow();

                // 初期ページのナビゲーション
                _navigationWindow.Navigate(typeof(Views.Pages.DashboardPage));
            }
            
            return Task.CompletedTask;
        }
    }
}
