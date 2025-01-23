using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Utilities;

namespace Sockethead.Backgrounder.Jobs;

public enum JobCreateType
{
    Inject,
    New,
    UseExisting,
}

public enum JobStatus
{
    Queued,
    Running,
    Completed,
}

public enum JobResult
{
    Pending,
    Success,
    Cancelled,
    Failed,
}

public class Job
{
    public string JobId { get; internal set; } = Guid.NewGuid().ToString();
    public required string JobName { get; set; }
    public required string ClassFullName { get; set; }
    public string? InitialStateJson { get; set; }  

    public JobCreateType JobCreateType { get; set; } = JobCreateType.Inject;
    
    public IJob? JobObject { get; set; }
    
    public DateTime CreateTime { get; } = DateTime.UtcNow;
    public DateTime? StartTime { get; internal set; }
    public DateTime? EndTime { get; internal set; }

    [Display(Name = "Elapsed")]
    public TimeSpan? ElapsedTime => EndTime - CreateTime;
    public JobStatus JobStatus { get; internal set; } = JobStatus.Queued;
    public JobResult JobResult { get; internal set; } = JobResult.Pending;
    
    private const int MaxDetailsPerJob = 100;
    private CircularBuffer<ProgressDetail> ProgressDetails { get; } = new(MaxDetailsPerJob);

    public void AddProgressDetail(ProgressDetail progressDetail) => ProgressDetails.Add(progressDetail);
    
    public IReadOnlyList<ProgressDetail> GetProgressDetails() => ProgressDetails;

    
    
    /// <summary>
    /// Get the Type of the actual Background Job that this Placeholder is for via Reflection
    /// </summary>
    private Type GetTargetType()
    {
        // look through all the Assemblies in the project
        Type? targetType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(ClassFullName))
            .OfType<Type>()
            .FirstOrDefault();
        
        if (targetType == null)
            throw new InvalidOperationException($"Background Processor could not find type [{ClassFullName}].");
        
        return targetType;
    }

    public IJob ResolveJobObject(IServiceProvider serviceProvider)
    {
        IJob? jobObject = JobCreateType switch
        {
            JobCreateType.Inject => serviceProvider.GetRequiredService(serviceType: GetTargetType()) as IJob,
            JobCreateType.New => Activator.CreateInstance(type: GetTargetType()) as IJob,
            JobCreateType.UseExisting => JobObject,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (jobObject is null)
            throw new InvalidOperationException($"Background Processor could not resolve Job Object for [{ClassFullName}].");
        
        if (InitialStateJson is not null)
            JsonConvert.PopulateObject(InitialStateJson, jobObject);

        return jobObject;
    }
}