
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using Tms.Api.Dtos;
using TmsApi.Data;


namespace TmsApi.Infrastructure.Services;


public class EnrollmentService(
    TmsDbContext context,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct)
    {
        return context.Enrollments
            .AsNoTracking()
            .Where(e =>
                e.Id == id &&
                e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EnrollmentResponseDto> CreateAsync(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Student {StudentId} enrolled in course {CourseId}. Enrollment ID: {EnrollmentId}",
            request.StudentId,
            courseId,
            enrollment.Id);

        var result = await GetByIdAsync(
            courseId,
            enrollment.Id,
            ct);

        return result
            ?? throw new InvalidOperationException(
                "Enrollment was created but could not be retrieved.");
    }
}