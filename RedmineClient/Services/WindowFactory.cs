using RedmineClient.ViewModels;

namespace RedmineClient.Services
{
    public class WindowFactory : IWindowFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(IViewModel viewModel)
        {
            return ActivatorUtilities.CreateInstance<T>(_serviceProvider, viewModel);
        }
    }
}
