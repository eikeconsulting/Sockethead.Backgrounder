using Microsoft.AspNetCore.SignalR;
using Sockethead.Backgrounder.Models;

namespace Sockethead.Backgrounder.Progress;

// ReSharper disable once NotAccessedPositionalProperty.Global
public record ProgressDetail(string JobId, string JobName, double Progress, string Message)
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class JobProgressMgr(IHubContext<JobProgressHub> hubContext)
{
    public async Task SendProgressAsync(Job job, double progress, string message, CancellationToken token)
    {
        ProgressDetail progressDetail = new(
            JobId: job.JobId,
            JobName: job.JobName,
            Progress: progress,
            Message: message);

        job.AddProgressDetail(progressDetail);
        await hubContext.Clients.All.SendAsync("ReceiveProgress", progressDetail, cancellationToken: token);
    }
}