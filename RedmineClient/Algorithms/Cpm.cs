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
                foreach (var link in t.Preds)
                {
                    switch (link.Type)
                    {
                        case LinkType.FS:
                            es = System.Math.Max(es, res.EF[link.PredId] + link.LagDays); break;
                        case LinkType.SS:
                            es = System.Math.Max(es, res.ES[link.PredId] + link.LagDays); break;
                        case LinkType.FF:
                            es = System.Math.Max(es, res.EF[link.PredId] + link.LagDays - t.Duration); break;
                        case LinkType.SF:
                            es = System.Math.Max(es, res.ES[link.PredId] + link.LagDays - t.Duration); break;
                    }
                }
                if (t.StartMin.HasValue) es = System.Math.Max(es, t.StartMin.Value);
                res.ES[id] = es < 0 ? 0 : es;
                res.EF[id] = res.ES[id] + t.Duration;
            }

            var succ = new Dictionary<string, List<DependencyLink>>();
            foreach (var t in tasks)
            foreach (var link in t.Preds)
            {
                if (!succ.TryGetValue(link.PredId, out var list)) succ[link.PredId] = list = new();
                list.Add(new DependencyLink { PredId = t.WbsNo, LagDays = link.LagDays, Type = link.Type });
            }

            int projectFinish = topoOrder.Max(id => res.EF[id]);
            var LS = new Dictionary<string, int>();
            var LF = new Dictionary<string, int>();
            foreach (var id in topoOrder) { LS[id] = int.MaxValue/4; LF[id] = projectFinish; }

            foreach (var id in ((IEnumerable<string>)topoOrder).Reverse())
            {
                if (succ.TryGetValue(id, out var list))
                {
                    foreach (var s in list)
                    {
                        switch (s.Type)
                        {
                            case LinkType.FS: LF[id] = System.Math.Min(LF[id], LS[s.PredId] - s.LagDays); break;
                            case LinkType.SS: LS[id] = System.Math.Min(LS[id], LS[s.PredId] - s.LagDays); break;
                            case LinkType.FF: LF[id] = System.Math.Min(LF[id], LF[s.PredId] - s.LagDays); break;
                            case LinkType.SF: LS[id] = System.Math.Min(LS[id], LF[s.PredId] - s.LagDays); break;
                        }
                    }
                }
                int dur = byId[id].Duration;
                if (LS[id] == int.MaxValue/4) LS[id] = LF[id] - dur; else LF[id] = System.Math.Min(LF[id], LS[id] + dur);
            }

            foreach (var id in topoOrder) { res.LS[id] = LS[id]; res.LF[id] = LF[id]; }
            return res;
        }
    }
}


