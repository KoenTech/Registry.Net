using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCIRegistry.Data;
using OCIRegistry.Helpers;

namespace OCIRegistry.Controllers
{
    [Route("v2/{prefix}/{name:required}/tags"), Route("v2/{name:required}/tags")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TagsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("list")]
        [DockerAuthorize(RepoScope.Pull)]
        public async Task<IActionResult> ListTags([FromRoute] string name, [FromRoute] string? prefix)
        {
            string repo = RepoHelper.RepoName(prefix, name);

            var tags = await _db.Tags
                .Include(t => t.Manifest)
                .ThenInclude(m => m.Repository)
                .Where(t => t.Manifest.Repository.Name == repo)
                .Select(t => t.Name)
                .ToListAsync();

            return Ok(new TagResponse(repo, tags));
        }

        record TagResponse(string name, List<string> tags);
    }
}
