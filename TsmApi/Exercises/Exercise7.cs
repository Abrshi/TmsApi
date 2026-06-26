using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Exercises;
public static class Exercise7
{
    public static async Task Run(TmsDbContext db)
    {
        var students = await db.Students
            .AsNoTracking()
            .ToListAsync();

        foreach (var s in students)
        {
            var count = await db.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id);

            Console.WriteLine($"{s.Name}: {count} enrollments");
        }
    }
}