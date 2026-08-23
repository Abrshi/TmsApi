using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface ICourseRepository
{
    Task<Course?> GetByCodeAsync(
        string courseCode,
        CancellationToken ct = default);

    Task<List<Course>> GetAllAsync(
        CancellationToken ct = default);
}