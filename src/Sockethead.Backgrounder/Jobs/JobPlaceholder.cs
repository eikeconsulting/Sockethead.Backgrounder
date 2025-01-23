using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Sockethead.Backgrounder.Jobs;

#if false
internal class JobPlaceholderX(Type type) : Job
{
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
    public Job ResolveJob(IServiceProvider serviceProvider)
    {
        Job job = (Job)serviceProvider.GetRequiredService(serviceType: GetTargetType());
        job.JobId = JobId;

        if (InitialStateJson is not null)
            JsonConvert.PopulateObject(InitialStateJson, job);
        
        PrepareJob?.Invoke(job);
        return job;
    }
}
#endif
