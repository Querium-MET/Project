using System.ComponentModel.DataAnnotations;

namespace Querim.Dtos
{
    public class ChangePasswordDto

    {
        [Required]
        public string UniversityIDCard { get; set; }
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        public string ConfirmNewPassword { get; set; }
    }
}
