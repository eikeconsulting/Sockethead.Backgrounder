using System.Diagnostics.CodeAnalysis;
using Sockethead.Backgrounder.Models;

namespace Sockethead.Backgrounder.Contracts;

public interface IJobRepo
{
    public IQueryable<Job> Jobs { get; }
    public void AddJob(Job job);
    public bool TryTakeQueuedJob([MaybeNullWhen(false)] out Job job);
    public void CancelQueuedJobs();
    public bool RequestCancellation(string jobId);
    public Job? FindJob(string jobId);
}