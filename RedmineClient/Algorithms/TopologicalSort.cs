using System;
using System.Collections.Generic;
using System.Linq;

namespace RedmineClient.Algorithms
{
    public static class TopologicalSort
    {
        public static List<string> Run(IEnumerable<string> nodes, Func<string, string, bool> edge)
        {
            var nodeList = nodes.Distinct().ToList();
            var inDegree = nodeList.ToDictionary(n => n, _ => 0);
            foreach (var v in nodeList)
            {
                foreach (var u in nodeList)
                {
                    if (!Equals(u, v) && edge(u, v)) inDegree[v]++;
                }
            }

            var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var result = new List<string>();

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                result.Add(u);
                foreach (var v in nodeList)
                {
                    if (!Equals(u, v) && edge(u, v))
                    {
                        inDegree[v]--;
                        if (inDegree[v] == 0) queue.Enqueue(v);
                    }
                }
            }

            return result;
        }
    }
}



