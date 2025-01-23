using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sockethead.Backgrounder.Jobs.Samples;

public class TestErrorJob : Job
{
    public override string JobName => "Test Error Job";
    
    public override async Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
        ILogger<TestErrorJob> logger = ServiceProvider.GetRequiredService<ILogger<TestErrorJob>>();

        // Simulate job progress
        for (int i = 1; i <= 5; i++)
        {
            logger.LogInformation("Background job current iterator {i}", i);

            if (token.IsCancellationRequested)
                return;
            
            await Task.Delay(1000, token); // Simulate work
            await callback(i / 10.0, $"The iterator is {i}");
        }
        
        throw new Exception("Test Background job failed - this is on purpose!");
    }
}