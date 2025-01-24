namespace Sockethead.Backgrounder.Models;

public class BackgrounderOptions
{
    /// <summary>
    /// The amount of time to wait between checking for new jobs when no jobs are available.
    /// (i.e. if multiple jobs are posted at once, they will all be pulled with no extra wait time)
    /// </summary>
    public int MillisecondsBetweenJobChecks { get; set; } = 1000; // 1 second
    
    /// <summary>
    /// The maximum number of jobs that can be run concurrently.
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 10;
    
}