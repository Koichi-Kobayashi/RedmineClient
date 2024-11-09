using System.Windows.Media;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;

namespace RedmineClient.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private IEnumerable<DataColor> _colors;

        public virtual async Task OnNavigatedToAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedTo, cts.Token);
        }

        public virtual void OnNavigatedTo()
        {
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            var random = new Random();
            var colorCollection = new List<DataColor>();

            for (int i = 0; i < 8192; i++)
                colorCollection.Add(
                    new DataColor
                    {
                        Color = new SolidColorBrush(
                            Color.FromArgb(
                                (byte)200,
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250),
                                (byte)random.Next(0, 250)
                            )
                        )
                    }
                );

            Colors = colorCollection;

            _isInitialized = true;
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedFrom, cts.Token);
        }

        public void OnNavigatedFrom() { }

        /// <summary>
        /// Dispatches the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to be dispatched.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected static async Task DispatchAsync(Action action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(action);
        }
    }
}
