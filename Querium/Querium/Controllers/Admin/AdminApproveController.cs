using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using Querim.Models;
using System.Text.Json;
using System.Text;
using Querim.Data;
using UglyToad.PdfPig;
using Querim.Services;

[ApiController]
[Route("api/[controller]")]
public class AdminApproveController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GeminiService _geminiService;
    private readonly ILogger<AdminApproveController> _logger;

    public AdminApproveController(ApplicationDbContext dbContext, GeminiService geminiService, ILogger<AdminApproveController> logger)
    {
        _dbContext = dbContext;
        _geminiService = geminiService;
        _logger = logger;
    }

    [HttpGet("uploads/pending")]
    public async Task<IActionResult> GetPendingUploads()
    {
        var uploads = await _dbContext.StudentUploads
            .Where(u => u.Status == "Pending" && !u.IsDeleted)
            .Include(u => u.Student)
            .Select(u => new
            {
                u.Id,
                StudentName = u.Student.FullName,
                u.FileName,
                u.UploadedAt,
                u.Status,
                FileUrl = "/uploads/students/" + Path.GetFileName(u.FilePath)
            })
            .ToListAsync();

        return Ok(uploads);
    }

    [HttpPost("uploads/{uploadId}/approve")]
    public async Task<IActionResult> ApproveUpload(int uploadId)
    {
        var upload = await _dbContext.StudentUploads.Include(u => u.Student).FirstOrDefaultAsync(u => u.Id == uploadId);
        if (upload == null)
            return NotFound();

        if (upload.Status == "Approved")
            return BadRequest("Upload is already approved.");

        upload.Status = "Approved";
        await _dbContext.SaveChangesAsync();

        // Extract text from PDF
        string extractedText;
        try
        {
            using var pdfStream = System.IO.File.OpenRead(upload.FilePath);
            using var pdfDocument = PdfDocument.Open(pdfStream);

            var sb = new StringBuilder();
            foreach (var page in pdfDocument.GetPages())
                sb.AppendLine(page.Text);

            extractedText = sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract PDF text.");
            return Ok(new { message = "Upload approved, but failed to extract text for question generation." });
        }

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return Ok(new { message = "Upload approved, but no text found for question generation." });
        }

        // Generate quiz questions using GeminiService
        List<GeminiService.QuizQuestion> questions;
        try
        {
            questions = await _geminiService.GenerateQuestionsAsync(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed generating questions.");
            return Ok(new { message = "Upload approved, but failed to generate questions." });
        }

        // Save the generated quiz questions linked to student
        var quizEntities = questions.Select(q => new StudentQuiz
        {
            StudentId = upload.StudentId,
            UploadId = upload.Id,
            QuestionText = q.QuestionText,
            QuestionCorrectAnswer = q.CorrectAnswer,
            QuestionAnswersJson = JsonSerializer.Serialize(q.Answers),
            Status = "Approved",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _dbContext.StudentQuizzes.AddRange(quizEntities);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Upload approved and questions generated." });
    }

    [HttpPost("uploads/{uploadId}/reject")]
    public async Task<IActionResult> RejectUpload(int uploadId)
    {
        var upload = await _dbContext.StudentUploads.FindAsync(uploadId);
        if (upload == null)
            return NotFound();

        upload.Status = "Rejected";

        // Also update associated quizzes to "Rejected" status
        var relatedQuizzes = await _dbContext.StudentQuizzes
            .Where(q => q.UploadId == uploadId)
            .ToListAsync();

        foreach (var quiz in relatedQuizzes)
        {
            quiz.Status = "Rejected";
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Upload and associated quizzes rejected." });
    }

}