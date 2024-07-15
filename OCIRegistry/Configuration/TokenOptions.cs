namespace OCIRegistry.Configuration
{
    public class TokenOptions
    {
        public const string SectionName = "JWT";

        public string? Secret { get; set; }
        public required string Issuer { get; set; } = "registry-auth";
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan RefreshLifetime { get; set; } = TimeSpan.FromDays(3);

    }
}
