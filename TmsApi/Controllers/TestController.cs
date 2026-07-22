using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly TmsDbContext context;

    public TestController(TmsDbContext context)
    {
        this.context = context;
    }

    
    // 1. DEFERRED EXECUTION DEMO
    
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building query (NO DB CALL)");

        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Adding sorting (still NO DB CALL)");

        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Executing query (DB CALL HAPPENS NOW)");

        var results = orderedQuery.ToList();

        Console.WriteLine(">>> STEP 4: Done\n");

        return Ok(results);
    }

    
    // 2. NON-TRANSLATABLE METHOD ERROR
    
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");

        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA)) // ❌ EF cannot translate
                .ToList();

            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION: {ex.Message}\n");

            return BadRequest(new
            {
                Message = ex.Message
            });
        }
    }

    
    // 3. ACTIVE STUDENTS COUNT
    
    [HttpGet("active-count")]
    public async Task<IActionResult> ActiveStudents()
    {
        var count = await context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        return Ok(count);
    }

    
    // 4. MOST ENROLLED COURSES
    
   
    
    // 5. AVERAGE GPA PER COURSE
    
    [HttpGet("avg-gpa")]
    public async Task<IActionResult> AvgGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();

        return Ok(list);
    }

    
    // 6A. STUDENTS WITH NO ENROLLMENTS (NOT ANY)
    
    [HttpGet("no-enrollments-any")]
    public async Task<IActionResult> NoEnrollmentsAny()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(list);
    }

    
    // 6B. LEFT JOIN VERSION
    
    [HttpGet("no-enrollments-join")]
    public async Task<IActionResult> NoEnrollmentsJoin()
    {
        var list = await context.Students
            .GroupJoin(
                context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e }
            )
            .SelectMany(
                x => x.e.DefaultIfEmpty(),
                (x, e) => new { x.s, e }
            )
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("students-paged")]
public async Task<IActionResult> GetStudentsPaged(
    int page = 1,
    int pageSize = 20)
{
    if (page < 1) page = 1;

    var students = await context.Students
        .OrderBy(s => s.Name) // MUST come first
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Ok(students);
}

[HttpGet("top-courses")]
public async Task<IActionResult> TopCourses()
{
    var list = await context.Courses
        .Select(c => new
        {
            c.Title,
            EnrollmentCount = c.Enrollments.Count
        })
        .OrderByDescending(x => x.EnrollmentCount)
        .Take(5)
        .ToListAsync();

    return Ok(list);
}
}