using Serilog;
using Serilog.Events;
using Sockethead.Backgrounder;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.WebApp;
using Sockethead.Backgrounder.Logging;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .WriteTo.Sink(new JobLogEventSink())
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services
    .AddControllersWithViews()
    .AddSocketheadBackgrounderEmbeddedControllersAndViews();

builder.Services.AddRazorPages();

builder
    .Services.AddSignalR()
    .Services.RegisterBackgrounderInfrastructure(options =>
    {
        options.MaxConcurrentJobs = 4;
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapHub<JobProgressHub>("/progressHub"); // Map SignalR hub

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(name: "MyAreas", pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(name: "Default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

