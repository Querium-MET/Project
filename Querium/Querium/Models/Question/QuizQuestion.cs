// Models/QuizQuestion.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Querim.Models
{
    public class QuizQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        public string CorrectAnswer { get; set; } = string.Empty;

        public string AnswersJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }

        public List<string> GetAnswers() =>
            JsonSerializer.Deserialize<List<string>>(AnswersJson) ?? new List<string>();
    }

}