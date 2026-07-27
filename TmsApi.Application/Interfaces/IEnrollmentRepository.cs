using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentRepository
{
    Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct = default);

    Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct = default);

    Task<List<Enrollment>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct = default);
}