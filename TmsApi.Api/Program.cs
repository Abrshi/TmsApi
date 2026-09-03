using Scalar.AspNetCore;
using TmsApi;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using TmsApi.Exercises;
using TmsApi.Infrastructure.Services;
using Asp.Versioning;
using TmsApi.Api.Middleware;
using TmsApi.Api.Hubs;

using TmsApi.Application.Enrollments.Commands;

using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Repositories;


using FluentValidation;
using MediatR;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Application.Behaviors;

using Microsoft.Extensions.Caching.Hybrid;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Infrastructure.Transcripts;
using Microsoft.AspNetCore.Antiforgery;
using TmsApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tms.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);

//  SERVICES 
builder.Services.AddCors(options =>
{
options.AddPolicy("AllowAngular", policy =>
policy.WithOrigins("http://localhost:4200")
.AllowAnyHeader()
.AllowAnyMethod());
});
builder.Services.AddRateLimiter(options =>
{
options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext,
string>(httpContext =>
{
var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);
return tier switch
{ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
partitionKey: $"paid:{partitionKey}",
factory: _ => new TokenBucketRateLimiterOptions
{
TokenLimit = 200,
TokensPerPeriod = 100,
ReplenishmentPeriod = TimeSpan.FromSeconds(10),
QueueLimit = 0,
AutoReplenishment = true
}),
ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
partitionKey: $"free:{partitionKey}",
factory: _ => new TokenBucketRateLimiterOptions
{
TokenLimit = 30,
TokensPerPeriod = 10,
ReplenishmentPeriod = TimeSpan.FromSeconds(10),
QueueLimit = 0,
AutoReplenishment = true
}),
_ => RateLimitPartition.GetTokenBucketLimiter(
partitionKey: $"anon:{partitionKey}",
factory: _ => new TokenBucketRateLimiterOptions
{
TokenLimit = 10,
TokensPerPeriod = 5,
ReplenishmentPeriod = TimeSpan.FromSeconds(10),
QueueLimit = 0,
AutoReplenishment = true
})
};
});
options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
options.OnRejected = async (context, ct) =>
{
var retryAfter = "10";
if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var
ts))
retryAfter = ((int)ts.TotalSeconds).ToString();
context.HttpContext.Response.Headers.RetryAfter = retryAfter;
context.HttpContext.Response.ContentType =
"application/problem+json";
await context.HttpContext.Response.WriteAsJsonAsync(new
ProblemDetails
{
Title = "Rate limit exceeded",
Detail = $"Too many requests. Retry after {retryAfter} seconds.",
Status = StatusCodes.Status429TooManyRequests,
Type = "https://tms.local/errors/rate_limit_exceeded"
}, ct);
};
});

builder.Services.AddMediatR(cfg =>
cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);
// LoggingBehavior FIRST—it must wrap ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>),typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
 new BoundedChannelOptions(100)
 {
 FullMode = BoundedChannelFullMode.Wait
 }));

 builder.Services.AddHostedService<TranscriptWorker>();

builder.Services.AddSignalR();



builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
// builder.Services.AddAuthentication();
// builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
//

// Exercise 3 - Options Pattern
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Given registrations (DO NOT CHANGE)
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// Host validation
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// m7s1
builder.Services.AddApiVersioning(options =>
{
options.DefaultApiVersion = new ApiVersion(1, 0);
options.AssumeDefaultVersionWhenUnspecified = true;
options.ReportApiVersions = true;
options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
options.GroupNameFormat = "'v'VVV";
options.SubstituteApiVersionInUrl = true;
});
// ===============

builder.Services.AddControllers();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});
// Register TmsDbContext scoped for incoming HTTP requests

builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information) // Log SQLto output window
.EnableSensitiveDataLogging()); // Show parameters in querylogs (dev only)
builder.Services.AddIdentityCore<TmsUser>(options =>
{
// Enterprise Password Policy
options.Password.RequiredLength = 12;
options.Password.RequireUppercase = true;
options.Password.RequireDigit = true;
options.Password.RequireNonAlphanumeric = true;
// Brute-Force Lockout Protection
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>();
builder.Services.AddRateLimiter(options =>
{
options.AddFixedWindowLimiter("AuthLimiter", opt =>
{
opt.PermitLimit = 5;
opt.Window = TimeSpan.FromMinutes(1);
opt.QueueLimit = 0;
});
});
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();

builder.Services.AddScoped<TokenService>();
builder.Services.AddAuthentication(options =>
{
options.DefaultAuthenticateScheme =
JwtBearerDefaults.AuthenticationScheme;
options.DefaultChallengeScheme =
JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
options.TokenValidationParameters = new TokenValidationParameters{
ValidateIssuer = true,
ValidateAudience = true,
ValidateLifetime = true,
ValidateIssuerSigningKey = true,
ValidIssuer = builder.Configuration["Jwt:Issuer"],
ValidAudience = builder.Configuration["Jwt:Audience"],
IssuerSigningKey = new SymmetricSecurityKey(
Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))};
});


builder.Services.AddAuthorizationBuilder()
.AddPolicy("CanEditCourse", policy =>
policy.Requirements.Add(new CourseInstructorRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, CourseInstructorHandler>();
builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddHybridCache(options =>
{
options.DefaultEntryOptions = new HybridCacheEntryOptions
{
Expiration = TimeSpan.FromMinutes(10),
LocalCacheExpiration = TimeSpan.FromMinutes(2)
};
});
// Production-only leave commented in lab
// builder.Services.AddStackExchangeRedisCache(options =>
// {
// options.Configuration =builder.Configuration.GetConnectionString("Redis");
// options.InstanceName = "tms:";
// });
// builder.Services.AddHybridCache();

builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
// Load allowed origins from appsettings.Development.json
var allowedOrigins = builder.Configuration
.GetSection("AllowedOrigins").Get<string[]>()
?? ["http://localhost:4200"];
// Register the CORS policy in the Dependency Injection container
builder.Services.AddCors(options =>
{
options.AddPolicy("TmsClient", policy =>
{
policy.WithOrigins(allowedOrigins)
.AllowAnyHeader()
.AllowAnyMethod()
.AllowCredentials() // Vital for HttpOnly auth cookies in Session 2
.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
});
});
var app = builder.Build();
app.MapHub<TmsHub>("/hubs/tms");

// app.UseCors("AllowAngular");
 app.UseExceptionHandler();
//  MIDDLEWARE 

// Routing must come early
app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "X-Content-Type-Options",
        "nosniff");

    context.Response.Headers.Append(
        "X-Frame-Options",
        "DENY");

    context.Response.Headers.Append(
        "Referrer-Policy",
        "strict-origin-when-cross-origin");

    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");

    await next();
});
app.UseRouting();
app.UseRateLimiter();
// CRITICAL: Middleware order matters!
// UseRouting -> UseCors -> UseAuthentication -> UseAuthorization
app.UseCors("TmsClient");
// Global error handling (ProblemDetails)
app.UseExceptionHandler();

// Converts empty responses (like 404) into JSON ProblemDetails
app.UseStatusCodePages();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
if (context.User.Identity?.IsAuthenticated == true || context.Request.Cookies.ContainsKey("tms_auth"))
{
var antiforgery = context.RequestServices
.GetRequiredService<IAntiforgery>();
var tokens = antiforgery.GetAndStoreTokens(context);
context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
new CookieOptions
{
HttpOnly = false, // MUST be false so Angular JavaScript can read it!
Secure = !builder.Environment.IsDevelopment(),SameSite = SameSiteMode.Strict
});
}
await next(context);
});
app.UseMiddleware<V1DeprecationMiddleware>();
app.MapControllers();

//  ENVIRONMENT TOOLS 
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//  TEST ENDPOINTS 

// Protected endpoint
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}));


// Error test endpoint (for ProblemDetails)
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException(
        "Simulated database failure for ProblemDetails testing");
});



// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

    context.Database.Migrate(); // Applies any pending migrations; keeps migration history intact

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };

        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
        };

        context.Courses.AddRange(courses);

        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 95.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 90.0m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 80.0m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 85.0m }
        };

        context.Enrollments.AddRange(enrollments);

        context.SaveChanges();
    }
}
Console.WriteLine("starting Exercise 7: Count enrollments for each student...");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

    await Exercise7.Run(db);
}
Console.WriteLine("Exercise 7 completed.");
app.Run();