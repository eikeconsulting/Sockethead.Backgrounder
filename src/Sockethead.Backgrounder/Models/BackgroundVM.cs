using Sockethead.Backgrounder.Jobs;

namespace Sockethead.Backgrounder.Models;

public record ActiveJobsVM(IReadOnlyList<Job> ActiveJobs, Job? CurrentJob);

public record CompletedJobsVM(IReadOnlyList<Job> CompletedJobs);
