using System;
using System.Collections.Generic;
using System.Linq;
using RedmineClient.Models;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 依存関係の矢印を管理するヘルパークラス
    /// </summary>
    public static class DependencyArrowHelper
    {
        /// <summary>
        /// WbsItemのリストから依存関係の辞書を生成
        /// </summary>
        /// <param name="wbsItems">WbsItemのリスト</param>
        /// <returns>先行タスクID -> 後続タスクIDのリストの辞書</returns>
        public static Dictionary<string, List<string>> BuildDependencyDictionary(IEnumerable<WbsItem> wbsItems)
        {
            var dependencies = new Dictionary<string, List<string>>();

            foreach (var item in wbsItems)
            {
                if (item.HasPredecessors)
                {
                    foreach (var predecessor in item.Predecessors)
                    {
                        if (!dependencies.ContainsKey(predecessor.Id))
                        {
                            dependencies[predecessor.Id] = new List<string>();
                        }
                        
                        if (!dependencies[predecessor.Id].Contains(item.Id))
                        {
                            dependencies[predecessor.Id].Add(item.Id);
                        }
                    }
                }
            }

            return dependencies;
        }

        /// <summary>
        /// 特定のタスクに関連する依存関係を取得
        /// </summary>
        /// <param name="taskId">対象タスクID</param>
        /// <param name="dependencies">依存関係辞書</param>
        /// <returns>関連する依存関係の辞書</returns>
        public static Dictionary<string, List<string>> GetRelatedDependencies(
            string taskId, 
            Dictionary<string, List<string>> dependencies)
        {
            var related = new Dictionary<string, List<string>>();

            // 対象タスクが先行タスクとして含まれる依存関係
            if (dependencies.ContainsKey(taskId))
            {
                related[taskId] = dependencies[taskId];
            }

            // 対象タスクが後続タスクとして含まれる依存関係
            foreach (var dependency in dependencies)
            {
                if (dependency.Value.Contains(taskId))
                {
                    if (!related.ContainsKey(dependency.Key))
                    {
                        related[dependency.Key] = new List<string>();
                    }
                    related[dependency.Key].Add(taskId);
                }
            }

            return related;
        }

        /// <summary>
        /// 依存関係の循環参照をチェック
        /// </summary>
        /// <param name="dependencies">依存関係辞書</param>
        /// <returns>循環参照がある場合はtrue</returns>
        public static bool HasCircularDependencies(Dictionary<string, List<string>> dependencies)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var taskId in dependencies.Keys)
            {
                if (IsCyclicUtil(taskId, dependencies, visited, recursionStack))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 循環参照チェックのユーティリティメソッド（深さ優先探索）
        /// </summary>
        private static bool IsCyclicUtil(
            string taskId, 
            Dictionary<string, List<string>> dependencies, 
            HashSet<string> visited, 
            HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(taskId))
            {
                return true; // 循環参照を検出
            }

            if (visited.Contains(taskId))
            {
                return false; // 既に訪問済み
            }

            visited.Add(taskId);
            recursionStack.Add(taskId);

            if (dependencies.ContainsKey(taskId))
            {
                foreach (var successorId in dependencies[taskId])
                {
                    if (IsCyclicUtil(successorId, dependencies, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(taskId);
            return false;
        }

        /// <summary>
        /// 依存関係の階層レベルを計算
        /// </summary>
        /// <param name="dependencies">依存関係辞書</param>
        /// <returns>タスクID -> 階層レベルの辞書</returns>
        public static Dictionary<string, int> CalculateDependencyLevels(Dictionary<string, List<string>> dependencies)
        {
            var levels = new Dictionary<string, int>();
            var inDegree = new Dictionary<string, int>();

            // 入次数を初期化
            foreach (var taskId in dependencies.Keys)
            {
                inDegree[taskId] = 0;
                levels[taskId] = 0;
            }

            // 入次数を計算
            foreach (var dependency in dependencies.Values)
            {
                foreach (var successorId in dependency)
                {
                    if (inDegree.ContainsKey(successorId))
                    {
                        inDegree[successorId]++;
                    }
                }
            }

            // トポロジカルソートでレベルを計算
            var queue = new Queue<string>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }

            while (queue.Count > 0)
            {
                var currentTaskId = queue.Dequeue();
                var currentLevel = levels[currentTaskId];

                if (dependencies.ContainsKey(currentTaskId))
                {
                    foreach (var successorId in dependencies[currentTaskId])
                    {
                        inDegree[successorId]--;
                        levels[successorId] = Math.Max(levels[successorId], currentLevel + 1);

                        if (inDegree[successorId] == 0)
                        {
                            queue.Enqueue(successorId);
                        }
                    }
                }
            }

            return levels;
        }

        /// <summary>
        /// 依存関係の可視性を判定（画面に表示すべきかどうか）
        /// </summary>
        /// <param name="predecessor">先行タスク</param>
        /// <param name="successor">後続タスク</param>
        /// <param name="visibleDateRange">表示される日付範囲</param>
        /// <returns>表示すべき場合はtrue</returns>
        public static bool ShouldShowDependency(
            WbsItem predecessor, 
            WbsItem successor, 
            (DateTime start, DateTime end) visibleDateRange)
        {
            // 先行タスクの終了日と後続タスクの開始日が表示範囲内にあるかチェック
            var predecessorEndVisible = predecessor.EndDate >= visibleDateRange.start && 
                                      predecessor.EndDate <= visibleDateRange.end;
            var successorStartVisible = successor.StartDate >= visibleDateRange.start && 
                                      successor.StartDate <= visibleDateRange.end;

            // 少なくとも一方が表示範囲内にあれば矢印を表示
            return predecessorEndVisible || successorStartVisible;
        }
    }
}
