using Microsoft.EntityFrameworkCore;
using Querim.Models;


namespace Querim.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuizQuestionEntity> QuizQuestions { get; set; }
        public DbSet<StudentUpload> StudentUploads { get; set; }
        public DbSet<StudentQuiz> StudentQuizzes { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.UniversityIDCard)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.NationalIDCard)
                .IsUnique();

            // Configure relationships and constraints if needed
            modelBuilder.Entity<Chapter>()
                .HasOne(c => c.Subject)
                .WithMany(s => s.Chapters)
                .HasForeignKey(c => c.SubjectId);

         
        }

    }

}
