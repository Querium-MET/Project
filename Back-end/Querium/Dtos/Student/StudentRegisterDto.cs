using System.ComponentModel.DataAnnotations;

namespace Querim.Dtos
{
    public class StudentRegisterDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [StringLength(100)]
        public string UniversityIDCard { get; set; }

        [StringLength(100)]
        public string NationalIDCard { get; set; }
    }
}

