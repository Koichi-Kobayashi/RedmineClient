using Wpf.Ui.Controls;

namespace RedmineClient.ViewModels
{
    public class SnackbarMessage
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ControlAppearance appearance { get; set; } = ControlAppearance.Secondary;
        public IconElement iconElement { get; set; }
        public TimeSpan timeSpan { get; set; } = new TimeSpan(0, 0, 2);
    }
}
