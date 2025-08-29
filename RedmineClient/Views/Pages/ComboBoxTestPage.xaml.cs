using System.Windows.Controls;
using RedmineClient.ViewModels.Pages;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace RedmineClient.Views.Pages
{
    public partial class ComboBoxTestPage : INavigableView<ComboBoxTestViewModel>
    {
        public ComboBoxTestViewModel ViewModel { get; }

        public ComboBoxTestPage(ComboBoxTestViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TestItem selectedItem)
            {
                ViewModel.OnItemSelectedCommand.Execute(selectedItem);
            }
        }
    }
}
