using System.ComponentModel.DataAnnotations;

namespace Querim.Dtos
{
    public class AdminLoginDto
    {
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }
    }
}
