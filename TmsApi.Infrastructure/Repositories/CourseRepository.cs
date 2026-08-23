using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly TmsDbContext context;

    public CourseRepository(TmsDbContext context)
    {
        this.context = context;
    }

    public async Task<Course?> GetByCodeAsync(
        string courseCode,
        CancellationToken ct = default)
    {
        return await context.Courses
            .Include(c => c.Enrollments)
            .SingleOrDefaultAsync(
                c => c.Code == courseCode,
                ct);
    }

    public async Task<List<Course>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await context.Courses
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}