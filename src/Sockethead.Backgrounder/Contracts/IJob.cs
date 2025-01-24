namespace Sockethead.Backgrounder.Contracts;

public delegate Task ProgressCallback(double progress, string message);

public interface IJob
{
    public Task ExecuteAsync(ProgressCallback callback, CancellationToken token);
}