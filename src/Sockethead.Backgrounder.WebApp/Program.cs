using Sockethead.Backgrounder;
using Sockethead.Backgrounder.Progress;
using Sockethead.Backgrounder.WebApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllersWithViews()
    .AddSocketheadBackgrounderEmbeddedControllersAndViews();

builder.Services.AddRazorPages();

    // TODO: logging support for BackgroundJobs
    //.Enrich.FromLogContext()
    //.WriteTo.Sink(new JobLogEventSink())

builder
    .Services.AddSignalR()
    .Services.RegisterBackgrounderInfrastructure();

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

