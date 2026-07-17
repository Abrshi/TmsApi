using Microsoft.AspNetCore.Mvc;
using Tms.Api.Dtos;
using TmsApi.Services.ICourseService;

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

[HttpGet]
public async Task<IActionResult> GetCourses(
[FromQuery] PagedRequest request, CancellationToken ct)
{
var result = await courseService.GetCoursesAsync(request, ct);
return Ok(result);
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