using Microsoft.Extensions.DependencyInjection;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.JobManagement;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Jobs;
using Sockethead.Backgrounder.Models;
using Sockethead.Backgrounder.Server;

namespace Sockethead.Backgrounder;

public static class DependencyInjection
{
    public static IServiceCollection RegisterBackgrounderInfrastructure(this IServiceCollection services, 
        Action<BackgrounderOptions>? optionsSetter = null)
    {
        BackgrounderOptions options = new();
        optionsSetter?.Invoke(options);
        
        return services
            .AddSingleton(options)

            // Runner/Background Process
            .AddSingleton<JobRunner>()
            .AddHostedService(provider => provider.GetRequiredService<JobRunner>())

            // Background Managers
            .AddSingleton<JobMgr>()
            .AddSingleton<JobProgressMgr>()
            .AddSingleton<JobLogMgr>()
            .AddSingleton<IJobRepo, JobInMemoryRepo>()
            .AddTransient<JobWorker>()

            // Background Jobs
            .AddScoped<TestSuccessJobInjectable>();
    }
}