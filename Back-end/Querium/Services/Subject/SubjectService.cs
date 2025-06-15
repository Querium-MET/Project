using Microsoft.EntityFrameworkCore;
using Querim.Data;
using Querim.Models;

namespace Querim.Services
{
    public class SubjectService
    {
        private readonly ApplicationDbContext _context;

        public SubjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Subject>> GetSubjectsByAcademicYear(int academicYear)
        {
            return await _context.Subjects
                .Where(s => s.AcademicYear == academicYear)
                .ToListAsync();
        }
    }
}
