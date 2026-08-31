using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/courses")]
public class CoursesController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public CoursesController(
        TmsDbContext context,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    // GET: /api/v2/courses
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = _context.Courses.AsNoTracking();

        var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Code,
                c.MaxCapacity,
                EnrollmentCount = c.Enrollments.Count
            })
            .ToListAsync(ct);

        var totalPages =
            (int)Math.Ceiling(totalCount / (double)pageSize);

        var hasNext = page < totalPages;
        var hasPrevious = page > 1;

        return Ok(new
        {
            data = rows,

            meta = new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                hasNext,
                hasPrevious
            },

            links = new
            {
                self = $"/api/v2/courses?page={page}&pageSize={pageSize}",

                next = hasNext
                    ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}"
                    : null,

                prev = hasPrevious
                    ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}"
                    : null,

                enroll = "/api/v2/enrollments"
            }
        });
    }


    // PUT: /api/v2/courses/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseDto dto)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return NotFound();
        }

        var authResult = await _authorizationService
            .AuthorizeAsync(User, course, "CanEditCourse");

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        course.Title = dto.Title;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}