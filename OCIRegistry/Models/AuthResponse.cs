using System.Text.Json.Serialization;

namespace OCIRegistry.Models
{
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")]
        public uint ExpiresIn { get; set; }
        [JsonPropertyName("issued_at")]
        public DateTime IssuedAt { get; set; }
    }
}
