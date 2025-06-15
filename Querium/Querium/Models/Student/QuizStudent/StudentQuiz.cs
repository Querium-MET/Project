using System.ComponentModel.DataAnnotations;

namespace Querim.Models
{
    public class StudentQuiz
    {
        [Key]
        public int Id { get; set; }
        public int UploadId { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }

        [Required]
        public string QuestionText { get; set; }

        [Required]
        public string QuestionAnswersJson { get; set; }

        [Required]
        public string QuestionCorrectAnswer { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
