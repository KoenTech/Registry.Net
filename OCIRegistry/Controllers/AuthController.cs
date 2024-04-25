using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OCIRegistry.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

/* 
 
    A non-functional implementation of the auth endpoint.
    The endpoint is for debugging only.
    
    TODO: Make an actual authentication system.

 */

namespace OCIRegistry.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        public AuthController(ILogger<AuthController> logger)
        {
            _logger=logger;
        }

        [HttpGet]
        public async Task<IActionResult> Login([FromHeader(Name = "Authorization")] string? authHeader, [FromQuery] string? scope)
        {
            if (authHeader is null)
            {
                var identity = new ClaimsIdentity();
                var response = new AuthResponse { Token = GenerateToken(identity), ExpiresIn = 7200, IssuedAt = DateTime.UtcNow };
                return Ok(response);
            }

            var credentials = DecodeBasicAuth(authHeader);
            if (credentials == "user:pass") // security 100
            {
                var identity = new ClaimsIdentity();
                if (scope is not null) identity.AddClaim(new Claim("access", JsonSerializer.Serialize(ParseScope(scope))));
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "user"));

                var response = new AuthResponse { Token = GenerateToken(identity), ExpiresIn = 7200, IssuedAt = DateTime.UtcNow };
                _logger.LogDebug("User {user} authenticated with scope {scope}", "user", scope);
                return Ok(response);
            }
            return Unauthorized();
        }

        static string DecodeBasicAuth(string authHeader)
        {
            string encodedCredentials = authHeader.Replace("Basic ", "");
            byte[] byteCredentials = Convert.FromBase64String(encodedCredentials);
            string decodedCredentials = Encoding.UTF8.GetString(byteCredentials);
            return decodedCredentials;
        }

        private string GenerateToken(ClaimsIdentity claimsIdentity)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("a_very_secure_jwt_signing_key_1234567890");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Issuer = "registry-auth",
                Audience = "registry",
                Expires = DateTime.Now.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static AccessClaim ParseScope(string scope)
        {
            var parts = scope.Split(':');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Scope is not in the correct format.");
            }

            var claim = new AccessClaim
            {
                Type = parts[0],
                Name = parts[1],
                Actions = parts[2].Split(',').ToList()
            };

            return claim;
        }
    }
}
