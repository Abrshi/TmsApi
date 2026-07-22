namespace TmsApi.Domain.Entities;

using System.ComponentModel.DataAnnotations;

public class Student
{
    // surrogate primary key — internal use only
    public int Id { get; set; }

    // natural key — human-readable unique identifier
    public required string RegistrationNumber { get; set; }

    public string? Name { get; set; }

    public decimal GPA { get; set; }

    public bool IsActive { get; set; } = true;

    // concurrency control (will be used in Exercise 8)
    [Timestamp]
    public byte[] RowVersion { get; set; }

    // many-to-many navigation property
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public uint Version { get; set; }

    public bool IsDeleted { get; set; }
}