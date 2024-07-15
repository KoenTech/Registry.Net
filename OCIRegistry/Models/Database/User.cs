using System.ComponentModel.DataAnnotations;

namespace OCIRegistry.Models.Database
{
    public class User
    {
        [Key]
        public ulong Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public List<Permission> Permissions { get; set; } = new();
    }

    public class Permission
    {
        [Key]
        public ulong Id { get; set; }
        public required string Resource { get; set; }
        public byte Action { get; set; }
        public ulong? UserId { get; set; }
        public User? User { get; set; }
    }
}
