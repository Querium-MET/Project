namespace Querim.Dtos
{
    public class ChapterCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PdfPath { get; set; }
        public int SubjectId { get; set; }

    }
}
