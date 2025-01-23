using Microsoft.AspNetCore.SignalR;

namespace Sockethead.Backgrounder.Progress;

/// <summary>
/// This is a SignalR hub to push Background Job Progress Messages to the front end.
/// This is quite rudimentary at this time, but does the job quite well.
/// </summary>
public class JobProgressHub : Hub
{
    // TODO add methods for client-server communication
    public async Task Notify(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}
