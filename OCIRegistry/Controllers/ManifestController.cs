﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCIRegistry.Data;
using OCIRegistry.Helpers;
using OCIRegistry.Models;
using OCIRegistry.Models.Database;
using OCIRegistry.Services;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OCIRegistry.Controllers
{
    [Route("v2/{prefix}/{name:required}/manifests"), Route("v2/{name:required}/manifests")]
    [ApiController]
    public class ManifestController : ControllerBase
    {
        private readonly DigestService _digest;
        private readonly AppDbContext _db;
        private readonly ILogger<ManifestController> _logger;

        public ManifestController(DigestService digest, AppDbContext db, ILogger<ManifestController> logger)
        {
            _digest = digest;
            _db = db;
            _logger=logger;
        }

        [HttpGet("{reference}")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> GetManifest([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);

            Manifest? manifest;
            if (DigestHelper.IsDigest(reference))
            {
                manifest = await _db.Manifests
                    .Include(m => m.Repository)
                    .Where(m => m.Digest == reference)
                    .Where(m => m.Repository.Name == repo)
                    .FirstOrDefaultAsync();
            }
            else
            {
                manifest = await _db.Tags
                    .Include(t => t.Manifest)
                    .ThenInclude(m => m.Repository)
                    .Where(t => t.Manifest.Repository.Name == repo)
                    .Where(t => t.Name == reference)
                    .Select(x => x.Manifest)
                    .FirstOrDefaultAsync();
            }

            if (manifest == null) return NotFound();

            Response.Headers.Append("Docker-Content-Digest", manifest.Digest);
            return File(manifest.Content, MediaType.ImageManifest);
        }

        [HttpHead("{reference}")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> ManifestExists([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);

            Manifest? manifest;
            if (DigestHelper.IsDigest(reference))
            {
                manifest = await _db.Manifests
                    .Include(m => m.Repository)
                    .Where(m => m.Digest == reference)
                    .Where(m => m.Repository.Name == repo)
                    .FirstOrDefaultAsync();
            }
            else
            {
                manifest = await _db.Tags
                    .Include(t => t.Manifest)
                    .ThenInclude(m => m.Repository)
                    .Where(t => t.Manifest.Repository.Name == repo)
                    .Where(t => t.Name == reference)
                    .Select(x => x.Manifest)
                    .FirstOrDefaultAsync();
            }

            if (manifest == null) return NotFound();

            Response.Headers.Append("Docker-Content-Digest", manifest.Digest);
            Response.Headers.ContentLength = manifest.Content.Length;
            Response.Headers.ContentType = MediaType.ImageManifest;
            return Ok();
        }

        [HttpPut("{reference}")]
        [DockerAuthorize(RepoScope.Push)]
        public async Task<IActionResult> PutManifest([FromRoute] string reference, [FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);

            var repository = await _db.Repositories.FirstOrDefaultAsync(r => r.Name == repo);

            string digest;
            byte[] content;
            Manifest? manifest;

            using (var buffer = new MemoryStream())
            {
                await Request.Body.CopyToAsync(buffer);
                buffer.Position = 0;
                digest = _digest.CreateDigest(buffer);
                content = buffer.ToArray();
            }

            //Console.WriteLine($"Received manifest: {digest}");
            System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(content);
            //Console.WriteLine($"Calculated digest: sha256:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}");

            if (repository == null)
            {
                repository = new Repository { Name = repo };
                _db.Repositories.Add(repository);

                _logger.LogDebug("Created new repository {repo}", repository.Name);

                manifest = new Manifest { Digest = digest, RepositoryId = repository.Id, Repository = repository, Content = content };

                if (!DigestHelper.IsDigest(reference))
                {
                    var tag = new Tag { Name = reference, ManifestId = manifest.Id, Manifest = manifest };
                    _db.Tags.Add(tag);
                    _logger.LogDebug("Created new tag {tag} for repository {repo}", tag.Name, repository.Name);
                }

                _db.Manifests.Add(manifest);
            }
            else
            {
                manifest = await _db.Manifests.FirstOrDefaultAsync(m => m.Digest == digest);
                if (manifest == null)
                {
                    manifest = new Manifest { Digest = digest, RepositoryId = repository.Id, Repository = repository, Content = content };

                    _db.Manifests.Add(manifest);
                    _logger.LogDebug("Created new manifest {digest} for repository {repo}", manifest.Digest, repository.Name);
                }
                if (!DigestHelper.IsDigest(reference))
                {
                    var tag = await _db.Tags.Where(t => t.Manifest.RepositoryId == repository.Id && t.Name == reference).FirstOrDefaultAsync();
                    if (tag == null)
                    {
                        var newTag = new Tag { Name = reference, ManifestId = manifest.Id, Manifest = manifest };
                        _db.Tags.Add(newTag);
                        _logger.LogDebug("Created new tag {tag} for repository {repo}", newTag.Name, repository.Name);
                    }
                    else
                    {
                        tag.Manifest = manifest;
                        _logger.LogDebug("Updated tag {tag} for repository {repo}", tag.Name, repository.Name);
                    }
                }
            }

            try
            {
                var imageManifest = JsonSerializer.Deserialize<ImageManifest>(manifest.Content);
                if (imageManifest is not null)
                {
                    var digests = imageManifest.layers.Select(x => x.digest).Append(imageManifest.config.digest);
                    var blobs = await _db.Blobs.Include(b => b.Manifests).Where(x => x.Repositories.Contains(repository)).Where(b => digests.Contains(b.Id)).ToListAsync();
                    if (blobs.Count != digests.Count())
                    {
                        _logger.LogWarning("Rejected manifest because not all layers are present or the manifest references layers from a different repository");
                        return BadRequest();
                    }

                    foreach (var blob in blobs.Where(b => !b.Manifests.Contains(manifest)))
                    {
                        blob.Manifests.Add(manifest);
                    }
                }
            }
            catch (JsonException e)
            {
                _logger.LogWarning("Failed to parse manifest as ImageManifest");
                return BadRequest();
            }

            await _db.SaveChangesAsync();

            Response.Headers.Location = $"/v2/{repo}/manifests/{reference}";
            Response.Headers.Append("Docker-Content-Digest", digest);
            return StatusCode(StatusCodes.Status201Created);
        }
    }
}
