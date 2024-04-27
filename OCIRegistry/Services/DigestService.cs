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
            lock (_sha256) // TODO: Have each upload session use its own instance of SHA256
            {
                var hash = _sha256.ComputeHash(stream);
                return $"sha256:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}";
            }
        }

        public bool CheckDigest(Stream stream, string digest)
        {
            var hash = _sha256.ComputeHash(stream);
            return $"sha256:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}" == digest.ToLowerInvariant();
        }
    }
}
