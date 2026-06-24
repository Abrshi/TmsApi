using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await _enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await _enrollmentService.GetByIdAsync(id);

        return record is not null
            ? Ok(record)
            : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateEnrollmentRequest request)
    {
        var record = await _enrollmentService.EnrollAsync(
            request.StudentId,
            request.CourseCode);

        return CreatedAtAction(
            nameof(GetById),
            new { id = record.Id },
            record);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _enrollmentService.DeleteAsync(id);

        return deleted
            ? NoContent()
            : NotFound();
    }
}

public record CreateEnrollmentRequest(
    string StudentId,
    string CourseCode);