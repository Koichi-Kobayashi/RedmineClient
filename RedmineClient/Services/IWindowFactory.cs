using RedmineClient.ViewModels;

namespace RedmineClient.Services
{
    public interface IWindowFactory
    {
        T Create<T>(IViewModel viewModel);
    }
}
