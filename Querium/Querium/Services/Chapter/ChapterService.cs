using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Dtos;
using Querim.Models;

namespace Querim.Services
{
    public interface IChapterService
    {
        Task<Chapter> UploadChapterAsync(int subjectId, ChapterUploadDto chapterDto);
        Task<List<Chapter>> GetChaptersBySubjectAsync(int subjectId);
    }
    public class ChapterService : IChapterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _uploadsFolder;

        public ChapterService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            // Create uploads directory if it doesn't exist
            _uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "chapters");
            Directory.CreateDirectory(_uploadsFolder);
        }

        private async Task<string> SavePdfFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded");

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf")
                throw new ArgumentException("Only PDF files are allowed");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadsFolder, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public async Task<Chapter> UploadChapterAsync(int subjectId, ChapterUploadDto chapterDto)
        {
            try
            {
                var subject = await _context.Subjects.FindAsync(subjectId);
                if (subject == null)
                    throw new ArgumentException($"Subject with ID {subjectId} not found");

                // Save the PDF file
                var pdfFileName = await SavePdfFileAsync(chapterDto.PdfFile);

                var chapter = new Chapter
                {
                    Title = chapterDto.Title,
                    Description = chapterDto.Description,

                    SubjectId = subjectId
                };

                _context.Chapters.Add(chapter);
                await _context.SaveChangesAsync();

                return chapter;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading chapter: {ex.Message}");
            }
        }

        public Task<List<Chapter>> GetChaptersBySubjectAsync(int subjectId)
        {
            throw new NotImplementedException();
        }
    }
}
