namespace OCIRegistry.Helpers
{
    public static class RepoHelper
    {
        public static string RepoName(string? prefix, string name)
        {
            return $"{(prefix != null ? $"{prefix}/" : "")}{name}";
        }
    }
}
