using Microsoft.Extensions.Caching.Memory;
using OCIRegistry.Models.Database;
using Serilog;

namespace OCIRegistry.Services
{
    public class BlobUpload
    {
        public Guid UUID { get; set; }
        public DateTime Started { get; set; }
        public string RepoName { get; }
        public long Position;
        private string _path;

        public BlobUpload(string repoName)
        {
            Started = DateTime.Now;
            UUID = Guid.NewGuid();
            Position = 0;
            _path = Path.Combine("./Blobs/upload", UUID.ToString()); // TODO: Configurable upload cache location
            RepoName = repoName;
        }

        public bool ValidateRange(long? position)
        {
            if (Position == 0 && position == 0) return true;
            return position == Position+1;
        }

        public async Task<long> AppendChunk(Stream chunk)
        {
            long firstLength = Position;
            using (var file = new FileStream(_path, FileMode.Append, FileAccess.Write))
            {
                await chunk.CopyToAsync(file);
                Position = file.Length-1;
            }

            return Position;
        }

        public Stream OpenContent()
        {
            return File.OpenRead(_path);
        }

        public void Cleanup()
        {
            File.Delete(_path);
        }
    }

    public class BlobUploadService : BackgroundService
    {
        private readonly ILogger<BlobUploadService> _logger;
        private Dictionary<Guid, BlobUpload> _uploads = new();

        public BlobUploadService(ILogger<BlobUploadService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Create a new upload session for a given repository
        /// </summary>
        /// <param name="repo"></param>
        /// <returns></returns>
        public Guid StartUpload(string repo)
        {
            var upload = new BlobUpload(repo);
            _uploads.Add(upload.UUID, upload);
            _logger.LogDebug("Created new upload session for {repo} with UUID {uuid}", repo, upload.UUID);
            return upload.UUID;
        }

        /// <summary>
        /// Get an upload session by UUID and repository name
        /// </summary>
        /// <param name="uuid">UUID of the session</param>
        /// <param name="repo">Current repository</param>
        /// <returns>Upload session</returns>
        /// <exception cref="InvalidUploadException"></exception>
        public BlobUpload GetUpload(Guid uuid, string repo)
        {
            if (!_uploads.TryGetValue(uuid, out var upload) || upload.RepoName != repo)
            {
                throw new InvalidUploadException("No upload session with this UUID exists or the repository does not match");
            }

            return upload;
        }

        /// <summary>
        /// Cleanup an upload session
        /// </summary>
        /// <param name="uuid"></param>
        public void CleanupUpload(Guid uuid)
        {
            _uploads[uuid].Cleanup();
            _uploads.Remove(uuid);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                _logger.LogTrace("Cleaning up expired upload sessions (TODO)");

                foreach (var upload in _uploads.Values)
                {
                    if (DateTime.Now - upload.Started > TimeSpan.FromMinutes(30))
                    {
                        try
                        {
                            upload.Cleanup();
                            _uploads.Remove(upload.UUID);
                        }
                        catch (IOException e)
                        {
                            _logger.LogWarning(e, $"Failed to clean up upload session {upload.UUID} with message: {e.Message}");
                        }
                    }
                }

                _logger.LogTrace("Finished cleaning up expired upload sessions");
            }
        }
    }

    public class InvalidUploadException : Exception
    {
        public InvalidUploadException() : base("Invalid upload session") { }
        public InvalidUploadException(string description) : base(description) { }
    }
}
