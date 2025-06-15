using System.ComponentModel.DataAnnotations;

namespace Querim.Dtos
{
    public class MultiChapterUploadDto
    {
        [Required]
        public List<ChapterUploadDto> Chapters { get; set; }
    }
}
