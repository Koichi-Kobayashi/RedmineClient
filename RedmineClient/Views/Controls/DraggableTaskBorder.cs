using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RedmineClient.ViewModels.Pages;

namespace RedmineClient.Views.Controls
{
    /// <summary>
    /// ドラッグで開始日・終了日を変更できるタスク期間表示Border
    /// </summary>
    public class DraggableTaskBorder : Border
    {
        private bool _isDraggingStart = false;
        private bool _isDraggingEnd = false;
        private bool _isDraggingMove = false; // 期間移動フラグを追加
        private Point _dragStartPoint;
        private DateTime _originalStartDate;
        private DateTime _originalEndDate;
        private DateTime _previewStartDate; // プレビュー用の開始日
        private DateTime _previewEndDate;   // プレビュー用の終了日
        private WbsItem _wbsItem;
        private DateTime _currentDate;
        private int _columnIndex;
        private int _totalColumns;

        // ドラッグハンドルの幅
        private const double DRAG_HANDLE_WIDTH = 10.0;

        public DraggableTaskBorder()
        {
            InitializeBorder();
        }

        /// <summary>
        /// Borderを初期化
        /// </summary>
        private void InitializeBorder()
        {
            // 基本設定 - 背景色はWbsPage.xaml.csで設定されるため、ここでは設定しない
            BorderBrush = Brushes.Blue; // 青色の罫線
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(2);
            Opacity = 0.8;
            Cursor = Cursors.SizeWE;
            
            // デバッグ情報は削除

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

            // ツールチップは動的に設定するため、初期値は空
            ToolTip = "";
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

            // デバッグ情報は削除
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
            _previewStartDate = _wbsItem.StartDate; // プレビュー用の開始日を初期化
            _previewEndDate = _wbsItem.EndDate;     // プレビュー用の終了日を初期化

            // ドラッグ開始位置を判定（日付ベース）
            var relativeX = _dragStartPoint.X;
            var width = ActualWidth;

            // 開始日と終了日に対応する列を計算
            var (startDateColumn, endDateColumn) = GetActualTaskColumns();
            var currentColumn = _columnIndex;

            // デバッグ情報を最小限に削減
            // System.Diagnostics.Debug.WriteLine($"=== 列計算の詳細 ===");
            // System.Diagnostics.Debug.WriteLine($"タスク開始日: {_wbsItem.StartDate:yyyy/MM/dd}");
            // System.Diagnostics.Debug.WriteLine($"タスク終了日: {_wbsItem.EndDate:yyyy/MM/dd}");
            // System.Diagnostics.Debug.WriteLine($"_currentDate: {_currentDate:yyyy/MM/dd}");
            // System.Diagnostics.Debug.WriteLine($"_columnIndex: {_columnIndex}");
            // System.Diagnostics.Debug.WriteLine($"_totalColumns: {_totalColumns}");
            // System.Diagnostics.Debug.WriteLine($"計算された開始日列: {startDateColumn}");
            // System.Diagnostics.Debug.WriteLine($"計算された終了日列: {endDateColumn}");
            // System.Diagnostics.Debug.WriteLine($"現在の列: {currentColumn}");
            // System.Diagnostics.Debug.WriteLine($"==================");

            // デバッグ情報を最小限に削減
            // System.Diagnostics.Debug.WriteLine($"ドラッグ開始位置: X={relativeX:F1}, 幅={width:F1}");
            // System.Diagnostics.Debug.WriteLine($"列情報: 現在列={currentColumn}, 開始日列={startDateColumn}, 終了日列={endDateColumn}");
            // System.Diagnostics.Debug.WriteLine($"ドラッグ開始時の日付: 開始日={_originalStartDate:yyyy/MM/dd}, 終了日={_originalEndDate:yyyy/MM/dd}, 期間={(_originalEndDate - _originalStartDate).TotalDays:F1}日");

            // 日付ベースでドラッグ開始位置を判定
            if (currentColumn == startDateColumn)
            {
                // 開始日の列の場合（開始日変更）
                _isDraggingStart = true;
                _isDraggingEnd = false;
                Cursor = Cursors.SizeWE;
                System.Diagnostics.Debug.WriteLine($"開始日変更ドラッグ開始: {_wbsItem.Title} (開始日列)");
            }
            else if (currentColumn == endDateColumn)
            {
                // 終了日の列の場合（終了日変更）
                _isDraggingStart = false;
                _isDraggingEnd = true;
                Cursor = Cursors.SizeWE;
                System.Diagnostics.Debug.WriteLine($"終了日変更ドラッグ開始: {_wbsItem.Title} (終了日列)");
            }
            else
            {
                // 開始日・終了日以外の列の場合
                _isDraggingStart = false;
                _isDraggingEnd = false;
                Cursor = Cursors.Arrow;
                System.Diagnostics.Debug.WriteLine($"開始日・終了日以外の列クリック: {_wbsItem.Title} (列: {currentColumn})");

                // 現在の列が表示範囲内にあるかチェック
                if (IsCurrentColumnStartOrEndDate())
                {
                    System.Diagnostics.Debug.WriteLine($"  現在の列は表示範囲内です");
                    
                    // タスク期間内の列の場合は、より柔軟なドラッグ判定を行う
                    var (taskStartColumn, taskEndColumn) = GetActualTaskColumns();
                    
                    // _columnIndexは固定列9個を含むグリッド全体のインデックス
                    // 日付列の相対位置に変換する（固定列9個を引く）
                    var dateColumnIndex = _columnIndex - 9;
                    
                    // タスク期間の判定：現在の列がタスクの開始日から終了日の範囲内にあるか
                    // ただし、負の値や上限を超える値も考慮する
                    var isInTaskPeriod = dateColumnIndex >= taskStartColumn && dateColumnIndex <= taskEndColumn;
                    
                    // デバッグ情報を追加
                    System.Diagnostics.Debug.WriteLine($"  タスク期間判定: _columnIndex={_columnIndex}, dateColumnIndex={dateColumnIndex}, taskStartColumn={taskStartColumn}, taskEndColumn={taskEndColumn}, isInTaskPeriod={isInTaskPeriod}");
                    
                    // タスク期間内の列の場合、または現在の列がタスクの表示範囲内にある場合は拡張ドラッグ判定を適用
                    if (isInTaskPeriod || (dateColumnIndex >= 0 && dateColumnIndex < _totalColumns - 9))
                    {
                        // タスク期間内の列の場合：より広い範囲でドラッグ判定
                        var extendedHandleWidth = Math.Max(DRAG_HANDLE_WIDTH, width * 0.4); // 幅の40%または10pxの大きい方
                        
                        // デバッグ情報を追加
                        System.Diagnostics.Debug.WriteLine($"  位置判定: relativeX={relativeX:F1}, width={width:F1}, extendedHandleWidth={extendedHandleWidth:F1}");
                        System.Diagnostics.Debug.WriteLine($"  判定条件: relativeX <= {extendedHandleWidth:F1} = {relativeX <= extendedHandleWidth}, relativeX >= {width - extendedHandleWidth:F1} = {relativeX >= width - extendedHandleWidth}");
                        
                        // より確実な判定のため、右端を優先的に判定
                        if (relativeX >= width - extendedHandleWidth)
                        {
                            // 右側（終了日変更）を優先
                            _isDraggingStart = false;
                            _isDraggingEnd = true;
                            Cursor = Cursors.SizeWE;
                            System.Diagnostics.Debug.WriteLine($"  拡張判定で終了日変更ドラッグ開始: {_wbsItem.Title} (右側)");
                        }
                        else if (relativeX <= extendedHandleWidth)
                        {
                            // 左側（開始日変更）
                            _isDraggingStart = true;
                            _isDraggingEnd = false;
                            Cursor = Cursors.SizeWE;
                            System.Diagnostics.Debug.WriteLine($"  拡張判定で開始日変更ドラッグ開始: {_wbsItem.Title} (左側)");
                        }
                        else
                        {
                            // 中央部分の場合は、期間移動のドラッグ操作を開始
                            _isDraggingStart = false;
                            _isDraggingEnd = false;
                            _isDraggingMove = true; // 期間移動フラグを追加
                            Cursor = Cursors.SizeAll;
                            System.Diagnostics.Debug.WriteLine($"  中央部分で期間移動ドラッグ開始: {_wbsItem.Title} (中央)");
                        }
                    }
                    else
                    {
                        // タスク期間外の列の場合でも、タスクが表示されている列であれば拡張ドラッグ判定を適用
                        if (dateColumnIndex >= 0 && dateColumnIndex < _totalColumns - 9)
                        {
                            // タスクが表示されている列の場合：拡張ドラッグ判定
                            var extendedHandleWidth = Math.Max(DRAG_HANDLE_WIDTH, width * 0.4);
                            
                            if (relativeX <= extendedHandleWidth)
                            {
                                // 左側（開始日変更）
                                _isDraggingStart = true;
                                _isDraggingEnd = false;
                                Cursor = Cursors.SizeWE;
                                System.Diagnostics.Debug.WriteLine($"  拡張判定（期間外）で開始日変更ドラッグ開始: {_wbsItem.Title} (左側)");
                            }
                            else if (relativeX >= width - extendedHandleWidth)
                            {
                                // 右側（終了日変更）
                                _isDraggingStart = false;
                                _isDraggingEnd = true;
                                Cursor = Cursors.SizeWE;
                                System.Diagnostics.Debug.WriteLine($"  拡張判定（期間外）で終了日変更ドラッグ開始: {_wbsItem.Title} (右側)");
                            }
                            else
                            {
                                // 中央部分の場合は、ドラッグ操作を開始しない
                                _isDraggingStart = false;
                                _isDraggingEnd = false;
                                Cursor = Cursors.Arrow;
                                System.Diagnostics.Debug.WriteLine($"  中央部分（期間外）のためドラッグ操作を開始しない: {_wbsItem.Title} (中央)");
                                return;
                            }
                        }
                        else
                        {
                            // 従来の位置ベース判定
                            if (relativeX <= DRAG_HANDLE_WIDTH)
                            {
                                // 左端（開始日変更）
                                _isDraggingStart = true;
                                _isDraggingEnd = false;
                                Cursor = Cursors.SizeWE;
                                System.Diagnostics.Debug.WriteLine($"  位置ベース判定で開始日変更ドラッグ開始: {_wbsItem.Title} (左端)");
                            }
                            else if (relativeX >= width - DRAG_HANDLE_WIDTH)
                            {
                                // 右端（終了日変更）
                                _isDraggingStart = false;
                                _isDraggingEnd = true;
                                Cursor = Cursors.SizeWE;
                                System.Diagnostics.Debug.WriteLine($"  位置ベース判定で終了日変更ドラッグ開始: {_wbsItem.Title} (右端)");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"  位置ベース判定で中央部分のため処理しない");
                                return;
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  現在の列は表示範囲外のため処理しない");
                    return; // 表示範囲外の列は処理しない
                }
            }

            CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// マウス移動イベント
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingStart && !_isDraggingEnd && !_isDraggingMove)
            {
                // マウスの位置に応じてツールチップを動的に設定
                UpdateToolTip(e.GetPosition(this));
                return;
            }

            var currentPoint = e.GetPosition(this);
            var deltaX = currentPoint.X - _dragStartPoint.X;

            // 列幅に基づいてセル単位での移動量を計算
            // WbsPage.xaml.csで設定されている列幅（40px）を使用
            var columnWidth = 40.0;
            
            // セル単位での移動量を計算（隣のセルに移動したら1日変わる）
            var cellsDelta = deltaX / columnWidth;
            
            // セル単位での移動量を整数に丸める（隣のセルに移動したら1日変わる）
            var roundedCellsDelta = Math.Round(cellsDelta);
            var daysDelta = roundedCellsDelta; // 1セル = 1日
            
            // 最小移動単位を1セル（1日）に設定
            if (Math.Abs(daysDelta) < 1)
            {
                daysDelta = 0;
            }

            // デバッグ情報を最小限に削減
            // System.Diagnostics.Debug.WriteLine($"ドラッグ中: deltaX={deltaX:F1}px, columnWidth={columnWidth}px, cellsDelta={cellsDelta:F2}セル, daysDelta={daysDelta}日");

            if (_isDraggingStart)
            {
                // 開始日を変更（プレビュー用、実際の変更はドラッグ完了時）
                // 小数点での計算を避けて、整数日での移動に統一
                var roundedDaysDelta = Math.Round(daysDelta);
                var newStartDate = _originalStartDate.AddDays(roundedDaysDelta);

                // 内側へのドラッグ制限を緩和（最低1日の期間を保つ）
                if (newStartDate >= _originalEndDate.AddDays(-1))
                {
                    newStartDate = _originalEndDate.AddDays(-1);
                    System.Diagnostics.Debug.WriteLine($"  開始日制限: 最低期間を保つため調整: {newStartDate:yyyy/MM/dd}");
                }

                // プレビュー用の変数に設定（元の日付は保持）
                _previewStartDate = newStartDate;
                UpdateVisualFeedback(true);

                // デバッグ情報
                if (Math.Abs(roundedDaysDelta) > 0)
                {
                    var direction = roundedDaysDelta > 0 ? "右方向" : "左方向";
                    var taskDuration = (_originalEndDate - newStartDate).TotalDays;
                    System.Diagnostics.Debug.WriteLine($"開始日変更プレビュー: {_originalStartDate:yyyy/MM/dd} -> {newStartDate:yyyy/MM/dd} (差分: {roundedDaysDelta}セル, {direction}, タスク期間: {taskDuration:F1}日)");
                }
            }
            else if (_isDraggingEnd)
            {
                // 終了日を変更（プレビュー用、実際の変更はドラッグ完了時）
                // 小数点での計算を避けて、整数日での移動に統一
                var roundedDaysDelta = Math.Round(daysDelta);
                var newEndDate = _originalEndDate.AddDays(roundedDaysDelta);

                // 内側へのドラッグ制限を緩和（最低1日の期間を保つ）
                if (newEndDate <= _originalStartDate.AddDays(1))
                {
                    newEndDate = _originalStartDate.AddDays(1);
                    System.Diagnostics.Debug.WriteLine($"  終了日制限: 最低期間を保つため調整: {newEndDate:yyyy/MM/dd}");
                }

                // プレビュー用の変数に設定（元の日付は保持）
                _previewEndDate = newEndDate;
                UpdateVisualFeedback(false);

                // デバッグ情報
                if (Math.Abs(roundedDaysDelta) > 0)
                {
                    var direction = roundedDaysDelta > 0 ? "右方向" : "左方向";
                    var taskDuration = (newEndDate - _originalStartDate).TotalDays;
                    System.Diagnostics.Debug.WriteLine($"終了日変更プレビュー: {_originalEndDate:yyyy/MM/dd} -> {newEndDate:yyyy/MM/dd} (差分: {roundedDaysDelta}セル, {direction}, タスク期間: {taskDuration:F1}日)");
                }
            }
            else if (_isDraggingMove)
            {
                // 期間移動（開始日と終了日を同時に移動、期間は保持）
                // 外側のスコープで既に計算済みの変数を使用
                // currentPoint, deltaX, columnWidth, daysDelta は既に定義済み

                // 小数点での計算を避けて、整数日での移動に統一
                var roundedDaysDelta = Math.Round(daysDelta);
                
                // 開始日と終了日を同時に移動（整数日単位）
                var newStartDate = _originalStartDate.AddDays(roundedDaysDelta);
                var newEndDate = _originalEndDate.AddDays(roundedDaysDelta);

                // プレビュー用の変数に設定（元の日付は保持）
                _previewStartDate = newStartDate;
                _previewEndDate = newEndDate;
                UpdateVisualFeedback(true); // 期間移動用のフィードバック

                // デバッグ情報
                if (Math.Abs(roundedDaysDelta) > 0)
                {
                    var direction = roundedDaysDelta > 0 ? "右方向" : "左方向";
                    var taskDuration = (newEndDate - newStartDate).TotalDays;
                    System.Diagnostics.Debug.WriteLine($"期間移動プレビュー: 開始日={newStartDate:yyyy/MM/dd}, 終了日={newEndDate:yyyy/MM/dd} (差分: {roundedDaysDelta}セル, {direction}, タスク期間: {taskDuration:F1}日)");
                }
            }
        }

        /// <summary>
        /// マウス左ボタンアップイベント
        /// </summary>
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingStart || _isDraggingEnd || _isDraggingMove)
            {
                // 日付変更が完了した場合、ViewModelに通知
                if (_wbsItem != null)
                {
                    // プレビュー用の日付を実際の日付に反映
                    if (_isDraggingStart || _isDraggingMove)
                        _wbsItem.StartDate = _previewStartDate;
                    if (_isDraggingEnd || _isDraggingMove)
                        _wbsItem.EndDate = _previewEndDate;

                    var oldStartDate = _originalStartDate;
                    var oldEndDate = _originalEndDate;

                    // ドラッグ中の変更を確定
                    var finalStartDate = _wbsItem.StartDate;
                    var finalEndDate = _wbsItem.EndDate;

                    // 日付が実際に変更されたかチェック
                    var startDateChanged = finalStartDate != oldStartDate;
                    var endDateChanged = finalEndDate != oldEndDate;

                    if (startDateChanged || endDateChanged)
                    {
                        var newTaskDuration = (finalEndDate - finalStartDate).TotalDays;
                        var originalTaskDuration = (oldEndDate - oldStartDate).TotalDays;

                        System.Diagnostics.Debug.WriteLine($"日付変更完了: {_wbsItem.Title}");
                        if (startDateChanged)
                            System.Diagnostics.Debug.WriteLine($"  開始日: {oldStartDate:yyyy/MM/dd} -> {finalStartDate:yyyy/MM/dd}");
                        if (endDateChanged)
                            System.Diagnostics.Debug.WriteLine($"  終了日: {oldEndDate:yyyy/MM/dd} -> {finalEndDate:yyyy/MM/dd}");
                        System.Diagnostics.Debug.WriteLine($"  タスク期間: {originalTaskDuration:F1}日 -> {newTaskDuration:F1}日 (差分: {newTaskDuration - originalTaskDuration:F1}日)");

                        // UIスレッドでViewModelの日付変更処理を呼び出し
                        try
                        {
                            var viewModel = DataContext as WbsViewModel;
                            if (viewModel != null)
                            {
                                // UIスレッドで非同期処理を実行
                                _ = Dispatcher.BeginInvoke(async () =>
                                {
                                    try
                                    {
                                        await viewModel.UpdateTaskScheduleAsync(_wbsItem, oldStartDate, oldEndDate);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"日付変更の更新処理でエラー: {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ViewModel取得でエラー: {ex.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"日付変更なし: {_wbsItem.Title}");
                    }
                }
            }

            _isDraggingStart = false;
            _isDraggingEnd = false;
            _isDraggingMove = false;
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
            if (!_isDraggingStart && !_isDraggingEnd && !_isDraggingMove)
            {
                // マウスの位置に応じてツールチップを動的に設定
                UpdateToolTip(e.GetPosition(this));
                
                // ドラッグハンドルを表示
                ShowDragHandles();
            }
        }

        /// <summary>
        /// マウスリーブイベント
        /// </summary>
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingStart && !_isDraggingEnd && !_isDraggingMove)
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
            if (Child is Grid grid && _wbsItem != null)
            {
                // 既存のハンドルをクリア
                var existingHandles = grid.Children.OfType<Rectangle>().ToList();
                foreach (var handle in existingHandles)
                {
                    grid.Children.Remove(handle);
                }

                // 色付きのRectangleは削除し、シンプルな透明度の変化のみに
                // ツールチップは動的に更新されるため、ここでは設定しない
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

            // ツールチップは動的に更新されるため、ここでは設定しない
        }

        /// <summary>
        /// 視覚的フィードバックを更新
        /// </summary>
        private void UpdateVisualFeedback(bool isStartDrag)
        {
            if (_isDraggingMove)
            {
                // 期間移動中のフィードバック：背景色と罫線を変更して区別
                Background = Brushes.Orange; // オレンジ色の背景
                BorderBrush = Brushes.DarkOrange; // 濃いオレンジ色の罫線
                BorderThickness = new Thickness(2); // 太い罫線
                Opacity = 0.9;
                
                // 期間中のTextBlockの色もオレンジ系に変更
                System.Diagnostics.Debug.WriteLine($"期間移動開始: TextBlockの色をオレンジ系に変更 (プレビュー開始日: {_previewStartDate:yyyy/MM/dd}, プレビュー終了日: {_previewEndDate:yyyy/MM/dd})");
                UpdateTaskPeriodColors(Brushes.Orange, Brushes.DarkOrange);
            }
            else if (_isDraggingStart)
            {
                // 開始日変更ドラッグ中のフィードバック
                // 背景色は一時的に変更（ドラッグ終了時に元に戻る）
                Background = Brushes.LightBlue; // 薄い青色の背景
                BorderBrush = Brushes.Blue; // 青色の罫線
                Opacity = 0.9;
                
                // 開始日部分のTextBlockを青色系に変更
                System.Diagnostics.Debug.WriteLine($"開始日変更開始: TextBlockの色を青色系に変更");
                UpdateStartDateColors(Brushes.LightBlue, Brushes.Blue);
            }
            else if (_isDraggingEnd)
            {
                // 終了日変更ドラッグ中のフィードバック
                // 背景色は一時的に変更（ドラッグ終了時に元に戻る）
                Background = Brushes.LightGreen; // 薄い緑色の背景
                BorderBrush = Brushes.Green; // 緑色の罫線
                Opacity = 0.9;
                
                // 終了日部分のTextBlockを緑色系に変更
                System.Diagnostics.Debug.WriteLine($"終了日変更開始: TextBlockの色を緑色系に変更");
                UpdateEndDateColors(Brushes.LightGreen, Brushes.Green);
            }
            else
            {
                // 通常のドラッグ中のフィードバック：透明度のみ変更
                Opacity = 0.9;
            }
        }

        /// <summary>
        /// 開始日部分のTextBlockの色を変更
        /// </summary>
        private void UpdateStartDateColors(Brush backgroundBrush, Brush foregroundBrush)
        {
            if (Child is Grid grid)
            {
                // 開始日部分（左端）のTextBlockの色を変更
                foreach (UIElement child in grid.Children)
                {
                    if (Grid.GetColumn(child) == 0 && child is TextBlock textBlock) // 左端の列
                    {
                        textBlock.Background = backgroundBrush;
                        textBlock.Foreground = foregroundBrush;
                    }
                }
            }
        }

        /// <summary>
        /// 終了日部分のTextBlockの色を変更
        /// </summary>
        private void UpdateEndDateColors(Brush backgroundBrush, Brush foregroundBrush)
        {
            if (Child is Grid grid)
            {
                // 終了日部分（右端）のTextBlockの色を変更
                foreach (UIElement child in grid.Children)
                {
                    if (Grid.GetColumn(child) == 2 && child is TextBlock textBlock) // 右端の列
                    {
                        textBlock.Background = backgroundBrush;
                        textBlock.Foreground = foregroundBrush;
                    }
                }
            }
        }

        /// <summary>
        /// 期間中のTextBlockの色を変更
        /// </summary>
        private void UpdateTaskPeriodColors(Brush backgroundBrush, Brush foregroundBrush)
        {
            if (Child is Grid grid)
            {
                // 期間移動中の場合は、プレビュー用の日付を使用
                int startColumn, endColumn;
                if (_isDraggingMove && _previewStartDate != default && _previewEndDate != default)
                {
                    // プレビュー用の日付から列インデックスを計算
                    startColumn = GetColumnIndexFromDate(_previewStartDate);
                    endColumn = GetColumnIndexFromDate(_previewEndDate);
                }
                else
                {
                    // 通常の場合は、現在のタスク期間を使用
                    var columns = GetActualTaskColumns();
                    startColumn = columns.startColumn;
                    endColumn = columns.endColumn;
                }
                
                // 現在のDraggableTaskBorderが表示されている列範囲を計算
                var currentStartColumn = _columnIndex;
                var currentEndColumn = _columnIndex + grid.ColumnDefinitions.Count - 1;
                
                // タスク期間と現在の表示範囲の重複部分を計算
                var overlapStart = Math.Max(startColumn, currentStartColumn);
                var overlapEnd = Math.Min(endColumn, currentEndColumn);
                
                // 重複部分がある場合のみ色を変更
                if (overlapStart <= overlapEnd)
                {
                    // 重複部分の列に対応するTextBlockの色を変更
                    for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
                    {
                        var columnIndex = currentStartColumn + i;
                        
                        // タスク期間内の列の場合のみ色を変更
                        if (columnIndex >= overlapStart && columnIndex <= overlapEnd)
                        {
                            foreach (UIElement child in grid.Children)
                            {
                                if (Grid.GetColumn(child) == i && child is TextBlock textBlock)
                                {
                                    textBlock.Background = backgroundBrush;
                                    textBlock.Foreground = foregroundBrush;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 開始日部分のTextBlockの色を元に戻す
        /// </summary>
        private void ResetStartDateColors()
        {
            if (Child is Grid grid)
            {
                // 開始日部分（左端）のTextBlockの色を元に戻す
                foreach (UIElement child in grid.Children)
                {
                    if (Grid.GetColumn(child) == 0 && child is TextBlock textBlock) // 左端の列
                    {
                        textBlock.Background = Brushes.Transparent;
                        textBlock.Foreground = Brushes.Black;
                    }
                }
            }
        }

        /// <summary>
        /// 終了日部分のTextBlockの色を元に戻す
        /// </summary>
        private void ResetEndDateColors()
        {
            if (Child is Grid grid)
            {
                // 終了日部分（右端）のTextBlockの色を元に戻す
                foreach (UIElement child in grid.Children)
                {
                    if (Grid.GetColumn(child) == 2 && child is TextBlock textBlock) // 右端の列
                    {
                        textBlock.Background = Brushes.Transparent;
                        textBlock.Foreground = Brushes.Black;
                    }
                }
            }
        }

        /// <summary>
        /// 期間中のTextBlockの色を元に戻す
        /// </summary>
        private void ResetTaskPeriodColors()
        {
            if (Child is Grid grid)
            {
                // 期間移動中の場合は、プレビュー用の日付を使用
                int startColumn, endColumn;
                if (_isDraggingMove && _previewStartDate != default && _previewEndDate != default)
                {
                    // プレビュー用の日付から列インデックスを計算
                    startColumn = GetColumnIndexFromDate(_previewStartDate);
                    endColumn = GetColumnIndexFromDate(_previewEndDate);
                }
                else
                {
                    // 通常の場合は、現在のタスク期間を使用
                    var columns = GetActualTaskColumns();
                    startColumn = columns.startColumn;
                    endColumn = columns.endColumn;
                }
                
                // 現在のDraggableTaskBorderが表示されている列範囲を計算
                var currentStartColumn = _columnIndex;
                var currentEndColumn = _columnIndex + grid.ColumnDefinitions.Count - 1;
                
                // タスク期間と現在の表示範囲の重複部分を計算
                var overlapStart = Math.Max(startColumn, currentStartColumn);
                var overlapEnd = Math.Min(endColumn, currentEndColumn);
                
                // 重複部分がある場合のみ色を元に戻す
                if (overlapStart <= overlapEnd)
                {
                    // 重複部分の列に対応するTextBlockの色を元に戻す
                    for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
                    {
                        var columnIndex = currentStartColumn + i;
                        
                        // タスク期間内の列の場合のみ色を元に戻す
                        if (columnIndex >= overlapStart && columnIndex <= overlapEnd)
                        {
                            foreach (UIElement child in grid.Children)
                            {
                                if (Grid.GetColumn(child) == i && child is TextBlock textBlock)
                                {
                                    textBlock.Background = Brushes.Transparent;
                                    textBlock.Foreground = Brushes.Black;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 視覚的フィードバックをクリア
        /// </summary>
        private void ClearVisualFeedback()
        {
            // 透明度を元に戻す
            Opacity = 0.8;
            
            if (_isDraggingMove)
            {
                // 期間移動中だった場合は、背景色と罫線も元に戻す
                // 背景色をクリアしてWbsPage.xaml.csの設定を適用
                ClearValue(BackgroundProperty);
                BorderBrush = Brushes.Blue; // 元の青色の罫線
                BorderThickness = new Thickness(1); // 元の細い罫線
                
                // 期間中のTextBlockの色も元に戻す
                System.Diagnostics.Debug.WriteLine($"期間移動終了: TextBlockの色を元に戻す");
                ResetTaskPeriodColors();
            }
            else if (_isDraggingStart)
            {
                // 開始日変更ドラッグ中だった場合は、背景色と罫線も元に戻す
                // 背景色をクリアしてWbsPage.xaml.csの設定を適用
                ClearValue(BackgroundProperty);
                BorderBrush = Brushes.Blue; // 元の青色の罫線
                
                // 開始日部分のTextBlockの色も元に戻す
                System.Diagnostics.Debug.WriteLine($"開始日変更終了: TextBlockの色を元に戻す");
                ResetStartDateColors();
            }
            else if (_isDraggingEnd)
            {
                // 終了日変更ドラッグ中だった場合は、背景色と罫線も元に戻す
                // 背景色をクリアしてWbsPage.xaml.csの設定を適用
                ClearValue(BackgroundProperty);
                BorderBrush = Brushes.Blue; // 元の青色の罫線
                
                // 終了日部分のTextBlockの色も元に戻す
                System.Diagnostics.Debug.WriteLine($"終了日変更終了: TextBlockの色を元に戻す");
                ResetEndDateColors();
            }
            else
            {
                // 通常のドラッグの場合は、罫線のみ元に戻す
                BorderBrush = Brushes.Blue; // 元の青色の罫線
            }
        }

        /// <summary>
        /// マウスの位置に応じてツールチップを動的に更新
        /// </summary>
        private void UpdateToolTip(Point mousePosition)
        {
            if (_wbsItem == null) return;

            var width = ActualWidth;
            var relativeX = mousePosition.X;
            var extendedHandleWidth = Math.Max(DRAG_HANDLE_WIDTH, width * 0.35);

            if (relativeX <= extendedHandleWidth)
            {
                // 左端の場合
                ToolTip = $"ドラッグして開始日（{_wbsItem.StartDate:yyyy/MM/dd}）を変更";
            }
            else if (relativeX >= width - extendedHandleWidth)
            {
                // 右端の場合
                ToolTip = $"ドラッグして終了日（{_wbsItem.EndDate:yyyy/MM/dd}）を変更";
            }
            else
            {
                // 中央部分の場合
                ToolTip = $"ドラッグして開始日・終了日を変更";
            }
        }

        /// <summary>
        /// 日付から列インデックスを計算
        /// </summary>
        private int GetColumnIndexFromDate(DateTime date)
        {
            // 現在の日付（_currentDate）からの差分日数を計算
            var daysDiff = (date - _currentDate).TotalDays;

            // 列インデックスに変換（0ベース）
            var columnIndex = (int)Math.Round(daysDiff);

            // デバッグ情報を最小限に削減
            // System.Diagnostics.Debug.WriteLine($"GetColumnIndexFromDate: 日付={date:yyyy/MM/dd}, _currentDate={_currentDate:yyyy/MM/dd}, 差分={daysDiff:F1}日, 列={columnIndex}");

            // 範囲チェック
            if (columnIndex < 0)
            {
                // System.Diagnostics.Debug.WriteLine($"  列インデックスが負のため0に調整: {columnIndex} -> 0");
                columnIndex = 0;
            }
            if (columnIndex >= _totalColumns)
            {
                // System.Diagnostics.Debug.WriteLine($"  列インデックスが上限を超えるため調整: {columnIndex} -> {_totalColumns - 1}");
                columnIndex = _totalColumns - 1;
            }

            return columnIndex;
        }

        /// <summary>
        /// タスクの開始日と終了日に対応する実際の列インデックスを計算
        /// </summary>
        private (int startColumn, int endColumn) GetActualTaskColumns()
        {
            if (_wbsItem == null) return (0, 0);

            // タスクの開始日と終了日を基準とした列インデックスを計算
            // _currentDateがタスクの期間外にある場合でも、タスクの実際の位置を正しく計算

            // タスクの開始日が現在表示されている最初の日付（列0）から何日後か
            var startDaysFromFirst = (_wbsItem.StartDate - _currentDate).TotalDays;
            var startColumn = (int)Math.Round(startDaysFromFirst);

            // タスクの終了日が現在表示されている最初の日付（列0）から何日後か
            var endDaysFromFirst = (_wbsItem.EndDate - _currentDate).TotalDays;
            var endColumn = (int)Math.Round(endDaysFromFirst);

            // 範囲チェック（負の値や上限を超える値も許可）
            // タスクが表示範囲外にある場合でも、適切なドラッグ処理を可能にする
            if (startColumn < -100) startColumn = -100; // 最小値制限
            if (startColumn > _totalColumns + 100) startColumn = _totalColumns + 100; // 最大値制限
            if (endColumn < -100) endColumn = -100; // 最小値制限
            if (endColumn > _totalColumns + 100) endColumn = _totalColumns + 100; // 最大値制限

            // デバッグ情報を有効化
            System.Diagnostics.Debug.WriteLine($"GetActualTaskColumns: 開始日列={startColumn}, 終了日列={endColumn}");
            System.Diagnostics.Debug.WriteLine($"  詳細計算: 開始日={_wbsItem.StartDate:yyyy/MM/dd}, 基準日={_currentDate:yyyy/MM/dd}, 開始日差分={startDaysFromFirst:F1}日, 終了日差分={endDaysFromFirst:F1}日");

            return (startColumn, endColumn);
        }

        /// <summary>
        /// 現在の列が開始日または終了日の列かどうかを判定
        /// </summary>
        private bool IsCurrentColumnStartOrEndDate()
        {
            if (_wbsItem == null) return false;

            // 実際のタスクの列インデックスを取得
            var (startDateColumn, endDateColumn) = GetActualTaskColumns();

            // 現在の列が開始日または終了日の列かチェック
            var isStartDateColumn = _columnIndex == startDateColumn;
            var isEndDateColumn = _columnIndex == endDateColumn;

            // 現在の列が開始日と終了日の間にあるかチェック
            var isInTaskPeriod = _columnIndex >= startDateColumn && _columnIndex <= endDateColumn;

            // デバッグ情報を最小限に削減
            // System.Diagnostics.Debug.WriteLine($"IsCurrentColumnStartOrEndDate: 現在列={_columnIndex}, 開始日列={startDateColumn}, 終了日列={endDateColumn}");
            // System.Diagnostics.Debug.WriteLine($"  開始日列か: {isStartDateColumn}, 終了日列か: {isEndDateColumn}, タスク期間内か: {isInTaskPeriod}");

            // タスク期間内または開始日・終了日の列の場合はtrue
            // タスク期間外でも、タスクの表示範囲内にある場合はtrue
            // _columnIndexは0から始まるインデックスなので、有効な範囲は 0 <= _columnIndex < _totalColumns
            var isInDisplayRange = _columnIndex >= 0 && _columnIndex < _totalColumns;
            
            // System.Diagnostics.Debug.WriteLine($"  表示範囲内か: {isInDisplayRange}");
            // System.Diagnostics.Debug.WriteLine($"  判定結果: {isStartDateColumn || isEndDateColumn || isInTaskPeriod || isInDisplayRange}");

            return isStartDateColumn || isEndDateColumn || isInTaskPeriod || isInDisplayRange;
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