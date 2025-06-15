using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Models;
using Querim.Services;
using System.Text.Json;

namespace Querim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly ILogger<FileUploadController> _logger;
        private readonly ApplicationDbContext _dbContext;
        public FileUploadController(
            GeminiService geminiService,
            ILogger<FileUploadController> logger, ApplicationDbContext dbContext)
        {
            _geminiService = geminiService;
            _logger = logger;
            _dbContext = dbContext;

        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                // Validate file presence
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file or empty file uploaded");
                    return BadRequest(new { error = "Please upload a valid file" });
                }

                // Validate file extension
                if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Invalid file type uploaded: {file.FileName}");
                    return BadRequest(new { error = "Only .txt files are supported" });
                }

                // Read file content
                string text;
                try
                {
                    using var reader = new StreamReader(file.OpenReadStream());
                    text = await reader.ReadToEndAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading file content");
                    return BadRequest(new { error = "Error reading file content" });
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Empty text content in file");
                    return BadRequest(new { error = "The file contains no text" });
                }

                // Generate questions using GeminiService
                List<GeminiService.QuizQuestion> questions;
                try
                {
                    _logger.LogInformation("Generating questions from text content");
                    questions = await _geminiService.GenerateQuestionsAsync(text);
                    _logger.LogInformation($"Successfully generated {questions.Count} questions");
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "Gemini API request failed");
                    return StatusCode(502, new
                    {
                        error = "Error communicating with Gemini API",
                        details = httpEx.Message
                    });
                }

                // Map generated questions to database entity
                var questionEntities = questions.Select(q => new QuizQuestionEntity
                {
                    
                    QuestionText = q.QuestionText,
                    CorrectAnswer = q.CorrectAnswer,
                    AnswersJson = JsonSerializer.Serialize(q.Answers)
                }).ToList();

                // Save questions into database
                _dbContext.QuizQuestions.AddRange(questionEntities);
                await _dbContext.SaveChangesAsync();

                // Return success response with generated questions
                return Ok(new
                {
                    success = true,
                    count = questions.Count,
                    questions = questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing file upload");
                return Problem(
                    title: "Error processing file",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        }
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions()
        {
            try
            {
                // Retrieve all questions from DB
                var questionEntities = await _dbContext.QuizQuestions.ToListAsync();

                // Map entities back to DTO model
                var questions = questionEntities.Select(q => new GeminiService.QuizQuestion
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    CorrectAnswer = q.CorrectAnswer,
                    Answers = JsonSerializer.Deserialize<List<string>>(q.AnswersJson) ?? new List<string>()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = questions.Count,
                    questions = questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching questions from database");
                return StatusCode(500, new { error = "An error occurred while retrieving questions." });
            }
        }

    }
}