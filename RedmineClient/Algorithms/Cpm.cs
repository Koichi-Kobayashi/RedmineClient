using System.Collections.Generic;
using System.Linq;
using RedmineClient.Models;

namespace RedmineClient.Algorithms
{
    public class CpmResult
    {
        public Dictionary<string, int> ES { get; } = new();
        public Dictionary<string, int> EF { get; } = new();
        public Dictionary<string, int> LS { get; } = new();
        public Dictionary<string, int> LF { get; } = new();
    }

    public static class Cpm
    {
        public static CpmResult Run(IReadOnlyList<WbsSampleTask> tasks, IReadOnlyList<string> topoOrder)
        {
            var byId = tasks.ToDictionary(t => t.WbsNo);
            var res = new CpmResult();

            foreach (var id in topoOrder)
            {
                var t = byId[id];
                int es = 0;
                foreach (var (p, lag) in t.Preds)
                {
                    es = System.Math.Max(es, res.EF[p] + lag);
                }
                res.ES[id] = es;
                res.EF[id] = es + t.Duration;
            }

            var succ = new Dictionary<string, List<(string succ, int lag)>>();
            foreach (var t in tasks)
            {
                foreach (var (p, lag) in t.Preds)
                {
                    if (!succ.TryGetValue(p, out var list)) succ[p] = list = new();
                    list.Add((t.WbsNo, lag));
                }
            }

            int projectFinish = topoOrder.Max(id => res.EF[id]);
            foreach (var id in ((IEnumerable<string>)topoOrder).Reverse())
            {
                int lf = succ.TryGetValue(id, out var list) && list.Count > 0
                    ? list.Min(s => res.LS[s.succ] - s.lag)
                    : projectFinish;
                res.LF[id] = lf;
                res.LS[id] = lf - byId[id].Duration;
            }

            return res;
        }
    }
}


