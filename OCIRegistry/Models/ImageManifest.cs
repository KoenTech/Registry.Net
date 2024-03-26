namespace OCIRegistry.Models
{
    public class ImageManifest
    {
        public int schemaVersion { get; set; }
        public required string mediaType { get; set; }
        public required Config config { get; set; }
        public required List<Layer> layers { get; set; }
    }

    public class Config
    {
        public required string mediaType { get; set; }
        public required string digest { get; set; }
        public ulong size { get; set; }
    }

    public class Layer
    {
        public required string mediaType { get; set; }
        public required string digest { get; set; }
        public ulong size { get; set; }
    }

    public struct MediaType
    {
        public const string Manifest = "application/vnd.docker.distribution.manifest.v2+json";
        public const string ManifestList = "application/vnd.docker.distribution.manifest.list.v2+json";
        public const string Image = "application/vnd.docker.container.image.v1+json";
        public const string Layer = "application/vnd.docker.image.rootfs.diff.tar.gzip";
    }
}
