using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Sockethead.Backgrounder.Jobs;

internal class JobPlaceholder(Type type) : Job
{
    public override string JobName => type.Name;
    public override string ClassFullName => type.FullName!;

    public string? InitialStateJson { get; set; }  
    
    /// <summary>
    /// This will be called immediately after the actual Background Job is constructed
    /// </summary>
    internal Action<object>? PrepareJob { get; set; }

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

    /// <summary>
    /// Resolve the actual Background Job for this Placeholder
    /// Move both the ServiceProvider and JobId over
    /// </summary>
    public Job ResolveJob()
    {
        Job job = (Job)ServiceProvider.GetRequiredService(serviceType: GetTargetType());
        job.ServiceProvider = ServiceProvider;
        job.JobId = JobId;

        if (InitialStateJson is not null)
            JsonConvert.PopulateObject(InitialStateJson, job);
        
        PrepareJob?.Invoke(job);
        return job;
    }
    
    public override Task ExecuteAsync(ProgressCallback callback, CancellationToken token)
    {
        throw new NotImplementedException("Background Job Placeholder ExecuteAsync should never be called.");
    }
}