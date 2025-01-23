using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Contracts;

namespace Sockethead.Backgrounder.Jobs.Samples;

public class TestSuccessJobInjectable(ILogger<TestSuccessJobInjectable> logger) : IJob
{
    public string JobName => "Test Success Job";

    public int Start { get; set; } = 1;
    public int End { get; set; } = 10;
    
    public async Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
        double total = End - Start;
        
        // Simulate job progress
        for (int i = Start; i < End; i++)
        {
            logger.LogInformation("Background job current iterator {i}", i);

            if (token.IsCancellationRequested)
                return;
            
            await callback((i - Start) / total, $"The iterator is {i}");
            await Task.Delay(1000, token); // Simulate work
        }
    }
}

public class TestSuccessJobNewable : IJob
{
    public string JobName => "Test Success Job";

    public int Start { get; set; } = 1;
    public int End { get; set; } = 10;
    
    public async Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
        double total = End - Start;
        
        // Simulate job progress
        for (int i = Start; i < End; i++)
        {
            if (token.IsCancellationRequested)
                return;
            
            await callback((i - Start) / total, $"The iterator is {i}");
            await Task.Delay(1000, token); // Simulate work
        }
    }
}