namespace RedmineClient.XmlData
{
    public class RedmineHeaderAttribute : Attribute
    {
        private string? _headerName;

        public RedmineHeaderAttribute() { }

        public RedmineHeaderAttribute(string? headerName)
        {
            _headerName = headerName;
        }
    }
}
