using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Models;
using Querim.Services;
using Querim.Dtos;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace Querim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly GeminiService _geminiService;
        private readonly ILogger<UploadController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public UploadController(GeminiService geminiService, ILogger<UploadController> logger, ApplicationDbContext dbContext)
        {
            _geminiService = geminiService;
            _logger = logger;
            _dbContext = dbContext;
        }


        [HttpPost("chapter")]
        public async Task<IActionResult> CreateChapter([FromBody] ChapterCreateDto dto)
        {
            var chapter = new Chapter
            {
                Title = dto.Title,
                Description = dto.Description,
                PdfPath = string.IsNullOrWhiteSpace(dto.PdfPath) ? null : dto.PdfPath,
                SubjectId = dto.SubjectId,
                // assumes FilePath is part of DTO and Chapter model
            };

            _dbContext.Chapters.Add(chapter);
            await _dbContext.SaveChangesAsync();

            return Ok(chapter);
        }
        [HttpPost("chapters")]  
        public async Task<IActionResult> CreateChapters([FromBody] List<ChapterCreateDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return BadRequest("Please provide at least one chapter to create.");
            }

            var chapters = dtos.Select(dto => new Chapter
            {
                Title = dto.Title,
                Description = dto.Description,
                PdfPath = string.IsNullOrWhiteSpace(dto.PdfPath) ? null : dto.PdfPath,
                SubjectId = dto.SubjectId,
                
            }).ToList();

            _dbContext.Chapters.AddRange(chapters);
            await _dbContext.SaveChangesAsync();

            return Ok(chapters); // returns list of created chapters with their generated IDs
        }

        [HttpGet("subjects/{subjectId}/chapters")]
        public async Task<IActionResult> GetChaptersBySubject(int subjectId)
        {
            var subject = await _dbContext.Subjects
                .Include(s => s.Chapters)
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
                return NotFound($"Subject with ID {subjectId} not found.");

            var chapters = subject.Chapters.Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                //c.SubjectId,
                SubjectName = subject.Title
                // Add other properties as needed
            }).ToList();

            return Ok(chapters);
        }
        [HttpPost("upload-multiple/{chapterId}")]
        public async Task<IActionResult> UploadFilesToChapter(int chapterId, [FromForm] List<IFormFile> files)
        {
            // Validate chapter exists
            var chapter = await _dbContext.Chapters.FindAsync(chapterId);
            if (chapter == null)
            {
                return NotFound("Chapter not found");
            }

            // Validate files presence
            if (files == null || files.Count == 0)
            {
                return BadRequest("Please upload at least one file.");
            }

            var allQuestions = new List<GeminiService.QuizQuestion>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest($"One or more files are empty. Please upload valid files.");
                    
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (fileExtension != ".pdf" && fileExtension != ".txt")
                {
                    return BadRequest($"File '{file.FileName}' is not supported. Only .txt and .pdf files are allowed.");
                }

                string extractedText = "";

                try
                {
                    if (fileExtension == ".pdf")
                    {
                        using var pdfStream = file.OpenReadStream();
                        using var pdfDocument = PdfDocument.Open(pdfStream);

                        var stringBuilder = new StringBuilder();
                        foreach (var page in pdfDocument.GetPages())
                        {
                            stringBuilder.AppendLine(page.Text);
                        }
                        extractedText = stringBuilder.ToString();
                    }
                    else if (fileExtension == ".txt")
                    {
                        using var reader = new StreamReader(file.OpenReadStream());
                        extractedText = await reader.ReadToEndAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reading file content from '{file.FileName}'");
                    return BadRequest($"Error reading file content from '{file.FileName}'.");
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return BadRequest($"The file '{file.FileName}' contains no readable text.");
                }

                List<GeminiService.QuizQuestion> questions;

                try
                {
                    questions = await _geminiService.GenerateQuestionsAsync(extractedText);
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error generating questions from file '{file.FileName}'");
                    return StatusCode(500, $"An error occurred while generating questions from file '{file.FileName}'.");
                }

                allQuestions.AddRange(questions);
            }

            if (allQuestions.Count == 0)
            {
                return BadRequest("No valid questions were generated from the uploaded files.");
            }

            var questionEntities = allQuestions.Select(q => new QuizQuestionEntity
            {
                QuestionText = q.QuestionText,
                CorrectAnswer = q.CorrectAnswer,
                AnswersJson = JsonSerializer.Serialize(q.Answers),
                ChapterId = chapterId
            }).ToList();

            _dbContext.QuizQuestions.AddRange(questionEntities);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                count = allQuestions.Count,
                questions = allQuestions
            });
        }

        [HttpPost("upload-single/{chapterId}")]
        public async Task<IActionResult> UploadFileToChapter(int chapterId, [FromForm] IFormFile file)
        {
            // Validate chapter exists
            var chapter = await _dbContext.Chapters.FindAsync(chapterId);
            if (chapter == null)
            {
                return NotFound("Chapter not found");
            }

            // Validate file presence
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a valid file.");
            }

            // Validate file extension (allow .txt and .pdf)
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf" && fileExtension != ".txt")
            {
                return BadRequest("Only .txt and .pdf files are supported.");
            }

            string extractedText = "";

            try
            {
                if (fileExtension == ".pdf")
                {
                    // Extract text from PDF using PdfPig
                    using var pdfStream = file.OpenReadStream();
                    using var pdfDocument = PdfDocument.Open(pdfStream);

                    var stringBuilder = new StringBuilder();
                    foreach (var page in pdfDocument.GetPages())
                    {
                        stringBuilder.AppendLine(page.Text);
                    }
                    extractedText = stringBuilder.ToString();
                }
                else if (fileExtension == ".txt")
                {
                    // Read txt file content as plain text
                    using var reader = new StreamReader(file.OpenReadStream());
                    extractedText = await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file content");
                return BadRequest("Error reading file content.");
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return BadRequest("The file contains no readable text.");
            }

            List<GeminiService.QuizQuestion> questions;

            try
            {
                questions = await _geminiService.GenerateQuestionsAsync(extractedText);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating questions");
                return StatusCode(500, "An error occurred while generating questions.");
            }

            var questionEntities = questions.Select(q => new QuizQuestionEntity
            {
                QuestionText = q.QuestionText,
                CorrectAnswer = q.CorrectAnswer,
                AnswersJson = JsonSerializer.Serialize(q.Answers),
                ChapterId = chapterId
            }).ToList();

            _dbContext.QuizQuestions.AddRange(questionEntities);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                count = questions.Count,
                questions
            });
        }
        [HttpGet("chapters/{chapterId}/questions")]
        public async Task<IActionResult> GetQuestionsByChapter(int chapterId)
        {
            var chapter = await _dbContext.Chapters
                .Include(c => c.QuizQuestions)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter == null)
                return NotFound($"Chapter with ID {chapterId} not found.");

            var questions = chapter.QuizQuestions.Select(q => new
            {
                q.Id,
                q.QuestionText,
                q.CorrectAnswer,
                Answers = JsonSerializer.Deserialize<List<string>>(q.AnswersJson)
            }).ToList();

            return Ok(questions);
        }

    }
}
        //[HttpPost("upload-multi/{chapterId}")]
        //public async Task<IActionResult> UploadFilesToChapter(int chapterId, [FromForm] List<IFormFile> files)
        //{
        //    // Validate chapter exists
        //    var chapter = await _dbContext.Chapters.FindAsync(chapterId);
        //    if (chapter == null)
        //    {
        //        return NotFound("Chapter not found");
        //    }

        //    // Validate files presence
        //    if (files == null || files.Count == 0)
        //    {
        //        return BadRequest("Please upload at least one file.");
        //    }

        //    var allQuestions = new List<GeminiService.QuizQuestion>();

        //    foreach (var file in files)
        //    {
        //        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        //        if (fileExtension != ".pdf" && fileExtension != ".txt")
        //        {
        //            return BadRequest("Only .txt and .pdf files are supported.");
        //        }

        //        string extractedText = "";

        //        try
        //        {
        //            if (fileExtension == ".pdf")
        //            {
        //                using var pdfStream = file.OpenReadStream();
        //                using var pdfDocument = PdfDocument.Open(pdfStream);

        //                var stringBuilder = new StringBuilder();
        //                foreach (var page in pdfDocument.GetPages())
        //                {
        //                    stringBuilder.AppendLine(page.Text);
        //                }
        //                extractedText = stringBuilder.ToString();
        //            }
        //            else // txt file
        //            {
        //                using var reader = new StreamReader(file.OpenReadStream());
        //                extractedText = await reader.ReadToEndAsync();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, $"Error reading file content: {file.FileName}");
        //            return BadRequest($"Error reading file content: {file.FileName}");
        //        }

        //        if (string.IsNullOrWhiteSpace(extractedText))
        //        {
        //            return BadRequest($"The file contains no readable text: {file.FileName}");
        //        }

        //        List<GeminiService.QuizQuestion> questions;

        //        try
        //        {
        //            questions = await _geminiService.GenerateQuestionsAsync(extractedText);
        //        }
        //        catch (HttpRequestException httpEx)
        //        {
        //            _logger.LogError(httpEx, $"Gemini API request failed for file: {file.FileName}");
        //            return StatusCode(502, new
        //            {
        //                error = $"Error communicating with Gemini API for file: {file.FileName}",
        //                details = httpEx.Message
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, $"Unexpected error generating questions for file: {file.FileName}");
        //            return StatusCode(500, $"An error occurred while generating questions for {file.FileName}.");
        //        }

        //        allQuestions.AddRange(questions);
        //    }

        //    var questionEntities = allQuestions.Select(q => new QuizQuestionEntity
        //    {
        //        QuestionText = q.QuestionText,
        //        CorrectAnswer = q.CorrectAnswer,
        //        AnswersJson = JsonSerializer.Serialize(q.Answers),
        //        ChapterId = chapterId
        //    }).ToList();

        //    _dbContext.QuizQuestions.AddRange(questionEntities);
        //    await _dbContext.SaveChangesAsync();

        //    return Ok(new
        //    {
        //        success = true,
        //        fileCount = files.Count,
        //        questionCount = allQuestions.Count,
        //        questions = allQuestions
        //    });
        //}
