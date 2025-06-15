using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Dtos;
using Querim.Models;

namespace Querim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeneralController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GeneralController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects(
    [FromQuery] int? academicYear,
    [FromQuery] string? search)  // nullable string, optional
        {
            IQueryable<Subject> query = _context.Subjects;

            if (academicYear.HasValue)
            {
                query = query.Where(s => s.AcademicYear == academicYear.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string loweredSearch = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Title.ToLower().Contains(loweredSearch) ||
                    s.Description != null && s.Description.ToLower().Contains(loweredSearch)
                );
            }

            var subjects = await query
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.AcademicYear,
                    s.Semester
                })
                .ToListAsync();

            return Ok(subjects);


        }
    }
}