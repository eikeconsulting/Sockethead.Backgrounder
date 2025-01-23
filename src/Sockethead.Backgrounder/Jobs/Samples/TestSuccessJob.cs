using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sockethead.Backgrounder.Jobs.Samples;

public class TestSuccessJob : Job
{
    public override string JobName => "Test Success Job";

    public int Start { get; set; } = 1;
    public int End { get; set; } = 10;
    
    public override async Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
        ILogger<TestSuccessJob> logger = ServiceProvider.GetRequiredService<ILogger<TestSuccessJob>>();

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