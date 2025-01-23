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
public class JobWorker(
    IServiceProvider serviceProvider,
    JobCompletedMgr jobCompletedMgr,
    JobProgressMgr jobProgressMgr,
    JobLogMgr jobLogMgr,
    ILogger<JobRunner> logger) : BackgroundService
{
    public Job? CurrentJob { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken jobToken)
    {
        Job job = CurrentJob!;
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

            jobCompletedMgr.Add(job);
            CurrentJob = null;

            jobLogMgr.CloseJobLogging(job.JobId);
        }

        return;

        async Task SendProgressAsync(double progress, string message)
            => await jobProgressMgr.SendProgressAsync(job, progress, message, jobToken);
    }
}