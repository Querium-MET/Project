using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Models;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class StudentQuizController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public StudentQuizController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    // Get quizzes linked to a specific upload for a student
    [HttpGet("student/{studentId}/uploads/{uploadId}/quizzes")]
    public async Task<IActionResult> GetQuizzesByUpload(int studentId, int uploadId)
    {
        // Verify upload exists and belongs to student
        var uploadExists = await _dbContext.StudentUploads
            .AnyAsync(u => u.Id == uploadId && u.StudentId == studentId && !u.IsDeleted);

        if (!uploadExists)
            return NotFound("Upload not found for this student.");

        // Query quizzes related to the upload and student
        var quizzesData = await _dbContext.StudentQuizzes
            .Where(q => q.UploadId == uploadId && q.StudentId == studentId)
            .Select(q => new
            {
                q.Id,
                q.UploadId,
                q.QuestionText,
                q.QuestionCorrectAnswer,
                q.QuestionAnswersJson,
                q.Status
            })
            .ToListAsync();

        var quizzes = quizzesData.Select(q => new
        {
            q.Id,
            q.UploadId,
            q.QuestionText,
            q.QuestionCorrectAnswer,
            Answers = JsonSerializer.Deserialize<List<string>>(q.QuestionAnswersJson),
            q.Status
        }).ToList();

        return Ok(quizzes);
    }
    [HttpGet("student/{studentId}/pdfs")]
    public async Task<IActionResult> GetUploadsByStudent(int studentId, [FromQuery] string status = null)
    {
        var query = _dbContext.StudentUploads
            .Where(u => u.StudentId == studentId && !u.IsDeleted);

        // Apply status filter if provided
        if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
        {
            query = query.Where(u => u.Status.ToLower() == status.ToLower());
        }

        var uploads = await query
            .Select(u => new
            {
                u.Id,
                u.FileName,
                FileUrl = "/uploads/students/" + System.IO.Path.GetFileName(u.FilePath),
                u.Status,
                u.UploadedAt
            })
            .ToListAsync();

        return Ok(uploads);
    }


}