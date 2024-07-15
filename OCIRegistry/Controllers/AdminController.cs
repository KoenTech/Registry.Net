using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCIRegistry.Data;
using OCIRegistry.Models.Dtos;

namespace OCIRegistry.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> ListAccounts()
        {
            var users = await _db.Users.Select(x => new UserDto() { Id = x.Id, Username = x.Username }).ToListAsync();
            return Ok(users);
        }

        [HttpPost("account/new")]
        public async Task<IActionResult> CreateAccount([FromBody] NewUserDto user)
        {
            if (await _db.Users.AnyAsync(x => x.Username == user.Username)) return Conflict("This username is already taken");

            var hasher = new PasswordHasher<Models.Database.User>();

            var newUser = new Models.Database.User() { Username = user.Username!, PasswordHash = "" };
            newUser.PasswordHash = hasher.HashPassword(newUser, user.Password!);
            await _db.Users.AddAsync(newUser);
            await _db.SaveChangesAsync();
            return Ok(UserDto.FromDatabaseModel(newUser));
        }

        [HttpPatch("account/{id}")]
        public async Task<IActionResult> UpdateAccount([FromRoute] ulong id, [FromBody] NewUserDto user)
        {
            var dbUser = await _db.Users.FindAsync(id);
            if (dbUser is null) return NotFound();
            var hasher = new PasswordHasher<Models.Database.User>();

            dbUser.Username = user.Username!;
            dbUser.PasswordHash = hasher.HashPassword(dbUser, user.Password!);
            await _db.SaveChangesAsync();
            return Ok(UserDto.FromDatabaseModel(dbUser));
        }

        [HttpDelete("account/{id}")]
        public async Task<IActionResult> DeleteAccount([FromRoute] ulong id)
        {
            var dbUser = await _db.Users.FindAsync(id);
            if (dbUser is null) return NotFound();
            _db.Users.Remove(dbUser);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("account/{id}/permissions")]
        public async Task<IActionResult> ListAccountPermissions([FromRoute] ulong id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();

            var permissions = await _db.Permissions.Where(x => x.UserId == id || x.UserId == null).ToListAsync();
            return Ok(permissions);
        }

        [HttpPost("account/{id}/permissions")]
        public async Task<IActionResult> AddAccountPermission([FromRoute] ulong id, [FromBody] PermissionDto permission)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (permission is null || permission.Resource is null) return BadRequest();
            var oldPermission = await _db.Permissions.FirstOrDefaultAsync(x => x.Resource == permission.Resource && x.UserId == id);
            if (oldPermission is null)
            {
                var newPermission = new Models.Database.Permission() { Resource = permission.Resource, User = user, Action = permission.Action };
                await _db.Permissions.AddAsync(newPermission);
            } else
            {
                oldPermission.Action = permission.Action;
            }

            await _db.SaveChangesAsync();

            return Ok(permission);
        }

        [HttpDelete("account/{id}/permissions")]
        public async Task<IActionResult> RemoveAccountPermission([FromRoute] ulong id, [FromBody] PermissionDto permission)
        {
            var user = await _db.Users.FindAsync(id);
            if (user is null) return NotFound();
            if (permission is null || permission.Resource is null) return BadRequest();
            var oldPermission = await _db.Permissions.FirstOrDefaultAsync(x => x.Resource == permission.Resource && x.UserId == id);
            if (oldPermission is null) return NotFound();

            _db.Permissions.Remove(oldPermission);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
