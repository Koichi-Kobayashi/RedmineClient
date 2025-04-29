using System.Collections.ObjectModel;
using System.Windows.Controls;
using Cysharp.Text;
using Redmine.Net.Api.Types;
using RedmineClient.Helpers;

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
            int count = Issue.CustomFields == null ? 0 : Issue.CustomFields.Count;
            UpdateGrid(count + 4, 4);
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

            // 必ず表示する項目
            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 0, Text = "ステータス：" });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 0, Text = "優先度：" });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 0, Text = "担当者：" });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 0, Text = "カテゴリー：" });
            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 2, Text = "開始日：" });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 2, Text = "期日：" });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 2, Text = "進捗率：" });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 2, Text = "予定工数：" });

            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 1, Text = Issue?.Status?.Name ?? string.Empty });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 1, Text = Issue?.Priority?.Name ?? string.Empty });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 1, Text = Issue?.AssignedTo?.Name ?? string.Empty });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 1, Text = Issue?.Category?.Name ?? string.Empty });
            TextBlocks.Add(new TextBlockItem { Row = 0, Column = 3, Text = Issue?.StartDate?.ToYYYYMMDD() ?? string.Empty });
            TextBlocks.Add(new TextBlockItem { Row = 1, Column = 3, Text = Issue?.DueDate?.ToYYYYMMDD() ?? string.Empty });
            TextBlocks.Add(new TextBlockItem { Row = 2, Column = 3, Text = ZString.Format("{0}%", Issue?.DoneRatio ?? 0) });
            TextBlocks.Add(new TextBlockItem { Row = 3, Column = 3, Text = ZString.Format("{0}時間", Issue?.EstimatedHours ?? 0) });

            // カスタムフィールド
            int count = Issue?.CustomFields?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                var field = Issue?.CustomFields[i];

                if (i % 2 == 0)
                {
                    TextBlocks.Add(new TextBlockItem { Row = i + rows, Column = 0, Text = ZString.Format("{0}：", field?.Name) });
                    TextBlocks.Add(new TextBlockItem { Row = i + rows, Column = 1, Text = field?.Value ?? string.Empty });
                }
                else
                {
                    TextBlocks.Add(new TextBlockItem { Row = i + rows, Column = 2, Text = ZString.Format("{0}：", field?.Name) });
                    TextBlocks.Add(new TextBlockItem { Row = i + rows, Column = 3, Text = field?.Value ?? string.Empty });
                }
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