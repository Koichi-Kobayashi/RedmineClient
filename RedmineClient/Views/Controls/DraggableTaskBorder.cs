using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RedmineClient.Models;
using RedmineClient.ViewModels.Pages;
using System.Linq;
using System.Threading.Tasks;

namespace RedmineClient.Views.Controls
{
    /// <summary>
    /// ドラッグで開始日・終了日を変更できるタスク期間表示Border
    /// </summary>
    public class DraggableTaskBorder : Border
    {
        private bool _isDraggingStart = false;
        private bool _isDraggingEnd = false;
        private Point _dragStartPoint;
        private DateTime _originalStartDate;
        private DateTime _originalEndDate;
        private WbsItem _wbsItem;
        private DateTime _currentDate;
        private int _columnIndex;
        private int _totalColumns;

        // ドラッグハンドルの幅
        private const double DRAG_HANDLE_WIDTH = 6.0;

        public DraggableTaskBorder()
        {
            InitializeBorder();
        }

        /// <summary>
        /// Borderを初期化
        /// </summary>
        private void InitializeBorder()
        {
            // 基本設定
            Background = Brushes.Transparent;
            BorderBrush = Brushes.Blue;
            BorderThickness = new Thickness(2);
            CornerRadius = new CornerRadius(3);
            Opacity = 0.8;
            Cursor = Cursors.SizeWE;

            // Gridレイアウトを設定（左端、中央、右端の3列）
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DRAG_HANDLE_WIDTH) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(DRAG_HANDLE_WIDTH) });
            
            Child = grid;

            // マウスイベントを設定
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;

            // ツールチップを設定
            ToolTip = "左端（青）をドラッグして開始日を変更、右端（緑）をドラッグして終了日を変更";
        }

        /// <summary>
        /// タスク情報を設定
        /// </summary>
        public void SetTaskInfo(WbsItem wbsItem, DateTime currentDate, int columnIndex, int totalColumns)
        {
            _wbsItem = wbsItem;
            _currentDate = currentDate;
            _columnIndex = columnIndex;
            _totalColumns = totalColumns;
        }

        /// <summary>
        /// マウス左ボタンダウンイベント
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_wbsItem == null) return;

            _dragStartPoint = e.GetPosition(this);
            _originalStartDate = _wbsItem.StartDate;
            _originalEndDate = _wbsItem.EndDate;

            // ドラッグ開始位置を判定
            var relativeX = _dragStartPoint.X;
            var width = ActualWidth;

            if (relativeX <= DRAG_HANDLE_WIDTH)
            {
                // 左端（開始日変更）
                _isDraggingStart = true;
                _isDraggingEnd = false;
                Cursor = Cursors.SizeWE;
                System.Diagnostics.Debug.WriteLine($"開始日変更ドラッグ開始: {_wbsItem.Title}");
            }
            else if (relativeX >= width - DRAG_HANDLE_WIDTH)
            {
                // 右端（終了日変更）
                _isDraggingStart = false;
                _isDraggingEnd = true;
                Cursor = Cursors.SizeWE;
                System.Diagnostics.Debug.WriteLine($"終了日変更ドラッグ開始: {_wbsItem.Title}");
            }
            else
            {
                // 中央（移動）は現在未実装
                _isDraggingStart = false;
                _isDraggingEnd = false;
                Cursor = Cursors.Arrow;
                System.Diagnostics.Debug.WriteLine($"中央部分クリック（移動機能は未実装）: {_wbsItem.Title}");
                return; // 中央部分は処理しない
            }

            CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// マウス移動イベント
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingStart && !_isDraggingEnd) return;

            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _dragStartPoint.X;

            // 列幅に基づいて日数に変換（1列 = 40px = 1日）
            // より細かい制御のため、小数点以下も考慮
            var daysDelta = deltaX / 40.0;
            
            // 最小移動単位を0.5日に設定（より細かい制御）
            if (Math.Abs(daysDelta) < 0.5)
            {
                daysDelta = 0;
            }

            if (_isDraggingStart)
            {
                // 開始日を変更
                var newStartDate = _originalStartDate.AddDays(daysDelta);
                
                // 終了日より前の日付に制限（最小1日間は確保）
                if (newStartDate < _wbsItem.EndDate.AddDays(-1))
                {
                    _wbsItem.StartDate = newStartDate;
                    UpdateVisualFeedback(true);
                    
                    // デバッグ情報
                    if (Math.Abs(daysDelta) > 0.1)
                    {
                        System.Diagnostics.Debug.WriteLine($"開始日変更: {_originalStartDate:yyyy/MM/dd} -> {newStartDate:yyyy/MM/dd} (差分: {daysDelta:F1}日)");
                    }
                }
            }
            else if (_isDraggingEnd)
            {
                // 終了日を変更
                var newEndDate = _originalEndDate.AddDays(daysDelta);
                
                // 開始日より後の日付に制限（最小1日間は確保）
                if (newEndDate > _wbsItem.StartDate.AddDays(1))
                {
                    _wbsItem.EndDate = newEndDate;
                    UpdateVisualFeedback(false);
                    
                    // デバッグ情報
                    if (Math.Abs(daysDelta) > 0.1)
                    {
                        System.Diagnostics.Debug.WriteLine($"終了日変更: {_originalEndDate:yyyy/MM/dd} -> {newEndDate:yyyy/MM/dd} (差分: {daysDelta:F1}日)");
                    }
                }
            }
        }

        /// <summary>
        /// マウス左ボタンアップイベント
        /// </summary>
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingStart || _isDraggingEnd)
            {
                // 日付変更が完了した場合、ViewModelに通知
                if (_wbsItem != null)
                {
                    var oldStartDate = _originalStartDate;
                    var oldEndDate = _originalEndDate;
                    
                    // 日付が実際に変更されたかチェック
                    var startDateChanged = _wbsItem.StartDate != oldStartDate;
                    var endDateChanged = _wbsItem.EndDate != oldEndDate;
                    
                    if (startDateChanged || endDateChanged)
                    {
                        System.Diagnostics.Debug.WriteLine($"日付変更完了: {_wbsItem.Title}");
                        if (startDateChanged)
                            System.Diagnostics.Debug.WriteLine($"  開始日: {oldStartDate:yyyy/MM/dd} -> {_wbsItem.StartDate:yyyy/MM/dd}");
                        if (endDateChanged)
                            System.Diagnostics.Debug.WriteLine($"  終了日: {oldEndDate:yyyy/MM/dd} -> {_wbsItem.EndDate:yyyy/MM/dd}");
                        
                        // 非同期でRedmineに更新を送信
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // ViewModelの日付変更処理を呼び出し
                                var viewModel = DataContext as WbsViewModel;
                                if (viewModel != null)
                                {
                                    await viewModel.UpdateTaskScheduleAsync(_wbsItem, oldStartDate, oldEndDate);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"日付変更の更新処理でエラー: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"日付変更なし: {_wbsItem.Title}");
                    }
                }
            }

            _isDraggingStart = false;
            _isDraggingEnd = false;
            ReleaseMouseCapture();
            Cursor = Cursors.SizeWE;
            ClearVisualFeedback();
            e.Handled = true;
        }

        /// <summary>
        /// マウスエンターイベント
        /// </summary>
        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isDraggingStart && !_isDraggingEnd)
            {
                // ドラッグハンドルを表示
                ShowDragHandles();
            }
        }

        /// <summary>
        /// マウスリーブイベント
        /// </summary>
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingStart && !_isDraggingEnd)
            {
                // ドラッグハンドルを非表示
                HideDragHandles();
            }
        }

        /// <summary>
        /// ドラッグハンドルを表示
        /// </summary>
        private void ShowDragHandles()
        {
            if (Child is Grid grid)
            {
                // 既存のハンドルをクリア
                var existingHandles = grid.Children.OfType<Rectangle>().ToList();
                foreach (var handle in existingHandles)
                {
                    grid.Children.Remove(handle);
                }

                // 左端のドラッグハンドル（開始日変更）
                var leftHandle = new Rectangle
                {
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 2,
                    Opacity = 0.9
                };
                Grid.SetColumn(leftHandle, 0);
                grid.Children.Add(leftHandle);

                // 右端のドラッグハンドル（終了日変更）
                var rightHandle = new Rectangle
                {
                    Fill = Brushes.LightGreen,
                    Stroke = Brushes.DarkGreen,
                    StrokeThickness = 2,
                    Opacity = 0.9
                };
                Grid.SetColumn(rightHandle, 2);
                grid.Children.Add(rightHandle);

                // ツールチップを更新
                ToolTip = "左端（青）をドラッグして開始日を変更、右端（緑）をドラッグして終了日を変更";
            }
        }

        /// <summary>
        /// ドラッグハンドルを非表示
        /// </summary>
        private void HideDragHandles()
        {
            if (Child is Grid grid)
            {
                var handles = grid.Children.OfType<Rectangle>().ToList();
                foreach (var handle in handles)
                {
                    grid.Children.Remove(handle);
                }
            }
            
            // ツールチップを元に戻す
            ToolTip = "左端をドラッグして開始日を変更、右端をドラッグして終了日を変更";
        }

        /// <summary>
        /// 視覚的フィードバックを更新
        /// </summary>
        private void UpdateVisualFeedback(bool isStartDrag)
        {
            if (isStartDrag)
            {
                // 開始日変更中のフィードバック
                Background = Brushes.LightBlue;
                BorderBrush = Brushes.DarkBlue;
                Opacity = 0.9;
            }
            else
            {
                // 終了日変更中のフィードバック
                Background = Brushes.LightGreen;
                BorderBrush = Brushes.DarkGreen;
                Opacity = 0.9;
            }
        }

        /// <summary>
        /// 視覚的フィードバックをクリア
        /// </summary>
        private void ClearVisualFeedback()
        {
            Background = Brushes.Transparent;
            BorderBrush = Brushes.Blue;
            Opacity = 0.8;
        }

        /// <summary>
        /// タスク期間の表示/非表示を制御
        /// </summary>
        public static bool ShouldShowTaskPeriod(DateTime currentDate, DateTime startDate, DateTime endDate)
        {
            return currentDate >= startDate && currentDate <= endDate;
        }
    }
}
