using System.Diagnostics.CodeAnalysis;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Models;

namespace Sockethead.Backgrounder.JobManagement;

public class JobInMemoryRepo : IJobRepo
{
    private readonly object Mutex = new();
    
    private readonly Dictionary<string, Job> _jobs = new();

    public IQueryable<Job> Jobs
    {
        get
        {
            lock (Mutex)
                return _jobs.Values.AsQueryable();
        }
    }

    public void AddJob(Job job)
    {
        lock (Mutex)
        {
            _jobs[job.JobId] = job;
            
            // keep the list of jobs to a reasonable size, remove the oldest completed job
            while (_jobs.Values.Count(j => j.JobStatus == JobStatus.Completed) > 100)
            {
                Job last = _jobs.Values.Last(j => j.JobStatus == JobStatus.Completed);
                _jobs.Remove(last.JobId);
            }
        }
    }
    
    public bool TryTakeQueuedJob([MaybeNullWhen(false)]out Job job)
    {
        lock (Mutex)
        {
            job = _jobs.Values.FirstOrDefault(j => j.JobStatus == JobStatus.Queued);
            if (job is null)
                return false;
            
            job.JobStatus = JobStatus.Selected;
            return true;
        }
    }

    public void CancelQueuedJobs()
    {
        lock (Mutex)
        {
            foreach (var job in _jobs.Values.Where(j => j.JobStatus == JobStatus.Queued))
            {
                job.JobStatus = JobStatus.Completed;
                job.JobResult = JobResult.Cancelled;
            }
        }
    }

    public bool RequestCancellation(string jobId)
    {
        lock (Mutex)
        {
            Job? job = _jobs.GetValueOrDefault(jobId);
            if (job is null)
                return false;

            switch (job.JobStatus)
            {
                case JobStatus.Queued:
                    job.JobStatus = JobStatus.Completed;
                    job.JobResult = JobResult.Cancelled;
                    return true;

                case JobStatus.Selected:
                case JobStatus.Running:
                    if (job.JobStatus != JobStatus.Running || job.CancellationTokenSource is null)
                        return false;
                    job.CancellationTokenSource.Cancel();
                    return true;
                
                case JobStatus.Completed:
                    return false;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    public Job? FindJob(string jobId)
    {
        lock (Mutex)
            return _jobs.GetValueOrDefault(jobId);
    }
}