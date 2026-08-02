using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
namespace TmsApi.Infrastructure.Services;

public interface IEnrollmentService
{
Task<EnrollmentResponseDto?> GetByIdAsync(
    int courseId,
    int id,
    CancellationToken ct);
Task<EnrollmentResponseDto> CreateAsync(
    int courseId,
    EnrollStudentRequest request,
    CancellationToken ct);
Task<List<EnrollmentResponseDto>> GetByCourseAsync(
    int courseId,
    CancellationToken ct);

Task<List<Enrollment>> GetByStudentIdAsync(
    int studentId,
    CancellationToken ct);

}
