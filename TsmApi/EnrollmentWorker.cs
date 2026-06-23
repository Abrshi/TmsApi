public class EnrollmentWorker
{
    private readonly IEnrollmentService _enrollmentService;

    // The constructor takes the scoped service directly
    public EnrollmentWorker(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    public void ProcessBatch()
    {
        // Simple placeholder for the background task
        System.Console.WriteLine("Processing batch...");
    }
}