//[HttpPost("upload/{chapterId}")]
//public async Task<IActionResult> UploadFileToChapter(int chapterId, IFormFile file)
//{
//    // Validate chapter exists
//    var chapter = await _dbContext.Chapters.FindAsync(chapterId);
//    if (chapter == null)
//    {
//        return NotFound("Chapter not found");
//    }

//    // Validate file presence
//    if (file == null || file.Length == 0)
//    {
//        return BadRequest("Please upload a valid file.");
//    }

//    // Validate file extension
//    if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
//    {
//        return BadRequest("Only .txt files are supported.");
//    }

//    string text;
//    try
//    {
//        using var reader = new StreamReader(file.OpenReadStream());
//        text = await reader.ReadToEndAsync();
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError(ex, "Error reading file content");
//        return BadRequest("Error reading file content.");
//    }

//    if (string.IsNullOrWhiteSpace(text))
//    {
//        return BadRequest("The file contains no text.");
//    }

//    List<GeminiService.QuizQuestion> questions;
//    try
//    {
//        questions = await _geminiService.GenerateQuestionsAsync(text);
//    }
//    catch (HttpRequestException httpEx)
//    {
//        _logger.LogError(httpEx, "Gemini API request failed");
//        return StatusCode(502, new
//        {
//            error = "Error communicating with Gemini API",
//            details = httpEx.Message
//        });
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError(ex, "Unexpected error generating questions");
//        return StatusCode(500, "An error occurred while generating questions.");
//    }

