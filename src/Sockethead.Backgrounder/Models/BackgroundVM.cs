namespace Sockethead.Backgrounder.Models;

public record ActiveJobsVM(IQueryable<Job> QueuedJobs, IQueryable<Job> RunningJobs);

public record CompletedJobsVM(IQueryable<Job> CompletedJobs);
