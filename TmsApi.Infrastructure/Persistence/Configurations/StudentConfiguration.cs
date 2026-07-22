using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        // Primary Key
        builder.HasKey(s => s.Id);

        // Required fields
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.GPA)
            .HasPrecision(3, 2);

        // Shadow Property
        builder.Property<DateTime>("LastUpdated");

        // Concurrency Token
        builder.Property(s => s.Version)
            .IsRowVersion();

        // Relationships
        builder.HasMany(s => s.Enrollments)
            .WithOne(e => e.Student)
            .HasForeignKey(e => e.StudentId);


            // exersise 9
        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}