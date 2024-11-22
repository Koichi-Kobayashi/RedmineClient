using System.Collections.ObjectModel;
using System.Windows.Controls;
using Cysharp.Text;
using RedmineClient.Helpers;
using RedmineClient.XmlData;

namespace RedmineClient.ViewModels.Windows
{
    public partial class IssueWindowViewModel : BaseViewModel
    {
        public Issue Issue { get; set; }

        [ObservableProperty]
        private ObservableCollection<RowDefinition> _rowDefinitions;

        [ObservableProperty]
        private ObservableCollection<ColumnDefinition> _columnDefinitions;

        [ObservableProperty]
        private ObservableCollection<TextBlockItem> _textBlocks;

        public string Title
        {
            get => ZString.Concat(Issue?.Tracker?.Name, " #", Issue?.Id);
        }

        [RelayCommand]
        private void OnLoaded()
        {
            RowDefinitions = new ObservableCollection<RowDefinition>();
            ColumnDefinitions = new ObservableCollection<ColumnDefinition>();
            TextBlocks = new ObservableCollection<TextBlockItem>();
            UpdateGrid(Issue.CustomFields.Count + 4, 4);
        }

        private void UpdateGrid(int rows, int columns)
        {
            for (int i = 0; i < rows; i++)
            {
                RowDefinitions.Add(new RowDefinition());
            }
            for (int j = 0; j < columns; j++)
            {
                ColumnDefinitions.Add(new ColumnDefinition());
            }

            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 0, Text = "ステータス：" });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 0, Text = "優先度：" });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 0, Text = "担当者：" });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 0, Text = "カテゴリー：" });
            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 2, Text = "開始日：" });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 2, Text = "期日：" });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 2, Text = "進捗率：" });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 2, Text = "予定工数：" });

            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 1, Text = Issue.Status.Name });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 1, Text = Issue.Priority.Name });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 1, Text = Issue.AssignedTo.Name });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 1, Text = Issue.Category.Name });
            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 3, Text = Issue.StartDate.ToYYYYMMDD() });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 3, Text = Issue.DueDate.ToYYYYMMDD() });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 3, Text = string.Format("{0}%", Issue.DoneRatio) });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 3, Text = string.Format("{0}時間", Issue.EstimatedHours) });

            foreach (var item in Issue.CustomFields)
            {
            }
        }
    }

    /// <summary>
    /// ViewにTextBlockを動的に表示するためのクラス
    /// </summary>
    public class TextBlockItem
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string Text { get; set; }
    }
}