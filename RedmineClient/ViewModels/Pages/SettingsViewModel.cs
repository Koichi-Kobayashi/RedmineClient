using CommunityToolkit.Mvvm.Messaging;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace RedmineClient.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        private string _redmineHost = String.Empty;

        [ObservableProperty]
        private string _login = String.Empty;

        [ObservableProperty]
        private string _password = String.Empty;

        [ObservableProperty]
        private string _apiKey = String.Empty;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        public async Task OnNavigatedToAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedFrom, cts.Token);
        }

        public virtual async Task OnNavigatedTo()
        {
            await InitializeViewModel();
        }

        private Task InitializeViewModel()
        {
            CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"UiDesktopApp1 - {GetAssemblyVersion()}";

            return Task.CompletedTask;
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedFrom, cts.Token);
        }

        public virtual async Task OnNavigatedFrom()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Dispatches the specified Func on the UI thread.
        /// </summary>
        /// <param name="callback">The Func to be dispatched.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected static async Task DispatchAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(callback);
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;

                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;

                    break;
            }
        }

        [RelayCommand]
        private void OnSave()
        {
            AppConfig.RedmineHost = RedmineHost;
            AppConfig.ApiKey = ApiKey;
            AppConfig.Save();

            WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = "設定を保存しました。" });
        }
    }
}
