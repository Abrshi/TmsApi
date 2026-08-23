using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/grades")]
public class GradesController(
    TmsDbContext context,
    ILogger<GradesController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostGrade(
        GradePayload payload,
        CancellationToken ct)
    {
        var student = await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == payload.StudentId, ct);
        if (student is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student not found",
                Detail = $"No student exists with id {payload.StudentId}.",
                Status = StatusCodes.Status404NotFound
            });
        }
        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == payload.CourseId, ct);

        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail = $"No course exists with id {payload.CourseId}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var enrollment = await context.Enrollments
            .FirstOrDefaultAsync(e =>
                e.StudentId == payload.StudentId &&
                e.CourseId == payload.CourseId, ct);

        if (enrollment is null)
        {
            enrollment = new Enrollment
            {
                StudentId = payload.StudentId,
                CourseId = payload.CourseId,
                EnrolledAt = DateTime.UtcNow
            };
            context.Enrollments.Add(enrollment);
        }

        enrollment.Grade = payload.Grade;

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded grade {Grade} for Student {StudentId} in Course {CourseId}",
            enrollment.Grade,
            enrollment.StudentId,
            enrollment.CourseId);

        var response = new GradeResponse(
            enrollment.Id,
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.Grade.Value,
            enrollment.EnrolledAt);

        return Ok(response);
    }
}
