using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Sockethead.Backgrounder.WebApp;

public static class LibraryHelper
{
    public static IMvcBuilder AddSocketheadBackgrounderEmbeddedControllersAndViews(this IMvcBuilder mvcBuilder)
    {
        Assembly libraryAssembly = Assembly.Load("Sockethead.Backgrounder"); 
        Console.WriteLine($"Assembly Loaded: {libraryAssembly.FullName}");
            
        mvcBuilder
            .AddApplicationPart(libraryAssembly)
            .AddRazorRuntimeCompilation(options => 
                options.FileProviders.Add(new EmbeddedFileProvider(libraryAssembly, "Sockethead.Backgrounder")));

        var resources = libraryAssembly.GetManifestResourceNames();
        foreach (var resource in resources)
            Console.WriteLine($"Resource: {resource}");
        
        return mvcBuilder;
    }
}