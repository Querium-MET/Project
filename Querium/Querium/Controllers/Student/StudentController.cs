
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Dtos;
using Querim.Models;
using Querim.Services;
using System.Threading.Tasks;
using BCrypt.Net;
namespace Querim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {


        public static string HashPassword(string password)
        {
            // The WorkFactor is 12 by default, adjusts hash complexity
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
        }
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] StudentRegisterDto registerDto)
        {
            if (await _context.Students.AnyAsync(s => s.Email == registerDto.Email || s.NationalIDCard == registerDto.NationalIDCard || s.UniversityIDCard == registerDto.UniversityIDCard))
            {
                return BadRequest(new { message = "already exists" });
            }

            var student = new Student
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                UniversityIDCard = registerDto.UniversityIDCard,
                NationalIDCard = registerDto.NationalIDCard
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Registration successful, pending approval",
                student = new
                {
                    student.Id,
                    student.FullName,
                    student.Email,
                    student.UniversityIDCard,
                    student.NationalIDCard,
                    student.IsApproved,
                    student.IsDeleted,
                    student.CreatedAt
                }
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] StudentLoginDto loginDto)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == loginDto.Email && !s.IsDeleted);

            if (student == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, student.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            if (!student.IsApproved)
            {
                return Unauthorized(new { message = "Account not approved" });
            }

            return Ok(new
            {
                message = "Login successful",
                student = new
                {
                    student.Id,
                    student.FullName,
                    student.Email,
                    student.UniversityIDCard,
                    student.NationalIDCard,
                    student.IsApproved,
                    student.IsDeleted,
                    student.CreatedAt
                }
            });
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Implement logout logic (e.g., clear session)
            return Ok(new { message = "Logout successful" });
        }
        [HttpGet("Profile/{id}")]
        public async Task<IActionResult> GetUserInfo(string id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UniversityIDCard == id);

            if (student == null)
                return NotFound();

            return Ok(student);
        }
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.NewPassword != model.ConfirmNewPassword)
                return BadRequest(new { message = "New passwords do not match." });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UniversityIDCard == model.UniversityIDCard);
            if (student == null)
                return NotFound("User not found.");

            // Verify current password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, student.PasswordHash))
                return BadRequest("Current password is incorrect.");

            // Hash the new password and update
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }
        //}
    }
}
