using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OCIRegistry.Controllers
{
    [Route("v2")]
    [ApiController]
    [DockerAuthorize(RepoScope.None)]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetVersion()
        {
            Response.Headers.Append("Docker-Distribution-API-Version", "registry/2.0");
            return Ok();
        }
    }
}