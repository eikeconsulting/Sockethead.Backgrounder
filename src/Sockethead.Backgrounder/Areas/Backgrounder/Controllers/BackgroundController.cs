using Microsoft.AspNetCore.Mvc;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Jobs;
using Sockethead.Backgrounder.Logging;
using Sockethead.Backgrounder.Models;
using Sockethead.Backgrounder.JobManagement;
using Sockethead.Razor.Alert.Extensions;

namespace Sockethead.Areas.Backgrounder.Controllers;

[Area("Backgrounder")]
public class BackgroundController(IJobRepo jobRepo, JobMgr jobMgr) : Controller
{
    public IActionResult SayHello() => Content("Hello from TestController!");

    [HttpGet]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ActiveJobs()
    {
        return View(new ActiveJobsVM(
            QueuedJobs: jobRepo.Jobs.Where(j => j.JobStatus == JobStatus.Queued), 
            RunningJobs: jobRepo.Jobs.Where(j => j.IsRunning)));
    }

    [HttpGet]
    public IActionResult CompletedJobs()
    {
        return View(new CompletedJobsVM(CompletedJobs: jobRepo.Jobs.Where(j => j.JobStatus == JobStatus.Completed)));
    }
    
    [HttpGet]
    public IActionResult JobDetails(string jobId)
    {
        Job? details = jobRepo.FindJob(jobId);
        if (details is null)
            return NotFound("Job not found");
        
        return View(model: details);
    }
    
    [HttpPost, HttpGet]
    public IActionResult CancelJob(string jobId)
    {
        bool success = jobRepo.RequestCancellation(jobId);
        return RedirectToAction(nameof(JobDetails), new { jobId })
            .Success($"Job {jobId} cancelled success: {success}");
    }
    
    [HttpGet]
    public IActionResult StartTestSuccessJob()
    {
        Job job = jobMgr.EnqueueJob<TestSuccessJobNewable>(JobCreateType.New);
        return RedirectToAction(nameof(JobDetails), new { job.JobId })
            .Success($"Job started (New)!");
    }

    [HttpGet]
    public IActionResult StartTestSuccessJobDI()
    {
        Job job = jobMgr.EnqueueJob<TestSuccessJobInjectable>(JobCreateType.Inject,
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
        Job job = jobMgr.EnqueueJob<TestErrorJobNewable>(JobCreateType.New);
        return RedirectToAction(nameof(JobDetails), new { job.JobId })
            .Success($"Job started!");
    }

    [HttpGet]
    public IActionResult ClearQueuedJobs()
    {
        jobRepo.CancelQueuedJobs();
        return RedirectToAction(nameof(ActiveJobs))
            .Success($"Successfully cleared all enqueued jobs.");
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