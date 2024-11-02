using System.Text.Json.Serialization;

namespace RedmineClient.Api
{
    public class IssuePostData : BasePostData
    {
        [JsonPropertyName("issue")]
        public IssuePostMainData Issue { get; set; }

        public override string GetUrl()
        {
            return "/issues.json";
        }
    }

    public class IssuePostMainData
    {
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }
    }
}
