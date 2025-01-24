using System.ComponentModel.DataAnnotations;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.Utilities;

namespace Sockethead.Backgrounder.Models;

/// <summary>
/// How the IJob object is to be created 
/// </summary>
public enum JobCreateType
{
    /// <summary>
    /// Use Dependency Injection to create the JobObject in the background
    /// </summary>
    Inject,
    
    /// <summary>
    /// Create the JobObject instance via new()
    /// Note: this must have a parameterless constructor 
    /// </summary>
    New,
    
    /// <summary>
    /// Use the actual object passed in, kept in memory
    /// Note this will only work using the default JobInMemoryRepo on a single server
    /// Also keep in the process will execute in a separate thread and the current scope
    /// will be lost; things like DataContext created via Dependency Injection will not work.
    /// </summary>
    UseExisting,
}

public enum JobStatus
{
    Queued,
    Selected,
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
    public JobCreateType JobCreateType { get; set; } = JobCreateType.Inject;
    public string? InitialStateJson { get; set; }  
    
    [Display(AutoGenerateField = false)]
    public IJob? JobObject { get; set; }
    public DateTime CreateTime { get; } = DateTime.UtcNow;
    public DateTime? StartTime { get; internal set; }
    public DateTime? EndTime { get; internal set; }

    [Display(Name = "Elapsed")]
    public TimeSpan? ElapsedTime => EndTime - CreateTime;
    public JobStatus JobStatus { get; internal set; } = JobStatus.Queued;
    public JobResult JobResult { get; internal set; } = JobResult.Pending;
    
    [Display(AutoGenerateField = false)]
    public CancellationTokenSource? CancellationTokenSource { get; set; }
    
    [Display(AutoGenerateField = false)]
    public bool IsRunning => JobStatus is JobStatus.Running or JobStatus.Selected;
    
    private const int MaxDetailsPerJob = 100;
    private CircularBuffer<ProgressDetail> ProgressDetails { get; } = new(MaxDetailsPerJob);
    public void AddProgressDetail(ProgressDetail progressDetail) => ProgressDetails.Add(progressDetail);
    public IReadOnlyList<ProgressDetail> GetProgressDetails() => ProgressDetails;
}