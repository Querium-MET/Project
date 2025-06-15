using Microsoft.AspNetCore.Mvc;
using Querim.Services;

namespace Querim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly ILogger<QuestionsController> _logger;
        private readonly GeminiService _geminiService;

        public QuestionsController(
            GeminiService geminiService,
            ILogger<QuestionsController> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                // Detailed file validation
                if (file == null)
                    return BadRequest(new { error = "No file uploaded" });

                if (file.Length == 0)
                    return BadRequest(new { error = "Uploaded file is empty" });

                if (!Path.GetExtension(file.FileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Only .txt files are allowed" });

                // Read file content with more robust method
                string text;
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    text = await reader.ReadToEndAsync();
                }

                // Additional text validation
                if (string.IsNullOrWhiteSpace(text))
                    return BadRequest(new { error = "File contains no meaningful text" });

                // Log incoming file details
                _logger.LogInformation(
                    "File uploaded: Name={FileName}, Size={FileSize}, ContentType={ContentType}",
                    file.FileName,
                    file.Length,
                    file.ContentType
                );

                // Generate questions
                var questionsResponse = await _geminiService.GenerateQuestionsAsync(text);

                return Ok(questionsResponse);
            }
            catch (Exception ex)
            {
                // Log the full exception
                _logger.LogError(ex, "Error processing file upload");

                // Return detailed error response
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
