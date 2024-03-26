using System.Security.Cryptography;

namespace OCIRegistry.Services
{
    public class DigestService
    {
        private SHA256 _sha256;

        public DigestService()
        {
            _sha256 = SHA256.Create();
        }

        public string CreateDigest(Stream stream)
        {
            var hash = _sha256.ComputeHash(stream);
            return $"sha256:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}";
        }

        public bool ChechDigest(Stream stream, string digest)
        {
            var hash = _sha256.ComputeHash(stream);
            return $"sha256:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}" == digest.ToLowerInvariant();
        }
    }
}
