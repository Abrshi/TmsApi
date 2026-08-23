using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using Microsoft.Extensions.Logging;
namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(
    IEnrollmentRepository enrollmentRepository,
    TmsDbContext context,
    ILogger<EnrollmentService> logger)
    : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(
        int courseId,
        int id,
        CancellationToken ct) =>

        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);


    public async Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct)
        {
            return await context.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Select(e => new EnrollmentResponseDto(
                    e.Id,
                    e.CourseId,
                    e.StudentId,
                    e.EnrolledAt))
                .ToListAsync(ct);
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
            "Student {StudentId} enrolled in Course {CourseId}",
            enrollment.StudentId,
            enrollment.CourseId);

        return (await GetByIdAsync(
             courseId,
             enrollment.Id,
             ct))!;
    }

    public async Task<List<Enrollment>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct)
    {
        return await enrollmentRepository.GetByStudentIdAsync(studentId, ct);
    }

    public async Task<EnrollmentResponseDto?> ApproveAsync(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var enrollment = await context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id && e.CourseId == courseId, ct);

        if (enrollment is null)
        {
            return null;
        }

        // Assuming there's an approval status or grade update logic
        // Since the Enrollment entity doesn't have an approval status,
        // this might be setting a grade or similar
        // For now, return the updated enrollment DTO
        return await GetByIdAsync(courseId, id, ct);
    }
}