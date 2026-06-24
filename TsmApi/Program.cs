
using TmsApi; // Or use the exact namespace found at the top of your EnrollmentWorker.cs file
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
// Exercise 3 - Options Pattern
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();    


// These registrations are given do NOT change them:
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

// 2. Add host validation to catch illegal lifetime wiring early
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddControllers();

var app = builder.Build();
app.UseExceptionHandler();

app.UseStatusCodePages();
app.UseMiddleware<RequestLoggingMiddleware>(); 
// Pipeline
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Protected endpoint
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();


app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException(
        "Simulated database failure for ProblemDetails testing");
});
app.Run();