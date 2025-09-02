namespace RedmineClient.Models
{
    public class DependencyLink
    {
        public string PredId { get; set; } = string.Empty;
        public int LagDays { get; set; }
        public LinkType Type { get; set; } = LinkType.FS;
        public override string ToString() => $"{PredId}:{LagDays}:{Type}";
        public static bool TryParse(string s, out DependencyLink link)
        {
            link = new DependencyLink();
            try
            {
                var parts = s.Split(':');
                link.PredId = parts[0].Trim();
                link.LagDays = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                if (parts.Length > 2 && System.Enum.TryParse<LinkType>(parts[2], out var t)) link.Type = t;
                return true;
            }
            catch { return false; }
        }
    }
}


