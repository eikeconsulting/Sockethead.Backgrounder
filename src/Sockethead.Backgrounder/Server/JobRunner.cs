using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Jobs;

namespace Sockethead.Backgrounder.Background;

/// <summary>
/// Background Job Runner manages all the execution of (all) Background Jobs.
/// </summary>
public class JobRunner(
    IServiceProvider serviceProvider,
    JobQueueMgr jobQueueMgr,
    ILogger<JobRunner> logger) : BackgroundService
{
    private const int MillisecondsBetweenJobChecks = 1000; // 1 second
    
    private CancellationTokenSource? _cts;

    public Job? CurrentJob { get; private set; }

    public void CancelCurrentJob() => _cts?.Cancel();

    protected override async Task ExecuteAsync(CancellationToken runnerToken)
    {
        logger.LogInformation("Background Job Manager is starting.");

        while (!runnerToken.IsCancellationRequested)
        {
            // Check the Job Queue for something to do
            if (!jobQueueMgr.TryTakeJob(out Job? job))
            {
                await Task.Delay(MillisecondsBetweenJobChecks, runnerToken);
                continue;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(runnerToken);

            using IServiceScope scope = serviceProvider.CreateScope();
            JobWorker worker = scope.ServiceProvider.GetRequiredService<JobWorker>();
            worker.Job = job;
            await worker.StartAsync(_cts.Token);
        }

        logger.LogInformation("Background Job Manager is stopping.");
    }
}