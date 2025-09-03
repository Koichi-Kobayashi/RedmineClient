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

            // Forward pass
            foreach (var id in topoOrder)
            {
                var t = byId[id];
                var es = t.StartMin;
                foreach (var p in t.Preds)
                {
                    if (res.EF.TryGetValue(p.PredId, out var predEf))
                    {
                        es = System.Math.Max(es, predEf + p.LagDays);
                    }
                }
                res.ES[id] = es;
                res.EF[id] = es + t.Duration;
            }

            // Backward pass
            var maxEf = res.EF.Values.DefaultIfEmpty(0).Max();
            for (int i = topoOrder.Count - 1; i >= 0; i--)
            {
                var id = topoOrder[i];
                var t = byId[id];
                int lf = maxEf;
                // 次タスク(後続)の最小LSを求める
                var successors = tasks.Where(x => x.Preds.Any(p => p.PredId == id)).Select(x => x.WbsNo).ToList();
                if (successors.Count > 0)
                {
                    int minLs = int.MaxValue;
                    foreach (var sId in successors)
                    {
                        if (res.LS.TryGetValue(sId, out var succLs))
                        {
                            minLs = System.Math.Min(minLs, succLs);
                        }
                    }
                    if (minLs != int.MaxValue) lf = minLs;
                }

                res.LF[id] = lf;
                res.LS[id] = lf - t.Duration;
            }

            return res;
        }
    }
}


