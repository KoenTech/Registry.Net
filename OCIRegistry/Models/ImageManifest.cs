namespace OCIRegistry.Models
{
    public class ImageManifest
    {
        public int schemaVersion { get; set; }
        public string? mediaType { get; set; }
        public required Config config { get; set; }
        public required List<Layer> layers { get; set; }
        public Dictionary<string, string> annotations { get; set; } = new();
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
        // Docker media types
        public const string DockerManifest = "application/vnd.docker.distribution.manifest.v2+json";
        public const string DockerManifestList = "application/vnd.docker.distribution.manifest.list.v2+json";
        public const string DockerImage = "application/vnd.docker.container.image.v1+json";
        public const string DockerLayer = "application/vnd.docker.image.rootfs.diff.tar.gzip";

        // OCI media types
        public const string Descriptor = "application/vnd.oci.descriptor.v1+json";
        public const string LayoutHeader = "application/vnd.oci.layout.header.v1+json";
        public const string ImageIndex = "application/vnd.oci.image.index.v1+json";
        public const string ImageManifest = "application/vnd.oci.image.manifest.v1+json";
        public const string ImageConfig = "application/vnd.oci.image.config.v1+json";
        public const string LayerTar = "application/vnd.oci.image.layer.v1.tar";
        public const string LayerTarGzip = "application/vnd.oci.image.layer.v1.tar+gzip";
        public const string NonDistributableLayerTar = "application/vnd.oci.image.layer.nondistributable.v1.tar";
        public const string NonDistributableLayerTarGzip = "application/vnd.oci.image.layer.nondistributable.v1.tar+gzip";
    }
}
