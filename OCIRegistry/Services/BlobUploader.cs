using Microsoft.Extensions.Caching.Memory;

namespace OCIRegistry.Services
{
    // TODO: Range header validation
    // TODO: Configurable upload cache location
    public class BlobUploader
    {
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private string _path = "./Blobs/upload";

        public Guid StartUpload(string repo)
        {
            var guid = Guid.NewGuid();
            _cache.Set($"{repo}:{guid}", (ulong)0);
            return guid;
        }

        public bool UploadAllowed(string repo, Guid uuid)
        {
            return _cache.TryGetValue($"{repo}:{uuid}", out _);
        }

        public async Task<ulong> UploadChunk(string repo, Guid uuid, Stream chunk)
        {
            ulong size;
            using (var file = File.OpenWrite(Path.Combine(_path, uuid.ToString())))
            {
                await chunk.CopyToAsync(file);
                size = (ulong)file.Length;
            }

            _cache.Set($"{repo}:{uuid}", size);
            return size;
        }

        public Stream FinishUpload(string repo, Guid uuid)
        {
            var size = _cache.Get<ulong>($"{repo}:{uuid}");
            var path = Path.Combine(_path, uuid.ToString());

            return File.OpenRead(path);
        }

        public void CleanupUpload(string repo, Guid uuid)
        {
            _cache.Remove($"{repo}:{uuid}");
            File.Delete(Path.Combine(_path, uuid.ToString()));
        }
    }
}
