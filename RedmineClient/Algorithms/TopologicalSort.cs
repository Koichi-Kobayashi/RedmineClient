using System;
using System.Collections.Generic;
using System.Linq;

namespace RedmineClient.Algorithms
{
    public static class TopologicalSort
    {
        public static List<string> Run(IEnumerable<string> nodes, Func<string, string, bool> edge)
        {
            var nlist = nodes.ToList();
            var indeg = nlist.ToDictionary(x => x, x => 0);
            foreach (var v in nlist)
                foreach (var u in nlist)
                    if (edge(u, v)) indeg[v]++;

            var q = new Queue<string>(indeg.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var order = new List<string>();

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                order.Add(u);
                foreach (var v in nlist)
                {
                    if (edge(u, v))
                    {
                        indeg[v]--;
                        if (indeg[v] == 0) q.Enqueue(v);
                    }
                }
            }

            if (order.Count != nlist.Count)
                throw new InvalidOperationException("循環依存を検出しました（WBSの依存関係を確認してください）。");

            return order;
        }
    }
}


