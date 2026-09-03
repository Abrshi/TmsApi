using NSubstitute;
using TmsApi.Application.Common;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Tests;

public class EnrollStudentHandlerTests
{
    [Fact]
    public async Task Handle_WhenAlreadyEnrolled_ReturnsDuplicateError()
    {
        // Arrange
        var enrollmentRepo = Substitute.For<IEnrollmentRepository>();
        var courseRepo = Substitute.For<ICourseRepository>();

        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced Web Dev",
            MaxCapacity = 30,
            Enrollments = new List<Enrollment>()
        };

        courseRepo
            .GetByCodeAsync("CS-401", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        enrollmentRepo
            .ExistsAsync(99, "CS-401", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var handler = new EnrollStudentHandler(
            enrollmentRepo,
            courseRepo);

        var command = new EnrollStudentCommand(
            StudentId: 99,
            CourseCode: "CS-401");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("already_enrolled", result.Error.Code);

        Assert.Equal(
            EnrollmentError.AlreadyEnrolled(99, "CS-401"),
            result.Error);

        await enrollmentRepo
            .DidNotReceive()
            .AddAsync(
                Arg.Any<Enrollment>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCourseFull_ReturnsCapacityError()
    {
        // Arrange
        var enrollmentRepo = Substitute.For<IEnrollmentRepository>();
        var courseRepo = Substitute.For<ICourseRepository>();

        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced Web Dev",
            MaxCapacity = 35,
            Enrollments = Enumerable.Range(1, 35)
                .Select(i => new Enrollment
                {
                    Id = i,
                    CourseId = 1
                })
                .ToList()
        };

        courseRepo
            .GetByCodeAsync("CS-401", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        var handler = new EnrollStudentHandler(
            enrollmentRepo,
            courseRepo);

        var command = new EnrollStudentCommand(
            StudentId: 100,
            CourseCode: "CS-401");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("course_full", result.Error.Code);

        Assert.Equal(
            EnrollmentError.CourseFull(
                "Advanced Web Dev",
                35),
            result.Error);

        await enrollmentRepo
            .DidNotReceive()
            .AddAsync(
                Arg.Any<Enrollment>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SuccessfulPath_AddsEnrollmentOnce()
    {
        // Arrange
        var enrollmentRepo = Substitute.For<IEnrollmentRepository>();
        var courseRepo = Substitute.For<ICourseRepository>();

        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced Web Dev",
            MaxCapacity = 35,
            Enrollments = Enumerable.Range(1, 20)
                .Select(i => new Enrollment
                {
                    Id = i,
                    CourseId = 1
                })
                .ToList()
        };

        courseRepo
            .GetByCodeAsync("CS-401", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Course?>(course));

        enrollmentRepo
            .ExistsAsync(
                100,
                "CS-401",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = new EnrollStudentHandler(
            enrollmentRepo,
            courseRepo);

        var command = new EnrollStudentCommand(
            StudentId: 100,
            CourseCode: "CS-401");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.StudentId);
        Assert.Equal("CS-401", result.Value.CourseCode);

        await enrollmentRepo
            .Received(1)
            .AddAsync(
                Arg.Is<Enrollment>(e =>
                    e.StudentId == 100 &&
                    e.CourseId == 1),
                Arg.Any<CancellationToken>());
    }
}