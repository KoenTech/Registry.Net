﻿using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using Microsoft.IdentityModel.Tokens;using OCIRegistry.Data;using OCIRegistry.Models;using OCIRegistry.Models.Database;using System.IdentityModel.Tokens.Jwt;using System.Security.Claims;using System.Text;using System.Text.Json;/*      A non-functional implementation of the auth endpoint.    The endpoint is for debugging only.        TODO: Make an actual authentication system. */namespace OCIRegistry.Controllers{    [Route("api/auth")]    [ApiController]    public class AuthController : ControllerBase    {        private readonly ILogger<AuthController> _logger;        private readonly AppDbContext _db;        public AuthController(ILogger<AuthController> logger, AppDbContext db)        {            _logger=logger;            _db=db;        }        [HttpGet]        public async Task<IActionResult> Login([FromHeader(Name = "Authorization")] string? authHeader, [FromQuery] string? scope)        {            var hasher = new PasswordHasher<User>();            var identity = new ClaimsIdentity();            var requestedScopes = ParseScope(scope ?? "");            if (requestedScopes.Count < 1 && authHeader is null)            {                var response = new AuthResponse { Token = GenerateToken(identity), ExpiresIn = 7200, IssuedAt = DateTime.UtcNow };                return Ok(response);            }            if (requestedScopes.Count < 1)            {                var _validHeader = TryDecodeBasicAuth(authHeader!, out string? _username, out string? _password);                if (!_validHeader) return DockerErrorResponse.Unauthorized;                var _user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == _username);                if (_user is null) return DockerErrorResponse.Unauthorized;                var _result = hasher.VerifyHashedPassword(_user, _user.PasswordHash, _password ?? "");                if (_result == PasswordVerificationResult.Success)                {                    identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, _user.Username));                    var response = new AuthResponse { Token = GenerateToken(identity), ExpiresIn = 7200, IssuedAt = DateTime.UtcNow };                    return Ok(response);                }                _logger.LogInformation("User {user} failed to authenticate from IP address: {ip}", _username, HttpContext.Connection.RemoteIpAddress);                return DockerErrorResponse.Unauthorized;            }            Dictionary<string, byte> permissions = new();            foreach (var requestedScope in requestedScopes)            {                var publicPermissions = await _db.Permissions.AsNoTracking()                    .Where(x => x.UserId == null)                    .Where(x => EF.Functions.Like(requestedScope.Name, x.Resource.Replace("*", "%")))                    .Select(x => x.Action)                    .ToListAsync();                var permittedActions = publicPermissions.Aggregate((byte)0, (a, b) => (byte)(a | b));                permissions[requestedScope.Name] = permittedActions;            }            if (authHeader is null)            {                bool isAllowed = requestedScopes.All(x => x.Actions.All(y => ValidateAccess(permissions[x.Name], y)));                if (isAllowed)                {                    identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "public"));                    identity.AddClaim(new Claim("access", JsonSerializer.Serialize(requestedScopes), JsonClaimValueTypes.JsonArray));                    var response = new AuthResponse { Token = GenerateToken(identity), ExpiresIn = 7200, IssuedAt = DateTime.UtcNow };                    return Ok(response);                }                else                {                    return DockerErrorResponse.Denied;                }            }            var validHeader = TryDecodeBasicAuth(authHeader, out string? username, out string? password);            if (!validHeader) return BadRequest();            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);            if (user is null) return DockerErrorResponse.Unauthorized;            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password ?? "");            if (result == PasswordVerificationResult.Success)            {                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.Username));                foreach (var requestedScope in requestedScopes)                {                    var userPermissions = await _db.Permissions.AsNoTracking()                        .Where(x => x.UserId == user.Id)                        .Where(x => EF.Functions.Like(requestedScope.Name, x.Resource.Replace("*", "%")))                        .Select(x => x.Action)                        .ToListAsync();                    var permittedActions = userPermissions.Aggregate((byte)0, (a, b) => (byte)(a | b));                                        permissions[requestedScope.Name] |= permittedActions;                }                bool isAllowed = requestedScopes.All(x => x.Actions.All(y => ValidateAccess(permissions[x.Name], y)));                if (!isAllowed) return DockerErrorResponse.Denied;                identity.AddClaim(new Claim("access", JsonSerializer.Serialize(requestedScopes), JsonClaimValueTypes.JsonArray));                var response = new AuthResponse { Token = GenerateToken(identity), ExpiresIn = 7200, IssuedAt = DateTime.UtcNow };                _logger.LogDebug("User {user} authenticated with scope {scope}", username, scope);                return Ok(response);            }            _logger.LogInformation("User {user} failed to authenticate from IP address: {ip}", username, HttpContext.Connection.RemoteIpAddress);            return DockerErrorResponse.Unauthorized;        }        //// post JWT login endpoint        //[HttpPost]        //public async Task<IActionResult> LoginAsync([FromForm] string username, [FromForm] string password)        //{        //	// check if user exists and verify password        //	var hasher = new PasswordHasher<User>();        //	var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);        //	if (user is null) return Unauthorized();        //	var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);        //	if (result != PasswordVerificationResult.Success) return Unauthorized();        //	// create JWT token        //	var identity = new ClaimsIdentity();        //	identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, user.Username));        //	// add admin role to identity        //	// generate access token        //	var accessToken = GenerateToken(identity);        //	// generate refresh token        //	var refreshToken = GenerateRefreshToken();        //}        #region Helpers        static bool ValidateAccess(byte action, string scope) => scope switch        {            "pull" => (action & 1) == 1,            "push" => (action & 2) == 2,            "delete" => (action & 4) == 4,            _ => false        };        static bool TryDecodeBasicAuth(string authHeader, out string? user, out string? password)        {            string encodedCredentials = authHeader.Replace("Basic ", "");            byte[] byteCredentials = Convert.FromBase64String(encodedCredentials);            string decodedCredentials = Encoding.UTF8.GetString(byteCredentials);            string[] credentials = decodedCredentials.Split(':');            if (credentials.Length != 2)            {                user = null;                password = null;                return false;            }            user = credentials[0];            password = credentials[1];            return true;        }        // Generate JWT token        private string GenerateToken(ClaimsIdentity claimsIdentity)        {            var tokenHandler = new JwtSecurityTokenHandler();            var key = Encoding.ASCII.GetBytes("a_very_secure_jwt_signing_key_1234567890");            var tokenDescriptor = new SecurityTokenDescriptor            {                Subject = claimsIdentity,                Issuer = "registry-auth",                Audience = "registry",                Expires = DateTime.Now.AddHours(2),                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)            };            var token = tokenHandler.CreateToken(tokenDescriptor);            return tokenHandler.WriteToken(token);        }        // Generate refresh token        private string GenerateRefreshToken()        {            var tokenHandler = new JwtSecurityTokenHandler();            var key = Encoding.ASCII.GetBytes("a_very_secure_jwt_signing_key_1234567890");            var tokenDescriptor = new SecurityTokenDescriptor            {                Subject = new ClaimsIdentity(),                Issuer = "registry-auth",                Audience = "registry",                Expires = DateTime.Now.AddHours(24),                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)            };            var token = tokenHandler.CreateToken(tokenDescriptor);            return tokenHandler.WriteToken(token);        }        private static List<AccessClaim> ParseScope(string scopeString)        {            var scopes = scopeString.Split(' ');            var claims = new List<AccessClaim>();            foreach (var scopePart in scopes)            {                var parts = scopePart.Split(':');                if (parts.Length != 3)                {                    continue;                }                var claim = new AccessClaim                {                    Type = parts[0],                    Name = parts[1],                    Actions = parts[2].Split(',').ToList()                };                claims.Add(claim);            }            return claims;        }        #endregion    }}