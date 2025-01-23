using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Contracts;
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
    ILogger<JobWorker> logger) : BackgroundService
{
    public Job? Job { get; set; }

    protected override async Task ExecuteAsync(CancellationToken jobToken)
    {
        logger.LogInformation("Background Job executing.");

        if (Job is null)
            return;
        
        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IJob jobObject = Job.ResolveJobObject(scope.ServiceProvider); 
            
            Job.StartTime = DateTime.UtcNow;
            Job.JobStatus = JobStatus.Running;

            await SendProgressAsync(0.0, "Job is starting.");

            using (logger.BeginScope(JobLogEventSink.JobLogScope(Job.JobId)))
            {
                await jobObject.ExecuteAsync(callback: SendProgressAsync, token: jobToken);
            }

            Job.JobResult = JobResult.Success;
        }
        catch (OperationCanceledException e)
        {
            Job.JobResult = JobResult.Cancelled;
            await SendProgressAsync(1.0, $"Job cancelled: {e.Message}.");
        }
        catch (Exception e)
        {
            Job.JobResult = JobResult.Failed;
            await SendProgressAsync(1.0, $"Job Error: {e.Message}");
            logger.LogError(e, "Background Job Error {jobId} {jobName}.", Job.JobId, Job.JobName);
        }
        finally
        {
            Job.EndTime = DateTime.UtcNow;
            Job.JobStatus = JobStatus.Completed;

            await SendProgressAsync(1.0, $"Job is complete!");

            jobCompletedMgr.Add(Job);
            jobLogMgr.CloseJobLogging(Job.JobId);
        }

        return;

        async Task SendProgressAsync(double progress, string message)
            => await jobProgressMgr.SendProgressAsync(Job, progress, message, jobToken);
    }
}