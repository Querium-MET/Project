using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Querim.Models
{
    public class QuizQuestionEntity
    {
        [Key]
        public int Id { get; set; }

        public string QuestionText { get; set; } = string.Empty;
        public string AnswersJson { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;

        // Foreign key to Chapter
        [Required]
        public int ChapterId { get; set; }

        [ForeignKey("ChapterId")]
        public Chapter Chapter { get; set; }
    }
}
