using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Jobs;

namespace Sockethead.Backgrounder.Background;
#if false
/// <summary>
/// Background Job Runner manages all the execution of (all) Background Jobs.
/// RJE - I know we could use Hangfire, but it is a pain to configure and a bit overkill
/// Also, this gives us full control over the process and more flexibility on the UI
/// </summary>
public class JobScheduler(
    IServiceProvider serviceProvider,
    JobQueueMgr jobQueueMgr,
    JobCompletedMgr jobCompletedMgr,
    JobProgressMgr jobProgressMgr,
    JobLogMgr jobLogMgr,
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
            await Task.Delay(MillisecondsBetweenJobChecks, runnerToken);
            if (runnerToken.IsCancellationRequested)
                break;

            // Check the Job Queue for something to do
            if (!jobQueueMgr.TryTakeJob(out Job? job))
                continue;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(runnerToken);

            await ExecuteJobAsync(job: job, jobToken: _cts.Token);
        }

        logger.LogInformation("Background Job Manager is stopping.");
    }
}
#endif