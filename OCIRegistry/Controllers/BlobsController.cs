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
        private readonly BlobUploader _uploader;

        public BlobsController(DigestService digest, AppDbContext db, IBlobStore store, BlobUploader uploader)
        {
            _digest = digest;
            _db = db;
            _store=store;
            _uploader=uploader;
        }

        [HttpGet("{reference}")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> GetBlob([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);
            var blob = await _db.Blobs.Where(x => x.Id == reference).Where(x => x.Manifests.Any(c => c.Repository.Name == repo)).FirstOrDefaultAsync();
            if (blob is null) return NotFound();

            var file = await _store.GetAsync(blob.Id);
            return File(file, MediaType.Layer);
        }

        [HttpHead("{reference}")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> HeadBlob([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);
            var blob = await _db.Blobs.Where(x => x.Id == reference).FirstOrDefaultAsync();
            if (blob is null) return NotFound();

            Response.Headers.Append("Docker-Content-Digest", blob.Id);
            Response.Headers.ContentLength = (long?)blob.Size;
            return Ok();
        }

        [HttpPost("uploads")]
        [DockerAuthorize(RepoScope.Push)]
        public async Task<IActionResult> Upload([FromRoute] string name, [FromRoute] string? prefix)
        {
            if (Request.Headers.ContentLength == 0) Response.Headers.Append("OCI-Chunk-Min-Length", "1024");

            var repo = RepoHelper.RepoName(prefix, name);
            var uuid = _uploader.StartUpload(repo);

            // check content type for monolithic upload
            if (Request.Headers.TryGetValue("Content-Type", out var values))
            {
                if (values.Contains("application/octet-stream"))
                {
                    var size = await _uploader.UploadChunk(repo, uuid, Request.Body);

                    string uploadDigest;

                    using (var upload = _uploader.FinishUpload(repo, uuid))
                    {
                        uploadDigest = _digest.CreateDigest(upload);
                        upload.Position = 0;

                        await _store.PutAsync(uploadDigest, upload);
                    }
                    _uploader.CleanupUpload(repo, uuid);

                    var blob = await _db.Blobs.FirstOrDefaultAsync(x => x.Id == uploadDigest);
                    if (blob is null)
                    {
                        blob = new Blob { Id = uploadDigest, Size = size };
                        _db.Blobs.Add(blob);
                    }

                    await _db.SaveChangesAsync();

                    Response.Headers.Location = $"/v2/{repo}/blobs/{uploadDigest}";
                    Response.Headers.Append("Range", $"0-{size}");
                    return StatusCode(StatusCodes.Status201Created);
                }
            }   

            Response.Headers.Location = $"/v2/{repo}/blobs/upload/{uuid}";

            // Response.Headers.Location = $"../upload/{uuid}";
            return Accepted();
        }

        [HttpPatch("upload/{uuid:guid}")]
        [DockerAuthorize(RepoScope.Push)]
        public async Task<IActionResult> PatchBlob([FromRoute] Guid uuid, [FromRoute] string name, [FromRoute] string? prefix)
        {
            var repo = RepoHelper.RepoName(prefix, name);
            if (!_uploader.UploadAllowed(repo, uuid)) return Forbid();

            var size = await _uploader.UploadChunk(repo, uuid, Request.Body);

            Response.Headers.Location = Request.Path.ToString();
            Response.Headers.Append("Range", $"0-{size}");

            return Accepted();
        }

        [HttpPut("upload/{uuid:guid}")]
        [DockerAuthorize(RepoScope.Push)]
        public async Task<IActionResult> PutBlob([FromRoute] Guid uuid,[FromQuery] string digest, [FromRoute] string name, [FromRoute] string? prefix)
        {
            var repo = RepoHelper.RepoName(prefix, name);
            if (!_uploader.UploadAllowed(repo, uuid)) return Forbid();

            var size = await _uploader.UploadChunk(repo, uuid, Request.Body);

            string uploadDigest;

            using (var upload = _uploader.FinishUpload(repo, uuid))
            {
                uploadDigest = _digest.CreateDigest(upload);
                upload.Position = 0;

                if (uploadDigest != digest)
                {
                    await upload.DisposeAsync();
                    _uploader.CleanupUpload(repo, uuid);
                    return BadRequest();
                }

                await _store.PutAsync(uploadDigest, upload);
            }
            _uploader.CleanupUpload(repo, uuid);

            var blob = await _db.Blobs.FirstOrDefaultAsync(x => x.Id == uploadDigest);
            if (blob is null)
            {
                blob = new Blob { Id = uploadDigest, Size = size };
                _db.Blobs.Add(blob);
            }

            await _db.SaveChangesAsync();


            Response.Headers.Location = $"/v2/{repo}/blobs/{uploadDigest}";
            Response.Headers.Append("Range", $"0-{size}");
            return StatusCode(StatusCodes.Status201Created);
        }
    }
}
