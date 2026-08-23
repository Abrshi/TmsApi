using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace TmsApi.Application.DTOs;

public record GradePayload
{
    [Required]
    [JsonPropertyName("studentId")]
    [Range(1, int.MaxValue, ErrorMessage = "StudentId must be a positive integer.")]
    public required int StudentId { get; init; }

    [Required]
    [JsonPropertyName("courseId")]
    [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a positive integer.")]
    public required int CourseId { get; init; }

    [Required]
    [JsonPropertyName("grade")]
    [Range(0, 100, ErrorMessage = "Grade must be between 0 and 100.")]
    public required decimal Grade { get; init; }
}