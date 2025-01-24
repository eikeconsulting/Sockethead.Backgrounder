using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sockethead.Backgrounder.Contracts;
using Sockethead.Backgrounder.Models;

namespace Sockethead.Backgrounder.JobManagement;

public class JobMgr(IJobRepo jobRepo)
{
    /// <summary>
    /// Enqueue a Job to be processed in the Background
    /// </summary>
    /// <param name="jobCreateType">How the object should be created</param>
    /// <param name="jobObject">The object to use or initial state</param>
    /// <param name="initialState">Initial state</param>
    /// <param name="jobName">Name of the job, will default to the IJob class name</param>
    /// <typeparam name="TJob">The IJob type</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Job EnqueueJob<TJob>(
        JobCreateType jobCreateType, 
        TJob? jobObject = null, 
        object? initialState = null, 
        string? jobName = null) 
        where TJob : class, IJob
    {
        Type type = typeof(TJob);
        
        switch (jobCreateType)
        {
            case JobCreateType.Inject:
                if (type.FullName == null)
                    throw new ArgumentNullException(nameof(jobObject), "Unable to resolve type to Inject.");
                initialState ??= jobObject;
                break;
            
            case JobCreateType.New:
                if (type.FullName == null)
                    throw new ArgumentException("Unable to resolve FullName of type to New (create).", nameof(jobObject));
                if (!HasParameterlessConstructor(type))
                    throw new ArgumentException("Type must have a parameterless constructor to New (create) it.", nameof(jobObject));
                initialState ??= jobObject;
                break;
            
            case JobCreateType.UseExisting:
                if (jobObject == null)
                    throw new ArgumentNullException(nameof(jobObject), "Job Object must be provided for an existing job.");
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(jobCreateType), jobCreateType, null);
        }
        
        Job job = new Job
        {
            JobName = jobName ?? type.Name,
            ClassFullName = type.FullName ?? string.Empty,
            InitialStateJson = initialState == null ? null : JsonConvert.SerializeObject(initialState),
            JobCreateType = jobCreateType,
        };

        jobRepo.AddJob(job);
        
        return job;
    }
    
    internal static IJob ResolveJobObject(Job job, IServiceProvider serviceProvider)
    {
        IJob? jobObject = job.JobCreateType switch
        {
            JobCreateType.Inject => serviceProvider.GetRequiredService(serviceType: GetTargetType(job.ClassFullName)) as IJob,
            JobCreateType.New => Activator.CreateInstance(type: GetTargetType(job.ClassFullName)) as IJob,
            JobCreateType.UseExisting => job.JobObject,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (jobObject is null)
            throw new InvalidOperationException($"Background Processor could not resolve Job Object for [{job.ClassFullName}].");
        
        if (job.InitialStateJson is not null)
            JsonConvert.PopulateObject(job.InitialStateJson, jobObject);

        return jobObject;
    }
    
    /// <summary>
    /// Get the Type of the actual Background Job that this Placeholder is for via Reflection
    /// </summary>
    private static Type GetTargetType(string classFullName)
    {
        // look through all the Assemblies in the project
        Type? targetType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(classFullName))
            .OfType<Type>()
            .FirstOrDefault();
        
        if (targetType == null)
            throw new InvalidOperationException($"Background Processor could not find type [{classFullName}].");
        
        return targetType;
    }    
    
    private static bool HasParameterlessConstructor(Type type)
    {
        return type
            // Get all public instance constructors
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            // Check if any constructor has no parameters
            .Any(ctor => ctor.GetParameters().Length == 0);
    }
}