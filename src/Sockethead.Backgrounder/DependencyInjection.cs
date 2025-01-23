using Microsoft.Extensions.DependencyInjection;
using Sockethead.Backgrounder.Background;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Jobs;
using Sockethead.Backgrounder.Jobs.Samples;

namespace Sockethead.Backgrounder;

public static class DependencyInjection
{
    public static IServiceCollection RegisterBackgrounderInfrastructure(this IServiceCollection services) =>
        services

            // Runner/Background Process
            .AddSingleton<JobRunner>()
            .AddHostedService(provider => provider.GetRequiredService<JobRunner>())
                
            // Background Managers
            .AddSingleton<JobQueueMgr>()
            .AddSingleton<JobCompletedMgr>()
            .AddSingleton<JobProgressMgr>()
            .AddSingleton<JobLogMgr>()
            .AddSingleton<JobResolver>()
            .AddSingleton<JobWorker>()

            // Background Jobs
            .AddScoped<TestSuccessJobInjectable>()
        ;
}