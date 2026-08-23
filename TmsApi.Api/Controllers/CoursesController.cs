using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService)
    : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(
        int id,
        CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        if (course is null)
        {
            return NotFound();
        }

        return Ok(course);
    }


  [HttpPost]
public async Task<IActionResult> CreateCourse(
    CreateCourseRequest request,
    CancellationToken ct)
{
    if (await courseService.CodeExistsAsync(request.Code, ct))
    {
        return Conflict(new ProblemDetails
        {
            Title = "Course code already exists",
            Detail = $"A course with code '{request.Code}' is already registered.",
            Status = StatusCodes.Status409Conflict
        });
    }

    var result = await courseService.CreateAsync(request, ct);

    return CreatedAtAction(
        nameof(GetCourseById),
        new { id = result.Id },
        result);
}
}
[ApiController]
[Route("api/v2/transcripts")]
public class TranscriptsController : ControllerBase
{
[HttpPost]
[EnableRateLimiting("transcripts")]
public IActionResult RequestTranscript([FromBody] object? _)
{
// Stub: Exercise 5 swaps this for enqueue + 202 + Location.
return Ok();
}
}
