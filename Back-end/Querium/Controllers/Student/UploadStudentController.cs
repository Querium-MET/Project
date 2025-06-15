using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Models;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class UploadStudentController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UploadStudentController> _logger;

    public UploadStudentController(ApplicationDbContext dbContext, ILogger<UploadStudentController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public class StudentPdfUploadDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }

    [HttpPost("upload-pdf")]
    public async Task<IActionResult> UploadStudentPdf([FromForm] StudentPdfUploadDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("File is required.");

        var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            return BadRequest("Only PDF files are supported.");

        string uniqueFileName = $"{Guid.NewGuid()}{ext}";
        string uploadFolder = Path.Combine("wwwroot", "uploads", "students");
        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        string filePath = Path.Combine(uploadFolder, uniqueFileName);
        using (var fileStream = System.IO.File.Create(filePath))
        {
            await dto.File.CopyToAsync(fileStream);
        }

        var upload = new StudentUpload
        {
            StudentId = dto.StudentId,
            FileName = dto.File.FileName,
            FilePath = filePath,
            Status = "Pending",
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _dbContext.StudentUploads.Add(upload);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Upload successful and pending approval." });
    }
}