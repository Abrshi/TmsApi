using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly TmsDbContext context;

    public EnrollmentRepository(TmsDbContext context)
    {
        this.context = context;
    }


    public async Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);
    }


    public async Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct)
    {
        return await context.Enrollments
            .Include(e => e.Course)
            .AnyAsync(
                e => e.StudentId == studentId &&
                     e.Course.Code == courseCode,
                ct);
    }


    public async Task<List<Enrollment>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct)
    {
        return await context.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);
    }
}