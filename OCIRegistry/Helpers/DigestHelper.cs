namespace OCIRegistry.Helpers
{
    public static class DigestHelper
    {
        public static bool IsDigest(string digest)
        {
            return digest.StartsWith("sha256:");
        }

        public static string ToHash(string digest)
        {
            return digest.Substring(7);
        }

        public static string ToDigest(string hash)
        {
            return $"sha256:{hash}";
        }
    }
}
