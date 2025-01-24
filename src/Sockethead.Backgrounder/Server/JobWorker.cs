using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.JobManagement;
using Sockethead.Backgrounder.Models;

namespace Sockethead.Backgrounder.Server;

/// <summary>
/// Job Worker takes care of actually running a single Job in the background.
/// </summary>
public class JobWorker(
    IServiceProvider serviceProvider,
    JobProgressMgr jobProgressMgr,
    JobLogMgr jobLogMgr,
    ILogger<JobWorker> logger) : BackgroundService
{
    public Job? Job { get; set; }

    protected override async Task ExecuteAsync(CancellationToken jobToken)
    {
        if (Job is null)
            return;

        logger.LogInformation("Background Job {jobId} executing.", Job.JobId);
        
        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IJob jobObject = JobMgr.ResolveJobObject(Job, scope.ServiceProvider); 
            
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

            //jobCompletedMgr.Add(Job);
            jobLogMgr.CloseJobLogging(Job.JobId);
        }

        logger.LogInformation("Background Job {jobId} completed.", Job.JobId);
        
        return;

        async Task SendProgressAsync(double progress, string message)
            => await jobProgressMgr.SendProgressAsync(Job, progress, message, jobToken);
    }
}