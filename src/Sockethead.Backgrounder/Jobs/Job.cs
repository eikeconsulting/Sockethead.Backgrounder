using System.ComponentModel.DataAnnotations;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Utilities;

namespace Sockethead.Backgrounder.Jobs;

public delegate Task ProgressCallback(double progress, string message);

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

public abstract class Job
{
    public string JobId { get; internal set; } = Guid.NewGuid().ToString();
    public abstract string JobName { get; }
    public virtual string ClassFullName => GetType().FullName!;
    
    public DateTime CreateTime { get; } = DateTime.UtcNow;
    public DateTime? StartTime { get; internal set; }
    public DateTime? EndTime { get; internal set; }

    [Display(Name = "Elapsed")]
    public TimeSpan? ElapsedTime => EndTime - CreateTime;
    
    public JobStatus JobStatus { get; internal set; } = JobStatus.Queued;
    public JobResult JobResult { get; internal set; } = JobResult.Pending;
    
    internal IServiceProvider ServiceProvider { get; set; } = null!;
    
    public abstract Task ExecuteAsync(ProgressCallback callback, CancellationToken token);
    
    private const int MaxDetailsPerJob = 100;

    private CircularBuffer<ProgressDetail> ProgressDetails { get; } = new(MaxDetailsPerJob);

    public void AddProgressDetail(ProgressDetail progressDetail) => ProgressDetails.Add(progressDetail);
    
    public IReadOnlyList<ProgressDetail> GetProgressDetails() => ProgressDetails;
}