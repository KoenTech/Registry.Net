using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCIRegistry.Data;
using OCIRegistry.Helpers;
using OCIRegistry.Models;
using OCIRegistry.Models.Database;
using OCIRegistry.Services;
using System;

namespace OCIRegistry.Controllers
{
    [Route("v2/{prefix}/{name:required}/blobs"), Route("v2/{name:required}/blobs")]
    [ApiController]
    public class BlobsController : ControllerBase
    {
        private readonly DigestService _digest;
        private readonly AppDbContext _db;
        private readonly IBlobStore _store;
        private readonly BlobUploadService _uploadService;
        private readonly ILogger<BlobsController> _logger;

        public BlobsController(DigestService digest, AppDbContext db, IBlobStore store, BlobUploadService uploadService, ILogger<BlobsController> logger)
        {
            _digest = digest;
            _db = db;
            _store=store;
            _uploadService = uploadService;
            _logger=logger;
        }

        [HttpGet("{reference}")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> GetBlob([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string mediaType = Request.Headers.Accept.FirstOrDefault() ?? MediaType.LayerTarGzip;
            string repo = RepoHelper.RepoName(prefix, name);
            var blob = await _db.Blobs.Where(x => x.Id == reference).Where(x => x.Repositories.Any(c => c.Name == repo)).FirstOrDefaultAsync();
            if (blob is null) return NotFound();

            var file = await _store.GetAsync(blob.Id);
            return File(file, mediaType);
        }

        [HttpHead("{reference}")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> HeadBlob([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);
            var blob = await _db.Blobs.Where(x => x.Id == reference).Where(x => x.Repositories.Any(c => c.Name == repo)).FirstOrDefaultAsync();
            if (blob is null) return NotFound();

            Response.Headers.Append("Docker-Content-Digest", blob.Id);
            Response.Headers.ContentLength = (long?)blob.Size;
            return Ok();
        }

        [HttpPost("uploads")]
        [DockerAuthorize(RepoScope.Push)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromRoute] string name, [FromRoute] string? prefix)
        {
            if (Request.Headers.ContentLength == 0) Response.Headers.Append("OCI-Chunk-Min-Length", "1024"); // TODO: Configurable chunk size

            var repo = RepoHelper.RepoName(prefix, name);
            var uuid = _uploadService.StartUpload(repo);

            // check content type for monolithic upload
            if (Request.Headers.TryGetValue("Content-Type", out var values))
            {
                if (values.Contains("application/octet-stream"))
                {
                    var session = _uploadService.GetUpload(uuid, repo);
                    long lastIndex = await session.AppendChunk(Request.Body);

                    string uploadDigest;

                    var repository = await EnsureRepo(repo);

                    using (var upload = session.OpenContent())
                    {
                        uploadDigest = _digest.CreateDigest(upload);
                        upload.Position = 0;

                        var blob = await _db.Blobs.Include(x => x.Repositories).FirstOrDefaultAsync(x => x.Id == uploadDigest);
                        if (blob is null)
                        {
                            blob = new Blob { Id = uploadDigest, Size = (ulong)lastIndex+1, Repositories = [repository] };
                            _db.Blobs.Add(blob);
                            await _store.PutAsync(uploadDigest, upload);

                            _logger.LogDebug("Created new blob {digest} for repo {repo} via monolithic upload", uploadDigest, repo);
                        }
                        else if (!blob.Repositories.Contains(repository))
                        {
                            blob.Repositories.Add(repository);
                            _logger.LogDebug("Added existing blob {digest} to repo {repo} via monolithic upload", uploadDigest, repo);
                        }
                    }
                    _uploadService.CleanupUpload(uuid);

                    await _db.SaveChangesAsync();

                    Response.Headers.Location = $"/v2/{repo}/blobs/{uploadDigest}";
                    Response.Headers.Append("Range", $"0-{lastIndex}");
                    return StatusCode(StatusCodes.Status201Created);
                }
            }

            Response.Headers.Location = $"/v2/{repo}/blobs/uploads/{uuid}";

            return Accepted();
        }

        [HttpPatch("uploads/{uuid:guid}")]
        [DockerAuthorize(RepoScope.Push)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PatchBlob([FromRoute] Guid uuid, [FromRoute] string name, [FromRoute] string? prefix)
        {
            var repo = RepoHelper.RepoName(prefix, name);

            try
            {
                var session = _uploadService.GetUpload(uuid, repo);
                var range = ParseRangeHeader(Request);

                if (!session.ValidateRange(range))
                {
                    _logger.LogWarning("Range not satisfiable for upload {uuid}", uuid);
                    return StatusCode(StatusCodes.Status416RangeNotSatisfiable);
                }

                var lastIndex = await session.AppendChunk(Request.Body);

                Response.Headers.Location = Request.Path.ToString();
                Response.Headers.Append("Range", $"0-{lastIndex}");
            }
            catch (InvalidOperationException)
            {
                return DockerErrorResponse.BlobUploadInvalid;
            }

            return Accepted();
        }

        [HttpPut("uploads/{uuid:guid}")]
        [DockerAuthorize(RepoScope.Push)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PutBlob([FromRoute] Guid uuid, [FromQuery] string digest, [FromRoute] string name, [FromRoute] string? prefix)
        {
            var repo = RepoHelper.RepoName(prefix, name);

            try
            {
                var session = _uploadService.GetUpload(uuid, repo);
                var range = ParseRangeHeader(Request);

                if (!session.ValidateRange(range) && Request.ContentLength != 0)
                {
                    _logger.LogWarning("Range not satisfiable for upload {uuid}", uuid);
                    return StatusCode(StatusCodes.Status416RangeNotSatisfiable);
                }

                long lastIndex = session.Position;
                if (Request.ContentLength > 0) lastIndex = await session.AppendChunk(Request.Body);

                string uploadDigest;

                var repository = await EnsureRepo(repo);

                using (var upload = session.OpenContent())
                {
                    uploadDigest = _digest.CreateDigest(upload);
                    upload.Position = 0;

                    if (uploadDigest != digest)
                    {
                        await upload.DisposeAsync();
                        _uploadService.CleanupUpload(uuid);
                        return DockerErrorResponse.DigestInvalid;
                    }

                    var blob = await _db.Blobs.Include(x => x.Repositories).FirstOrDefaultAsync(x => x.Id == uploadDigest);
                    if (blob is null)
                    {
                        blob = new Blob { Id = uploadDigest, Size = (ulong)(lastIndex+1), Repositories = [repository] };
                        _db.Blobs.Add(blob);
                        await _store.PutAsync(uploadDigest, upload);

                        _logger.LogDebug("Created new blob {digest} for repo {repo} via chunked upload", uploadDigest, repo);
                    }
                    else if(!blob.Repositories.Contains(repository))
                    {
                        blob.Repositories.Add(repository);

                        _logger.LogDebug("Added existing blob {digest} to repo {repo} via chunked upload", uploadDigest, repo);
                    }
                }

                _uploadService.CleanupUpload(uuid);

                await _db.SaveChangesAsync();


                Response.Headers.Location = $"/v2/{repo}/blobs/{uploadDigest}";
                Response.Headers.Append("Range", $"0-{lastIndex}");
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (InvalidOperationException)
            {
                return DockerErrorResponse.Denied;
            }
        }

        [HttpGet("uploads/{uuid:guid}")]
        [DockerAuthorize(RepoScope.Push)]
        public IActionResult GetBlobUpload([FromRoute] Guid uuid, [FromRoute] string name, [FromRoute] string? prefix)
        {
            var repo = RepoHelper.RepoName(prefix, name);

            try
            {
                var session = _uploadService.GetUpload(uuid, repo);
                Response.Headers.Location = $"/v2/{repo}/blobs/uploads/{uuid}";
                Response.Headers.Append("Range", $"{0}-{session.Position}");
                return NoContent();
            }
            catch (InvalidUploadException)
            {
                return DockerErrorResponse.BlobUploadUnknown;
            }
        }

        private async Task<Repository> EnsureRepo(string name)
        {
            var repo = await _db.Repositories.Where(x => x.Name == name).FirstOrDefaultAsync();
            if (repo is null)
            {
                repo = new Repository { Name = name };
                _db.Repositories.Add(repo);
                await _db.SaveChangesAsync();
                _logger.LogDebug("Created repository {name}", name);
            }
            return repo;
        }

        static private long ParseRangeHeader(HttpRequest request)
        {
            var hasRange = request.Headers.TryGetValue("Content-Range", out var rangeHeader);
            if (!hasRange) return 0;
            var rangeValue = rangeHeader.FirstOrDefault();
            if (rangeValue is null) return 0;
            var split = rangeValue.Split('-');
            if (split.Length != 2) return 0;
            if (!long.TryParse(split[0], out var from) || !long.TryParse(split[1], out var to)) return 0;
            return from;
        }
    }
}
