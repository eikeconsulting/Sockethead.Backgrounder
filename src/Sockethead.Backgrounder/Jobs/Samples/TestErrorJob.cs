using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Contracts;

namespace Sockethead.Backgrounder.Jobs.Samples;

public class TestErrorJobInjectable(ILogger<TestErrorJobInjectable> logger) : IJob
{
    public string JobName => "Test Error Job";
    
    public async Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
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

public class TestErrorJobNewable : IJob
{
    public string JobName => "Test Error Job";
    
    public async Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
        // Simulate job progress
        for (int i = 1; i <= 5; i++)
        {
            if (token.IsCancellationRequested)
                return;
            
            await Task.Delay(1000, token); // Simulate work
            await callback(i / 10.0, $"The iterator is {i}");
        }
        
        throw new Exception("Test Background job failed - this is on purpose!");
    }
}