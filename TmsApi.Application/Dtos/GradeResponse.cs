using System.Text.Json.Serialization;
namespace TmsApi.Application.DTOs;

public record GradeResponse(
    [property: JsonPropertyName("enrollmentId")] int EnrollmentId,
    [property: JsonPropertyName("studentId")] int StudentId,
    [property: JsonPropertyName("courseId")] int CourseId,
    [property: JsonPropertyName("grade")] decimal Grade,
    [property: JsonPropertyName("gradedAt")] DateTime GradedAt);