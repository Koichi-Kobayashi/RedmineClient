using Cysharp.Text;
using RedmineClient.XmlData;

namespace RedmineClient.ViewModels.Windows
{
    public class IssueWindowViewModel : BaseViewModel
    {
        public Issue Issue { get; set; }

        public string Title
        {
            get => ZString.Concat(Issue?.Tracker?.Name, " #", Issue?.Id);
        }
    }
}
