using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Sockethead.Backgrounder.Jobs;

public class JobQueueMgr
{
    private BlockingCollection<Job> JobQueue { get; set; } = new();

    public void ClearQueuedJobs() => JobQueue = new BlockingCollection<Job>();

    public IReadOnlyList<Job> GetQueuedJobs() => JobQueue.ToArray();

    public bool TryTakeJob([MaybeNullWhen(false)] out Job job) => JobQueue.TryTake(out job);

    public Job? FindJob(string jobId) => JobQueue.FirstOrDefault(t => t.JobId == jobId);

    // TODO: split out the Enqueue business logic to a separate class from the Queue itself
    
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
}