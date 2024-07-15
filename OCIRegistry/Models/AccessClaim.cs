using System.Text.Json.Serialization;

namespace OCIRegistry.Models
{
    public class AccessClaim
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }
        [JsonPropertyName("name")]
        public required string Name { get; set; }
        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new();
    }
}
