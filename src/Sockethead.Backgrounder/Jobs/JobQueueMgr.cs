using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;
using Sockethead.Backgrounder.Contracts;

namespace Sockethead.Backgrounder.Jobs;

public class JobQueueMgr
{
    private BlockingCollection<Job> JobQueue { get; set; } = new();

    public void ClearQueuedJobs() => JobQueue = new BlockingCollection<Job>();

    public IReadOnlyList<Job> GetQueuedJobs() => JobQueue.ToArray();

    public bool TryTakeJob([MaybeNullWhen(false)] out Job job) => JobQueue.TryTake(out job);

    public Job? FindJob(string jobId) => JobQueue.FirstOrDefault(t => t.JobId == jobId);

    
    // TODO: split out the Enqueue business logic to a separate class from the Queue itself

    private static bool HasParameterlessConstructor(Type type)
    {
        return type
            // Get all public instance constructors
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            // Check if any constructor has no parameters
            .Any(ctor => ctor.GetParameters().Length == 0);
    }
    
    public Job EnqueueJob<TJob>(JobCreateType jobCreateType, TJob? jobObject = null, object? initialState = null) 
        where TJob : class, IJob
    {
        Type type = typeof(TJob);
        
        switch (jobCreateType)
        {
            case JobCreateType.Inject:
                if (type.FullName == null)
                    throw new ArgumentNullException(nameof(jobObject), "Unable to resolve type to Inject.");
                break;
            
            case JobCreateType.New:
                if (type.FullName == null)
                    throw new ArgumentException("Unable to resolve FullName of type to New (create).", nameof(jobObject));
                if (!HasParameterlessConstructor(type))
                    throw new ArgumentException("Type must have a parameterless constructor to New (create) it.", nameof(jobObject));
                break;
            
            case JobCreateType.UseExisting:
                if (jobObject == null)
                    throw new ArgumentNullException(nameof(jobObject), "Job Object must be provided for an existing job.");
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(jobCreateType), jobCreateType, null);
        }
        
        Job job = new Job
        {
            JobName = jobObject?.JobName ?? type.Name,
            ClassFullName = type.FullName ?? string.Empty,
            InitialStateJson = initialState == null ? null : JsonConvert.SerializeObject(initialState),
            JobCreateType = jobCreateType,
        };

        JobQueue.Add(job);
        
        return job;
    }
    
#if false
    /// <summary>
    /// Queue a Job (FIFO) for processing
    /// The Job itself will be kept in memory until it is run
    /// If the Queue is to be Serialized (for durability) then it MUST have a parameterless
    /// constructor because it may not participate in Dependency Injection;
    /// this requirement is enforced by the generic parameterization.
    /// </summary>
    /// <param name="prepareJob">an optional callback to prepare the job for execution.
    /// This allows you to effectively pass the data to the Job needed for execution instance.</param>
    /// <typeparam name="TJob">The of the Job</typeparam>
    /// <returns>the JobId of the Job created.</returns>
    public string EnqueueJob<TJob>(
        Action<TJob>? prepareJob = null) 
        where TJob : Job, new()
    {
        TJob job = new TJob();
        
        prepareJob?.Invoke(job);
        
        JobQueue.Add(job);
        
        return job.JobId;
    }

    /// <summary>
    /// Queue a Job by Dependency Injection (DI)
    /// A Placeholder Job will be placed on the Queue and when ready to be executed, the
    /// actual Background Job will be generated via DI through the ServiceProvider.
    /// The object will exist in a newly generated "Scope" explicitly created for running it.
    /// </summary>
    /// <param name="initialState">an optional object to initialize your class with.
    /// This will be applied first.</param>
    /// <param name="prepareJob">an optional callback to prepare the job for execution.
    /// This allows you to effectively pass the data to the job needed for execution instance.</param>
    /// <typeparam name="TJob">The type that should be generated via DI</typeparam>
    /// <returns>the JobId of the Placeholder Job which will transfer over to the actual Job when created.</returns>
    public string EnqueueJobDI<TJob>(
        object? initialState = null,
        Action<TJob>? prepareJob = null) 
        where TJob : Job
    {
        JobPlaceholder placeholder = new(typeof(TJob));

        if (initialState is not null)
            placeholder.InitialStateJson = JsonConvert.SerializeObject(initialState);
        
        if (prepareJob is not null)
            placeholder.PrepareJob = o => prepareJob((TJob)o);

        JobQueue.Add(placeholder);
        
        return placeholder.JobId;
    }
#endif
}