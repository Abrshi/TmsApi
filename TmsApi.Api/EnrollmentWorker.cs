using Microsoft.Extensions.DependencyInjection;
using TmsApi.Infrastructure.Services;
namespace TmsApi; // Use your actual project namespace here

public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        // TODO 2: Create a short-lived scope using the injected factory.
        using var scope = scopeFactory.CreateScope();

        // TODO 3: Resolve the scoped service from the new scope's provider.
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // TODO 4: Use the service, then let the 'using' block dispose the scope 
        // and its scoped services automatically.
        System.Console.WriteLine("Worker scope created successfully.");
        
        // Example of calling a method on your service (once you implement it):
        // var records = await enrollmentService.GetAllAsync(); 
    }
}