using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Exercises;

public static class Exercise7
{
    public static async Task Run(TmsDbContext db)
    {
        var students = await db.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .ToListAsync();

        foreach (var s in students)
        {
            Console.WriteLine($"{s.Name}: {s.Enrollments.Count} enrollments");
        }
    }
}