using System.Collections.Concurrent;
using Sockethead.Backgrounder.Jobs;
using Sockethead.Backgrounder.Utilities;

namespace Sockethead.Backgrounder.Background;

public class JobMgr
{
    public BlockingCollection<Job> JobsQueued { get; } = new();
    public BlockingCollection<Job> JobsRunning { get; } = new(100);
    public BlockingCircularBuffer<Job> JobsCompleted { get; } = new(100);
    
    
}