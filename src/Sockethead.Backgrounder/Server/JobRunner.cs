using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Models;

namespace Sockethead.Backgrounder.Server;

/// <summary>
/// Background Job Runner manages all the execution of (all) Background Jobs.
/// </summary>
public class JobRunner(
    BackgrounderOptions options,
    IServiceProvider serviceProvider,
    IJobRepo jobRepo,
    ILogger<JobRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken runnerToken)
    {
        logger.LogInformation("Background Job Manager is starting.");

        while (!runnerToken.IsCancellationRequested)
        {
            // Check the Job Queue for something to do
            if (jobRepo.Jobs.Count(j => j.IsRunning) >= options.MaxConcurrentJobs ||
                !jobRepo.TryTakeQueuedJob(out Job? job))
            {
                await Task.Delay(options.MillisecondsBetweenJobChecks, runnerToken);
                continue;
            }

            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(runnerToken);

            JobWorker worker = serviceProvider.GetRequiredService<JobWorker>();
            job.CancellationTokenSource = cts;
            worker.Job = job;
            await worker.StartAsync(cts.Token);
        }

        logger.LogInformation("Background Job Manager is stopping.");
    }
}