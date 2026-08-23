using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Hubs;
using TmsApi.Api.Hubs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v2/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService,
    IHubContext<TmsHub, ITmsHubClient> hubContext)
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

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(
        int courseId,
        int id,
        CancellationToken ct)
    {
        var enrollment = await enrollmentService.ApproveAsync(
            courseId,
            id,
            ct);

        if (enrollment is null)
        {
            return NotFound();
        }

        // The database has been successfully updated/committed.
        // Now notify all connected Angular clients.
        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(
                id.ToString(),
                "Approved");

        return NoContent();
    }
}