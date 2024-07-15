using System.ComponentModel.DataAnnotations;

namespace OCIRegistry.Models.Dtos
{
    public class NewUserDto
    {
        [Required]
        public string? Username { get; set; }
        [Required]
        public string? Password { get; set; }
    }
}
