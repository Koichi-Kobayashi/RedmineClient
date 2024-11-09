using CommunityToolkit.Mvvm.Messaging;
using RedmineClient.Models;
using RedmineClient.ViewModels;
using RedmineClient.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace RedmineClient.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        private readonly ISnackbarService _snackbarService;

        public MainWindow(
            MainWindowViewModel viewModel,
            IPageService pageService,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);

            // PageクラスからSnackbarを呼び出すメッセージを受け取ったときのメソッドを登録
            WeakReferenceMessenger.Default.Register<SnackbarMessage>(this, (r, m) => ShowSnackbar(m));
            _snackbarService = new SnackbarService();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

        public void ShowWindow()
        {
            AppConfig.Load();
            Show();
        }

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        private void ShowSnackbar(SnackbarMessage message)
        {
            _snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            _snackbarService.Show(message.Title, message.Message, message.appearance, message.iconElement, message.timeSpan);
        }
    }
}
