using System.ComponentModel.DataAnnotations;

namespace Querim.Dtos
{
    public class ChapterUploadDto
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public IFormFile PdfFile { get; set; }
    }
}
