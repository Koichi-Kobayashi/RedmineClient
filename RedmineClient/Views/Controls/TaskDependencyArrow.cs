using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RedmineClient.Models;

namespace RedmineClient.Views.Controls
{
    /// <summary>
    /// タスク間の依存関係を示す矢印を描画するコントロール
    /// </summary>
    public class TaskDependencyArrow : Canvas
    {
        private readonly List<Line> _arrows = new();
        private readonly List<Polygon> _arrowheads = new();
        
        // 矢印のスタイル設定
        private const double ARROW_STROKE_THICKNESS = 2.0;
        private const double ARROWHEAD_SIZE = 8.0;
        private static readonly Brush ARROW_BRUSH = Brushes.DarkBlue;
        private static readonly Brush ARROWHEAD_BRUSH = Brushes.DarkBlue;

        public TaskDependencyArrow()
        {
            // 背景を透明に設定
            Background = Brushes.Transparent;
            
            // マウスイベントを無効化（矢印は操作対象ではない）
            IsHitTestVisible = false;
        }

        /// <summary>
        /// 依存関係の矢印を描画
        /// </summary>
        /// <param name="dependencies">依存関係のリスト（先行タスクID -> 後続タスクID）</param>
        /// <param name="taskPositions">タスクの位置情報（タスクID -> 位置）</param>
        /// <param name="columnWidth">列の幅</param>
        /// <param name="rowHeight">行の高さ</param>
        /// <param name="taskInfoColumnCount">タスク情報列数</param>
        /// <param name="startDate">表示開始日</param>
        public void DrawDependencies(
            Dictionary<string, List<string>> dependencies,
            Dictionary<string, (Point position, DateTime startDate, DateTime endDate)> taskPositions,
            double columnWidth,
            double rowHeight,
            int taskInfoColumnCount,
            DateTime startDate)
        {
            // 既存の矢印をクリア
            ClearArrows();

            foreach (var dependency in dependencies)
            {
                string predecessorId = dependency.Key;
                foreach (string successorId in dependency.Value)
                {
                    if (taskPositions.ContainsKey(predecessorId) && taskPositions.ContainsKey(successorId))
                    {
                        var predecessor = taskPositions[predecessorId];
                        var successor = taskPositions[successorId];

                        // 先行タスクの終了日から後続タスクの開始日まで矢印を描画
                        DrawDependencyArrow(
                            predecessor, 
                            successor, 
                            columnWidth, 
                            rowHeight, 
                            taskInfoColumnCount, 
                            startDate);
                    }
                }
            }
        }

        /// <summary>
        /// 個別の依存関係矢印を描画
        /// </summary>
        private void DrawDependencyArrow(
            (Point position, DateTime startDate, DateTime endDate) predecessor,
            (Point position, DateTime startDate, DateTime endDate) successor,
            double columnWidth,
            double rowHeight,
            int taskInfoColumnCount,
            DateTime startDate)
        {
            // 先行タスクの終了日の位置を計算
            var predecessorEndDate = predecessor.endDate;
            var predecessorEndColumn = GetColumnFromDate(predecessorEndDate, startDate, taskInfoColumnCount);
            var predecessorEndX = predecessor.position.X + (predecessorEndColumn * columnWidth) + (columnWidth / 2);
            var predecessorEndY = predecessor.position.Y + (rowHeight / 2);

            // 後続タスクの開始日の位置を計算
            var successorStartDate = successor.startDate;
            var successorStartColumn = GetColumnFromDate(successorStartDate, startDate, taskInfoColumnCount);
            var successorStartX = successor.position.X + (successorStartColumn * columnWidth) + (columnWidth / 2);
            var successorStartY = successor.position.Y + (rowHeight / 2);

            // 矢印の線を描画
            var arrowLine = new Line
            {
                X1 = predecessorEndX,
                Y1 = predecessorEndY,
                X2 = successorStartX,
                Y2 = successorStartY,
                Stroke = ARROW_BRUSH,
                StrokeThickness = ARROW_STROKE_THICKNESS,
                StrokeDashArray = new DoubleCollection { 5, 3 } // 破線スタイル
            };

            // 矢印の先端を描画
            var arrowhead = CreateArrowhead(successorStartX, successorStartY, predecessorEndX, predecessorEndY);

            // コントロールに追加
            Children.Add(arrowLine);
            Children.Add(arrowhead);

            // リストに保存
            _arrows.Add(arrowLine);
            _arrowheads.Add(arrowhead);
        }

        /// <summary>
        /// 日付から列インデックスを計算
        /// </summary>
        private int GetColumnFromDate(DateTime date, DateTime startDate, int taskInfoColumnCount)
        {
            var daysDiff = (date - startDate).TotalDays;
            return Math.Max(0, (int)Math.Round(daysDiff)) + taskInfoColumnCount;
        }

        /// <summary>
        /// 矢印の先端を作成
        /// </summary>
        private Polygon CreateArrowhead(double endX, double endY, double startX, double startY)
        {
            // 矢印の方向を計算
            var angle = Math.Atan2(endY - startY, endX - startX);
            
            // 矢印の先端のポイントを計算
            var arrowheadPoints = new PointCollection();
            
            // 矢印の先端から少し手前の位置を基準点とする
            var baseX = endX - (ARROWHEAD_SIZE * Math.Cos(angle));
            var baseY = endY - (ARROWHEAD_SIZE * Math.Sin(angle));
            
            // 矢印の先端の3つのポイントを計算
            var leftAngle = angle + Math.PI / 6; // 30度
            var rightAngle = angle - Math.PI / 6; // 30度
            
            arrowheadPoints.Add(new Point(endX, endY));
            arrowheadPoints.Add(new Point(
                baseX + (ARROWHEAD_SIZE * Math.Cos(leftAngle)),
                baseY + (ARROWHEAD_SIZE * Math.Sin(leftAngle))));
            arrowheadPoints.Add(new Point(
                baseX + (ARROWHEAD_SIZE * Math.Cos(rightAngle)),
                baseY + (ARROWHEAD_SIZE * Math.Sin(rightAngle))));

            return new Polygon
            {
                Points = arrowheadPoints,
                Fill = ARROWHEAD_BRUSH
            };
        }

        /// <summary>
        /// 全ての矢印をクリア
        /// </summary>
        public void ClearArrows()
        {
            foreach (var arrow in _arrows)
            {
                Children.Remove(arrow);
            }
            foreach (var arrowhead in _arrowheads)
            {
                Children.Remove(arrowhead);
            }
            
            _arrows.Clear();
            _arrowheads.Clear();
        }

        /// <summary>
        /// 特定のタスクに関連する矢印をハイライト
        /// </summary>
        /// <param name="taskId">ハイライトするタスクID</param>
        /// <param name="isHighlighted">ハイライトするかどうか</param>
        public void HighlightTaskArrows(string taskId, bool isHighlighted)
        {
            // 実装予定：特定のタスクに関連する矢印の色や太さを変更
        }
    }
}
