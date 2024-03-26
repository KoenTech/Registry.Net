using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OCIRegistry.Models.Database
{
    public class Blob
    {
        [Key]
        public required string Id { get; set; }
        public required ulong Size { get; set; }
        public List<Manifest> Manifests { get; set; } = new();
    }

    public class Manifest
    {
        [Key]
        public ulong Id { get; set; }
        public required string Digest { get; set; }
        public required byte[] Content { get; set; }
        public List<Blob> Blobs { get; set; } = new();
        public required ulong RepositoryId { get; set; }
        public required Repository Repository { get; set; }
    }

    public class Tag
    {
        [Key]
        public ulong Id { get; set; }
        public required string Name { get; set; }
        public required ulong ManifestId { get; set; }
        public required Manifest Manifest { get; set; }
    }

    public class Repository
    {
        [Key]
        public ulong Id { get; set; }
        public required string Name { get; set; }
        public List<Manifest> Manifests { get; set; } = new();
    }
}
