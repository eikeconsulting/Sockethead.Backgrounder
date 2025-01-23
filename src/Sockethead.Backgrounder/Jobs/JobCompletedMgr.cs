using Sockethead.Backgrounder.Utilities;

namespace Sockethead.Backgrounder.Jobs;

public class JobCompletedMgr
{
    private const int MaxJobsToStore = 100;

    private readonly object Mutex = new();

    private CircularBuffer<Job> CompletedJobs { get; set; } = new(MaxJobsToStore);

    public void Add(Job job)
    {
        lock (Mutex)
            CompletedJobs.Add(job);
    }
    
    public IReadOnlyList<Job> GetCompletedJobs()
    {
        lock (Mutex)
            return CompletedJobs.ToArray();        
    }

    public Job? FindJob(string jobId)
    {
        lock (Mutex)
            return CompletedJobs.FirstOrDefault(t => t.JobId == jobId);
    }
    
    public void ClearCompletedJobs()
    {
        lock (Mutex)
            CompletedJobs = new CircularBuffer<Job>(MaxJobsToStore);
    }
}