//    var questionEntities = questions.Select(q => new QuizQuestionEntity
//    {
//        QuestionText = q.QuestionText,
//        CorrectAnswer = q.CorrectAnswer,
//        AnswersJson = JsonSerializer.Serialize(q.Answers),
//        ChapterId = chapterId
//    }).ToList();

//    _dbContext.QuizQuestions.AddRange(questionEntities);
//    await _dbContext.SaveChangesAsync();

//    return Ok(new
//    {
//        success = true,
//        count = questions.Count,
//        questions
//    });
//}
//[HttpPost("upload/{chapterId}")]
//public async Task<IActionResult> UploadFileToChapter(int chapterId, [FromForm] IFormFile file)
//{
//    // Validate chapter exists
//    var chapter = await _dbContext.Chapters.FindAsync(chapterId);
//    if (chapter == null)
//    {
//        return NotFound("Chapter not found");
//    }

//    // Validate file presence
//    if (file == null || file.Length == 0)
//    {
//        return BadRequest("Please upload a valid file.");
//    }

//    // Validate file extension (allow .txt and .pdf)
//    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
//    if (fileExtension != ".pdf" && fileExtension != ".txt")
//    {
//        return BadRequest("Only .txt and .pdf files are supported.");
//    }

//    string extractedText = "";

//    try
//    {
//        if (fileExtension == ".pdf")
//        {
//            // Extract text from PDF using PdfPig
//            using var pdfStream = file.OpenReadStream();
//            using var pdfDocument = PdfDocument.Open(pdfStream);

//            var stringBuilder = new StringBuilder();
//            foreach (var page in pdfDocument.GetPages())
//            {
//                stringBuilder.AppendLine(page.Text);
//            }
//            extractedText = stringBuilder.ToString();
//        }
//        else if (fileExtension == ".txt")
//        {
//            // Read txt file content as plain text
//            using var reader = new StreamReader(file.OpenReadStream());
//            extractedText = await reader.ReadToEndAsync();
//        }
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError(ex, "Error reading file content");
//        return BadRequest("Error reading file content.");
//    }

//    if (string.IsNullOrWhiteSpace(extractedText))
//    {
//        return BadRequest("The file contains no readable text.");
//    }

//    List<GeminiService.QuizQuestion> questions;

//    try
//    {
//        questions = await _geminiService.GenerateQuestionsAsync(extractedText);
//    }
//    catch (HttpRequestException httpEx)
//    {
//        _logger.LogError(httpEx, "Gemini API request failed");
//        return StatusCode(502, new
//        {
//            error = "Error communicating with Gemini API",
//            details = httpEx.Message
//        });
//    }
//    catch (Exception ex)
//    {
//        _logger.LogError(ex, "Unexpected error generating questions");
//        return StatusCode(500, "An error occurred while generating questions.");
//    }

//    var questionEntities = questions.Select(q => new QuizQuestionEntity
//    {
//        QuestionText = q.QuestionText,
//        CorrectAnswer = q.CorrectAnswer,
//        AnswersJson = JsonSerializer.Serialize(q.Answers),
//        ChapterId = chapterId
//    }).ToList();

//    _dbContext.QuizQuestions.AddRange(questionEntities);
//    await _dbContext.SaveChangesAsync();

//    return Ok(new
//    {
//        success = true,
//        count = questions.Count,
//        questions
//    });
//}