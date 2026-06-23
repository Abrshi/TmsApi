

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>(); 
// Pipeline
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Protected endpoint
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();