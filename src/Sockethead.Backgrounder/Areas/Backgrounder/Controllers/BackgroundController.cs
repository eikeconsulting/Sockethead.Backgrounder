using Microsoft.AspNetCore.Mvc;
using Sockethead.Backgrounder.Background;
using Sockethead.Backgrounder.Jobs;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Models;
using Sockethead.Backgrounder.Jobs.Samples;
using Sockethead.Razor.Alert.Extensions;

namespace Sockethead.Areas.Backgrounder.Controllers;

[Area("Backgrounder")]
public class BackgroundController(
    JobRunner jobRunner, 
    JobQueueMgr jobQueueMgr, 
    JobCompletedMgr jobCompletedMgr) : Controller
{
    public IActionResult SayHello() => Content("Hello from TestController!");

    public IActionResult MyView() => View();

    [HttpGet]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ActiveJobs()
    {
        return View(new ActiveJobsVM(
            ActiveJobs: jobQueueMgr.GetQueuedJobs(), 
            CurrentJob: jobRunner.CurrentJob));
    }

    [HttpGet]
    public IActionResult CompletedJobs()
    {
        return View(new CompletedJobsVM(CompletedJobs: jobCompletedMgr.GetCompletedJobs()));
    }
    
    [HttpGet]
    public IActionResult JobDetails(string jobId)
    {
        Job? details = FindJob(jobId);
        if (details is null)
            return NotFound("Job not found");
        
        return View(model: details);
    }
    
    private Job? FindJob(string jobId)
    {
        Job? result = jobCompletedMgr.FindJob(jobId);
        if (result is not null)
            return result;

        Job? job = jobRunner.CurrentJob; 
        if (job is not null && job.JobId == jobId)
            return job;

        return jobQueueMgr.FindJob(jobId);
    }
    
    
    [HttpGet]
    public IActionResult StartTestSuccessJob()
    {
        Job job = jobQueueMgr.EnqueueJob<TestSuccessJobNewable>(JobCreateType.New);
        return RedirectToAction(nameof(JobDetails), new { job.JobId })
            .Success($"Job started (New)!");
    }

    [HttpGet]
    public IActionResult StartTestSuccessJobDI()
    {
        Job job = jobQueueMgr.EnqueueJob<TestSuccessJobInjectable>(JobCreateType.Inject,
            initialState: new 
            { 
                Start = 25, 
                End = 50, 
            });
        
        return RedirectToAction(nameof(JobDetails), new { job.JobId })
            .Success($"Job started: TestSuccessJob (Inject): {job.JobId}");
    }

    
    [HttpGet]
    public IActionResult StartTestErrorJob()
    {
        Job job = jobQueueMgr.EnqueueJob<TestErrorJobNewable>(JobCreateType.New);
        return RedirectToAction(nameof(JobDetails), new { job.JobId })
            .Success($"Job started!");
    }

    [HttpGet]
    public IActionResult ClearQueuedJobs()
    {
        jobQueueMgr.ClearQueuedJobs();
        return RedirectToAction(nameof(ActiveJobs))
            .Success($"Successfully cleared all enqueued jobs.");
    }

    [HttpGet]
    public IActionResult CancelCurrentJob()
    {
        jobRunner.CancelCurrentJob();
        return RedirectToAction(nameof(ActiveJobs))
            .Success($"Successfully cancelled current job (if any was running).");
    }
    
    [HttpGet]
    public IActionResult LogFile(string jobId)
    {
        // Path to the local file
        string filePath = JobLogEventSink.GetLogFileName(jobId); 
            
        // Check if the file exists
        if (!System.IO.File.Exists(filePath))
            return NotFound("File not found.");

        // Return the file as a download
        return File(
            fileContents: System.IO.File.ReadAllBytes(filePath), 
            contentType: "text/plain",  
            fileDownloadName: $"BackgroundJob-{jobId}.log");
    }
}