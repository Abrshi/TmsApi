using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;

using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.Persistence.Configurations;

namespace TmsApi.Api.Controllers; // ✅ Fix: Matches TmsApi.Api format

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) // ✅ Fix: Clean interface reference
    : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    public async Task<IActionResult> GetEnrollment(
        int courseId,
        int id,
        CancellationToken ct)
    {
       var enrollment = await enrollmentService.GetByIdAsync(
        courseId,
        id,
        ct);

        return enrollment is not null
            ? Ok(enrollment)
            : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> EnrollStudent(
        int courseId,
        EnrollStudentRequest request,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(
            courseId,
            ct);

        if (course is null)
        {
            return NotFound();
        }

        if (course.EnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course is full",
                Detail =
                    $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
                Status = StatusCodes.Status409Conflict
            });
        }

         var enrollment = await enrollmentService.CreateAsync(
                courseId,
                request,
                ct);


        return CreatedAtAction(
            nameof(GetEnrollment),
            new
            {
                courseId,
                id = enrollment.Id
            },
            enrollment);
    }
}