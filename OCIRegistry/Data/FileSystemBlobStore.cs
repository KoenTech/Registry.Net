using OCIRegistry.Helpers;
using OCIRegistry.Services;

namespace OCIRegistry.Data
{
    public class FileSystemBlobStore : IBlobStore
    {
        private readonly string _path;
        private readonly DigestService _digest;

        public FileSystemBlobStore(DigestService digest)
        {
            _path = "./Blobs"; // TODO: Use configuration
            _digest = digest;
        }

        public async Task<Stream> GetAsync(string digest)
        {
            var hash = DigestHelper.ToHash(digest);
            var path = Path.Combine(_path, hash.Substring(0, 2), hash);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            return File.OpenRead(path);
        }

        public async Task PutAsync(string digest, Stream stream)
        {
            var hash = DigestHelper.ToHash(digest);
            var dir = Path.Combine(_path, hash.Substring(0, 2));
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, hash);
            if (File.Exists(path))
            {
                return;
            }

            using (var fileStream = File.Create(path))
            {
                await stream.CopyToAsync(fileStream);
            }
            return;
        }

        public Task DeleteAsync(string digest)
        {
            var hash = DigestHelper.ToHash(digest);
            var path = Path.Combine(_path, hash.Substring(0, 2), hash);
            if (!File.Exists(path))
            {
                return Task.CompletedTask;
            }

            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string digest)
        {
            var hash = DigestHelper.ToHash(digest);
            var path = Path.Combine(_path, hash.Substring(0, 2), hash);
            return Task.FromResult(File.Exists(path));
        }
    }
}
