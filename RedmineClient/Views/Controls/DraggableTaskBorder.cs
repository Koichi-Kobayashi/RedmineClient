using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System;
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
        private DateTime _startDate; // 表示開始日（列0の日付）
        private DateTime _currentDate; // 現在のDraggableTaskBorderが表示している日付
        private int _columnIndex;
        private int _totalColumns;

        // ドラッグハンドルの幅
        private const double DRAG_HANDLE_WIDTH = 10.0;
        
        // タスク情報列数（ID, タスク名, 説明, 開始日, 終了日, 進捗, ステータス, 優先度, 担当者の9列）
        private const int TASK_INFO_COLUMN_COUNT = 9;

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
            // 透明度はWbsPage.xaml.csで日付に応じて動的に設定されるため、ここでは設定しない
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
        public void SetTaskInfo(WbsItem wbsItem, DateTime startDate, DateTime currentDate, int columnIndex, int totalColumns)
        {
            _wbsItem = wbsItem;
            _startDate = startDate; // 表示開始日（列0の日付）
            _currentDate = currentDate; // 現在のDraggableTaskBorderが表示している日付
            _columnIndex = columnIndex;
            _totalColumns = totalColumns;
            
            // タスクの開始日・終了日に対応する列インデックスを事前計算
            var (startDateColumn, endDateColumn) = GetActualTaskColumns();
            
            // GridにTextBlockを追加
            InitializeGridContent();
        }

        /// <summary>
        /// Gridの内容を初期化
        /// </summary>
        private void InitializeGridContent()
        {
            if (Child is Grid grid)
            {
                // 既存の子要素をクリア
                grid.Children.Clear();
            }
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
                _isDraggingMove = false;
                Cursor = Cursors.SizeWE;
            }
            else if (currentColumn == endDateColumn)
            {
                // 終了日の列の場合（終了日変更）
                _isDraggingStart = false;
                _isDraggingEnd = true;
                _isDraggingMove = false;
                Cursor = Cursors.SizeWE;
            }
            else
            {
                // 開始日・終了日以外の列の場合
                _isDraggingStart = false;
                _isDraggingEnd = false;
                Cursor = Cursors.Arrow;

                // 現在の列が表示範囲内にあるかチェック
                if (IsCurrentColumnStartOrEndDate())
                {
                    // タスク期間内の列の場合は、より柔軟なドラッグ判定を行う
                    var (taskStartColumn, taskEndColumn) = GetActualTaskColumns();
                    
                    // _columnIndexはタスク情報列を含むグリッド全体のインデックス
                    // 日付列の相対位置に変換する（タスク情報列数を引く）
                    var dateColumnIndex = _columnIndex - TASK_INFO_COLUMN_COUNT;
                    
                    // タスク期間の判定：現在の列がタスクの開始日から終了日の範囲内にあるか
                    // ただし、負の値や上限を超える値も考慮する
                    var isInTaskPeriod = dateColumnIndex >= taskStartColumn && dateColumnIndex <= taskEndColumn;
                    
                    // タスク期間内の列の場合、または現在の列がタスクの表示範囲内にある場合は拡張ドラッグ判定を適用
                    if (isInTaskPeriod || (dateColumnIndex >= 0 && dateColumnIndex < _totalColumns - TASK_INFO_COLUMN_COUNT))
                    {
                        // セル単位での判定に変更
                        // 左端のセル（列0）: 開始日変更
                        // 右端のセル（列2）: 終了日変更
                        // 中央のセル（列1）: 期間移動
                        var clickedColumn = Grid.GetColumn(e.OriginalSource as UIElement);
                        
                        // e.OriginalSourceがDraggableTaskBorder自体の場合は、そのDraggableTaskBorderが表示している日付で判定
                        if (e.OriginalSource is DraggableTaskBorder)
                        {
                            
                            // 現在のDraggableTaskBorderが表示している日付が開始日・終了日かどうかを判定
                            // _currentDateは既に設定されているので、直接使用
                            var isCurrentDateStartDate = _currentDate.Date == _wbsItem.StartDate.Date;
                            var isCurrentDateEndDate = _currentDate.Date == _wbsItem.EndDate.Date;
                            
                                            // デバッグ情報を追加
                // System.Diagnostics.Debug.WriteLine($"  日付計算詳細: _startDate={_startDate:yyyy/MM/dd}, _currentDate={_currentDate:yyyy/MM/dd}");
                // System.Diagnostics.Debug.WriteLine($"  比較対象: startDate={_wbsItem.StartDate:yyyy/MM/dd}, endDate={_wbsItem.EndDate:yyyy/MM/dd}");
                
                // System.Diagnostics.Debug.WriteLine($"  表示日付判定: _columnIndex={_columnIndex}, currentDate={_currentDate:yyyy/MM/dd}, startDate={_wbsItem.StartDate:yyyy/MM/dd}, endDate={_wbsItem.EndDate:yyyy/MM/dd}");
                // System.Diagnostics.Debug.WriteLine($"  判定結果: isStartDate={isCurrentDateStartDate}, isEndDate={isCurrentDateEndDate}");
                            
                            if (isCurrentDateStartDate)
                            {
                                // 開始日の日付（開始日変更のみ）
                                _isDraggingStart = true;
                                _isDraggingEnd = false;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else if (isCurrentDateEndDate)
                            {
                                // 終了日の日付（終了日変更のみ）
                                _isDraggingStart = false;
                                _isDraggingEnd = true;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else
                            {
                                // 開始日・終了日以外の日付（期間移動のみ）
                                _isDraggingStart = false;
                                _isDraggingEnd = false;
                                _isDraggingMove = true;
                                Cursor = Cursors.SizeAll;
                            }
                        }
                        else
                        {
                            // 通常のセル判定
                            if (clickedColumn == 0)
                            {
                                // 左端のセル（開始日変更）
                                _isDraggingStart = true;
                                _isDraggingEnd = false;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else if (clickedColumn == 2)
                            {
                                // 右端のセル（終了日変更）
                                _isDraggingStart = false;
                                _isDraggingEnd = true;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else
                            {
                                // 中央のセル（期間移動）
                                _isDraggingStart = false;
                                _isDraggingEnd = false;
                                _isDraggingMove = true;
                                Cursor = Cursors.SizeAll;
                            }
                        }
                        
                    }
                    else
                    {
                        // タスク期間外の列の場合でも、タスクが表示されている列であれば拡張ドラッグ判定を適用
                        if (dateColumnIndex >= 0 && dateColumnIndex < _totalColumns - TASK_INFO_COLUMN_COUNT)
                        {
                            // セル単位での判定に変更
                            var clickedColumn = Grid.GetColumn(e.OriginalSource as UIElement);
                            
                            if (clickedColumn == 0)
                            {
                                // 左端のセル（開始日変更）
                                _isDraggingStart = true;
                                _isDraggingEnd = false;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else if (clickedColumn == 2)
                            {
                                // 右端のセル（終了日変更）
                                _isDraggingStart = false;
                                _isDraggingEnd = true;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else
                            {
                                // 中央のセル（期間移動）
                                _isDraggingStart = false;
                                _isDraggingEnd = false;
                                _isDraggingMove = true;
                                Cursor = Cursors.SizeAll;
                            }
                        }
                        else
                        {
                            // 従来の位置ベース判定
                            // セル単位での判定に変更
                            var clickedColumn = Grid.GetColumn(e.OriginalSource as UIElement);
                            
                            if (clickedColumn == 0)
                            {
                                // 左端のセル（開始日変更）
                                _isDraggingStart = true;
                                _isDraggingEnd = false;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else if (clickedColumn == 2)
                            {
                                // 右端のセル（終了日変更）
                                _isDraggingStart = false;
                                _isDraggingEnd = true;
                                _isDraggingMove = false;
                                Cursor = Cursors.SizeWE;
                            }
                            else
                            {
                                // 中央のセル（期間移動）
                                _isDraggingStart = false;
                                _isDraggingEnd = false;
                                _isDraggingMove = true;
                                Cursor = Cursors.SizeAll;
                            }
                        }
                    }
                }
                else
                {
                    return; // 表示範囲外の列は処理しない
                }
            }

            // ドラッグ開始時に即座に視覚的フィードバックを表示
            if (_isDraggingStart)
            {
                UpdateVisualFeedback();
            }
            else if (_isDraggingEnd)
            {
                UpdateVisualFeedback();
            }
            else if (_isDraggingMove)
            {
                UpdateVisualFeedback(); // 期間移動用
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
                UpdateVisualFeedback();

                // デバッグ情報
                if (Math.Abs(roundedDaysDelta) > 0)
                {
                    var direction = roundedDaysDelta > 0 ? "右方向" : "左方向";
                    var taskDuration = (_originalEndDate - newStartDate).TotalDays;
                    // System.Diagnostics.Debug.WriteLine($"開始日変更プレビュー: {_originalStartDate:yyyy/MM/dd} -> {newStartDate:yyyy/MM/dd} (差分: {roundedDaysDelta}セル, {direction}, タスク期間: {taskDuration:F1}日)");
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
                UpdateVisualFeedback();

                // デバッグ情報
                if (Math.Abs(roundedDaysDelta) > 0)
                {
                    var direction = roundedDaysDelta > 0 ? "右方向" : "左方向";
                    var taskDuration = (newEndDate - _originalStartDate).TotalDays;
                    // System.Diagnostics.Debug.WriteLine($"終了日変更プレビュー: {_originalEndDate:yyyy/MM/dd} -> {newEndDate:yyyy/MM/dd} (差分: {roundedDaysDelta}セル, {direction}, タスク期間: {taskDuration:F1}日)");
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
                UpdateVisualFeedback(); // 期間移動用のフィードバック

                // デバッグ情報
                if (Math.Abs(roundedDaysDelta) > 0)
                {
                    var direction = roundedDaysDelta > 0 ? "右方向" : "左方向";
                    var taskDuration = (newEndDate - newStartDate).TotalDays;
                    // System.Diagnostics.Debug.WriteLine($"期間移動プレビュー: 開始日={newStartDate:yyyy/MM/dd}, 終了日={newEndDate:yyyy/MM/dd} (差分: {roundedDaysDelta}セル, {direction}, タスク期間: {taskDuration:F1}日)");
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
                            // DataContextの取得方法を改善
                            var viewModel = GetWbsViewModel();
                            if (viewModel != null)
                            {
                                // UIスレッドで非同期処理を実行（エラーハンドリングを強化）
                                _ = Dispatcher.BeginInvoke(async () =>
                                {
                                    try
                                    {
                                        // 日付変更の監視が有効かチェック
                                        if (viewModel.IsDateChangeWatchingEnabled)
                                        {
                                            await viewModel.UpdateTaskScheduleAsync(_wbsItem, oldStartDate, oldEndDate);
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine("日付変更の監視が無効のため、更新処理をスキップしました");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"日付変更の更新処理でエラー: {ex.Message}");
                                        System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
                                    }
                                }, System.Windows.Threading.DispatcherPriority.Normal);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("WbsViewModelの取得に失敗しました");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ViewModel取得でエラー: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
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
        private void UpdateVisualFeedback()
        {
            // ドラッグフラグに基づいて適切なフィードバックを表示
            if (_isDraggingMove)
            {
                // 期間移動中のフィードバック：透明度のみ変更
                Opacity = 1.0; // ドラッグ中は完全に不透明
            }
            else if (_isDraggingStart)
            {
                // 開始日変更ドラッグ中のフィードバック：透明度のみ変更
                Opacity = 1.0; // ドラッグ中は完全に不透明
            }
            else if (_isDraggingEnd)
            {
                // 終了日変更ドラッグ中のフィードバック：透明度のみ変更
                Opacity = 1.0; // ドラッグ中は完全に不透明
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
                // Gridの子要素が存在しない場合は、背景色のみ変更
                if (grid.Children.Count == 0)
                {
                    return;
                }
                
                foreach (UIElement child in grid.Children)
                {
                    var column = Grid.GetColumn(child);
                    
                    if (column == 0) // 左端の列
                    {
                        if (child is TextBlock textBlock)
                        {
                            textBlock.Background = backgroundBrush;
                            textBlock.Foreground = foregroundBrush;
                        }
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
                // Gridの子要素が存在しない場合は、背景色のみ変更
                if (grid.Children.Count == 0)
                {
                    return;
                }
                
                foreach (UIElement child in grid.Children)
                {
                    var column = Grid.GetColumn(child);
                    
                    if (column == 2) // 右端の列
                    {
                        if (child is TextBlock textBlock)
                        {
                            textBlock.Background = backgroundBrush;
                            textBlock.Foreground = foregroundBrush;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 全てのセルのTextBlockの色を変更
        /// </summary>
        private void UpdateAllCellsColors(Brush backgroundBrush, Brush foregroundBrush)
        {
            if (Child is Grid grid)
            {
                // 全てのセルのTextBlockの色を変更
                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBlock textBlock)
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
        /// 全てのセルのTextBlockの色を元に戻す
        /// </summary>
        private void ResetAllCellsColors()
        {
            if (Child is Grid grid)
            {
                // 全てのセルのTextBlockの色を元に戻す
                foreach (UIElement child in grid.Children)
                {
                    if (child is TextBlock textBlock)
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
            // 透明度を元に戻す（日付に応じた透明度を適用）
            if (_currentDate != default)
            {
                if (RedmineClient.Services.HolidayService.IsHoliday(_currentDate) ||
                    _currentDate.DayOfWeek == DayOfWeek.Saturday ||
                    _currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    Opacity = 0.3; // 土日祝は透明度を下げる
                }
                else
                {
                    Opacity = 0.8; // 平日は通常の透明度
                }
            }
            else
            {
                Opacity = 0.8; // デフォルト値
            }
            
            if (_isDraggingMove)
            {
                // 期間移動中だった場合は、背景色と罫線も元に戻す
                // 背景色をクリアしてWbsPage.xaml.csの設定を適用
                ClearValue(BackgroundProperty);
                BorderBrush = Brushes.Blue; // 元の青色の罫線
                BorderThickness = new Thickness(1); // 元の細い罫線
                
                // 全てのセルのTextBlockの色を元に戻す
                ResetAllCellsColors();
            }
            else if (_isDraggingStart)
            {
                // 開始日変更ドラッグ中だった場合は、背景色と罫線も元に戻す
                // 背景色をクリアしてWbsPage.xaml.csの設定を適用
                ClearValue(BackgroundProperty);
                BorderBrush = Brushes.Blue; // 元の青色の罫線
                
                // 全てのセルのTextBlockの色を元に戻す
                ResetAllCellsColors();
            }
            else if (_isDraggingEnd)
            {
                // 終了日変更ドラッグ中だった場合は、背景色と罫線も元に戻す
                // 背景色をクリアしてWbsPage.xaml.csの設定を適用
                ClearValue(BackgroundProperty);
                BorderBrush = Brushes.Blue; // 元の青色の罫線
                
                // 全てのセルのTextBlockの色を元に戻す
                ResetAllCellsColors();
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

            // 現在のDraggableTaskBorderが表示している日付で判定
            // _currentDateは既に設定されているので、直接使用
            var isCurrentDateStartDate = _currentDate.Date == _wbsItem.StartDate.Date;
            var isCurrentDateEndDate = _currentDate.Date == _wbsItem.EndDate.Date;

            // デバッグ情報を追加
            // System.Diagnostics.Debug.WriteLine($"  ツールチップ日付計算: _startDate={_startDate:yyyy/MM/dd}, _currentDate={_currentDate:yyyy/MM/dd}");
            // System.Diagnostics.Debug.WriteLine($"  ツールチップ比較: startDate={_wbsItem.StartDate:yyyy/MM/dd}, endDate={_wbsItem.EndDate:yyyy/MM/dd}");

            if (isCurrentDateStartDate)
            {
                // 開始日の日付
                ToolTip = $"ドラッグして開始日（{_wbsItem.StartDate:yyyy/MM/dd}）を変更";
            }
            else if (isCurrentDateEndDate)
            {
                // 終了日の日付
                ToolTip = $"ドラッグして終了日（{_wbsItem.EndDate:yyyy/MM/dd}）を変更";
            }
            else
            {
                // 開始日・終了日以外の日付
                ToolTip = $"ドラッグして期間を移動";
            }
        }

        /// <summary>
        /// 日付から列インデックスを計算
        /// </summary>
        private int GetColumnIndexFromDate(DateTime date)
        {
            // 現在の日付（_startDate）からの差分日数を計算
            var daysDiff = (date - _startDate).TotalDays;

            // 列インデックスに変換（0ベース）し、タスク情報列数を加算
            var columnIndex = (int)Math.Round(daysDiff) + TASK_INFO_COLUMN_COUNT;

            // デバッグ情報を最小限に削減
            // System.Diagnostics.Debug.WriteLine($"GetColumnIndexFromDate: 日付={date:yyyy/MM/dd}, _startDate={_startDate:yyyy/MM/dd}, 差分={daysDiff:F1}日, 列={columnIndex}");

            // 範囲チェック
            if (columnIndex < TASK_INFO_COLUMN_COUNT) // タスク情報列数未満は許可しない
            {
                // System.Diagnostics.Debug.WriteLine($"  列インデックスがタスク情報列数未満のため調整: {columnIndex} -> {TASK_INFO_COLUMN_COUNT}");
                columnIndex = TASK_INFO_COLUMN_COUNT;
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
            // _startDateは表示開始日（列0の日付）なので、タスク情報列数を加算して実際の列インデックスに変換

            // タスクの開始日が現在表示されている最初の日付（列0）から何日後か
            var startDaysFromFirst = (_wbsItem.StartDate - _startDate).TotalDays;
            var startColumn = (int)Math.Round(startDaysFromFirst) + TASK_INFO_COLUMN_COUNT; // タスク情報列数を加算

            // タスクの終了日が現在表示されている最初の日付（列0）から何日後か
            var endDaysFromFirst = (_wbsItem.EndDate - _startDate).TotalDays;
            var endColumn = (int)Math.Round(endDaysFromFirst) + TASK_INFO_COLUMN_COUNT; // タスク情報列数を加算

            // 範囲チェック（負の値や上限を超える値も許可）
            // タスクが表示範囲外にある場合でも、適切なドラッグ処理を可能にする
            if (startColumn < TASK_INFO_COLUMN_COUNT) startColumn = TASK_INFO_COLUMN_COUNT; // タスク情報列数未満は許可しない
            if (startColumn > _totalColumns - 1) startColumn = _totalColumns - 1; // 最大値制限
            if (endColumn < TASK_INFO_COLUMN_COUNT) endColumn = TASK_INFO_COLUMN_COUNT; // タスク情報列数未満は許可しない
            if (endColumn > _totalColumns - 1) endColumn = _totalColumns - 1; // 最大値制限

                            // デバッグ情報を有効化
                // System.Diagnostics.Debug.WriteLine($"GetActualTaskColumns: 開始日列={startColumn}, 終了日列={endColumn}");
                // System.Diagnostics.Debug.WriteLine($"  詳細計算: 開始日={_wbsItem.StartDate:yyyy/MM/dd}, 基準日={_startDate:yyyy/MM/dd}, 開始日差分={startDaysFromFirst:F1}日, 終了日差分={endDaysFromFirst:F1}日");

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

        /// <summary>
        /// WbsViewModelを安全に取得する
        /// </summary>
        /// <returns>WbsViewModel、取得できない場合はnull</returns>
        private WbsViewModel? GetWbsViewModel()
        {
            try
            {
                // まず直接のDataContextを試行
                if (DataContext is WbsViewModel directViewModel)
                {
                    return directViewModel;
                }

                // DataContextがWbsViewModelでない場合、親要素を辿って検索
                var parent = VisualTreeHelper.GetParent(this);
                while (parent != null)
                {
                    if (parent is FrameworkElement frameworkElement)
                    {
                        if (frameworkElement.DataContext is WbsViewModel parentViewModel)
                        {
                            return parentViewModel;
                        }
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }

                // 最後に、Application.Current.MainWindowから検索
                if (Application.Current?.MainWindow?.DataContext is WbsViewModel mainViewModel)
                {
                    return mainViewModel;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WbsViewModel取得でエラー: {ex.Message}");
                return null;
            }
        }
    }
}