using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RedmineClient.ViewModels.Pages
{
    public partial class ComboBoxTestViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<TestItem> _availableItems = new();

        [ObservableProperty]
        private TestItem? _selectedItem;

        public ComboBoxTestViewModel()
        {
            // テスト用のアイテムを初期化
            AvailableItems.Add(new TestItem { Id = 1, Name = "アイテム1" });
            AvailableItems.Add(new TestItem { Id = 2, Name = "アイテム2" });
            AvailableItems.Add(new TestItem { Id = 3, Name = "アイテム3" });
            
            // デフォルトで最初のアイテムを選択
            SelectedItem = AvailableItems[0];
        }

        public ICommand OnItemSelectedCommand => new RelayCommand<TestItem?>(OnItemSelected);

        private void OnItemSelected(TestItem? item)
        {
            if (item != null)
            {
                System.Diagnostics.Debug.WriteLine($"テスト: アイテムが選択されました: {item.Name} (ID: {item.Id})");
                
                // SelectedItemを更新
                SelectedItem = item;
                
                System.Diagnostics.Debug.WriteLine($"テスト: 更新後のSelectedItem: {SelectedItem?.Name ?? "null"} (ID: {SelectedItem?.Id ?? 0})");
            }
        }
    }

    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
