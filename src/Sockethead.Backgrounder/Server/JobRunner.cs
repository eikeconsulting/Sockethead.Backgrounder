using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Jobs;

namespace Sockethead.Backgrounder.Background;

/// <summary>
/// Background Job Runner manages all the execution of (all) Background Jobs.
/// RJE - I know we could use Hangfire, but it is a pain to configure and a bit overkill
/// Also, this gives us full control over the process and more flexibility on the UI
/// </summary>
public class JobRunner(
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

    private async Task ExecuteJobAsync(Job job, CancellationToken jobToken)
    {
        CurrentJob = job;

        try
        {
            logger.LogInformation("Background Job executing.");

            using IServiceScope scope = serviceProvider.CreateScope();

            job.ServiceProvider = scope.ServiceProvider;
            if (job is JobPlaceholder placeholder)
                CurrentJob = job = placeholder.ResolveJob();

            job.StartTime = DateTime.UtcNow;
            job.JobStatus = JobStatus.Running;

            await SendProgressAsync(0.0, "Job is starting.");

            using (logger.BeginScope(new Dictionary<string, object>
                   {
                       [ JobLogEventSink.ContextName ] = job.JobId
                   }))
            {
                await job.ExecuteAsync(callback: SendProgressAsync, token: jobToken);
            }

            job.JobResult = JobResult.Success;
        }
        catch (OperationCanceledException e)
        {
            job.JobResult = JobResult.Cancelled;
            await SendProgressAsync(1.0, $"Job cancelled: {e.Message}.");
        }
        catch (Exception e)
        {
            job.JobResult = JobResult.Failed;
            await SendProgressAsync(1.0, $"Job Error: {e.Message}");
            logger.LogError(e, "Background Job Error {jobId} {jobName}.", job.JobId, job.JobName);
        }
        finally
        {
            job.EndTime = DateTime.UtcNow;
            job.JobStatus = JobStatus.Completed;

            await SendProgressAsync(1.0, $"Job is complete!");

            jobCompletedMgr.Add(CurrentJob);
            CurrentJob = null;

            jobLogMgr.CloseJobLogging(job.JobId);
        }

        return;

        async Task SendProgressAsync(double progress, string message)
            => await jobProgressMgr.SendProgressAsync(CurrentJob, progress, message, jobToken);
    }
}