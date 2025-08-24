using CommunityToolkit.Mvvm.Messaging;
using RedmineClient.Models;
using RedmineClient.ViewModels;
using RedmineClient.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
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
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();

            navigationService.SetNavigationControl(RootNavigation);

            // PageクラスからSnackbarを呼び出すメッセージを受け取ったときのメソッドを登録
            WeakReferenceMessenger.Default.Register<SnackbarMessage>(this, (r, m) => ShowSnackbar(m));
            _snackbarService = new SnackbarService();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow()
        {
            // 設定を読み込み
            AppConfig.Load();
            
            Show();
            
            // 初期ページとしてWBSページを設定
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // WBSページにナビゲート
                    RootNavigation.Navigate(typeof(Views.Pages.WbsPage));
                    System.Diagnostics.Debug.WriteLine("初期ページとしてWBSページを設定しました");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"初期ページ設定中にエラー: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        
        private void ApplyCurrentTheme()
        {
            try
            {
                ApplicationThemeManager.Apply(AppConfig.ApplicationTheme);
            }
            catch
            {
                // デフォルトはライトテーマ
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
            }
        }

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // ウィンドウサイズを保存
            AppConfig.WindowWidth = Width;
            AppConfig.WindowHeight = Height;
            AppConfig.Save();

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
