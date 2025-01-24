![Sockethead logo](sockethead-logo.png)

# Sockethead.Backgrounder
A simple background job runner for ASP.Net Core applications.

## Why Sockethead.Backgrounder?
When you want to run tasks in the background in your ASP.Net Core application, you have many options, but it often boils down to two options:
1. The built-in BackgroundService class that was introduced in ASP.Net Core 2.1.
2. Hangfire, a popular library for background tasks.

Hangfire is a great approach, but can be overkill for simple tasks. 
BackgroundService does the job for simple tasks, but is lacking in terms of visibility and control of your jobs.

Sockethead.Backgrounder fits in the middle. It runs in memory, is lightweight, and is quick and easy to set up and use. 
It provides visibility to monitor and control of your background jobs, but does not use persistent storage by default.

## Key Features
1. Enables immediate execution of background jobs.
3. Progress bar with realtime update (via SignalR) and percentage complete for each job.
4. Listing of Queued, Running, and Completed jobs.
5. Ability to cancel jobs.
6. Capture of logs for each job to a file that can be downloaded via the UI.
7. UI Features are installed into your app as an Area allowing you to control the look and feel and functionality.

## Getting Started

