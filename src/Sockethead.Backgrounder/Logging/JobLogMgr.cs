using Serilog.Context;
// ReSharper disable MemberCanBeMadeStatic.Global

namespace Sockethead.Backgrounder.Logging;

public class JobLogMgr
{
    public IDisposable PrepareJobLogging(string jobId)
    {
        return LogContext.PushProperty(JobLogEventSink.ContextName, jobId);
    }

    public void CloseJobLogging(string jobId)
    {
        string logfile = JobLogEventSink.GetLogFileName(jobId);
        if (File.Exists(logfile))
        {
            // TODO: upload to S3 and delete
        }
    }
}