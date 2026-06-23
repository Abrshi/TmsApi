

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
// 1. Register the services with conflicting lifetimes
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// 2. Add host validation to catch illegal lifetime wiring early
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
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