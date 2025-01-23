using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Sockethead.Backgrounder.Logging;

/// <summary>
/// A custom Log Sink so that we can isolate those log files from a specific Background Job
/// and also so the file gets closed when the Job completes and can be processed and deleted if necessary.
/// This means it is not as efficient as the standard file sync which keeps the file open, but
/// this overhead should be fine for background processes which are not responding directly to an
/// end user action.
/// </summary>
public class JobLogEventSink : ILogEventSink
{
    public static string GetLogFileName(string jobId) => $"Logs/BackgroundJob-{jobId}.txt";
    public static string ContextName => "BackgroundContext";

    private readonly CompactJsonFormatter _jsonFormatter = new();

    public JobLogEventSink()
    {
        Directory.CreateDirectory("Logs");
    }
    
    public void Emit(LogEvent logEvent)
    {
        // The ContextName gets set immediately before the Job is run, so this is how we filter
        // the log events that "belong" to the Background Job 
        if (!logEvent.Properties.TryGetValue(ContextName, out LogEventPropertyValue? prop))
            return;

        string jobId = prop.ToString().Trim('"');
        string fileName = GetLogFileName(jobId);
        using var writer = File.AppendText(fileName);
        _jsonFormatter.Format(logEvent, writer);
    }
}