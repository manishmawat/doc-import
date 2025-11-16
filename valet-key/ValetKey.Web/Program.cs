using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Authentication.Library.Extensions;
using ValetKey.Web.Middleware;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(appBuilder =>
    {
        // Configure blob storage extension
        appBuilder.ConfigureBlobStorageExtension();
        
        // Add CORS middleware FIRST (important for preflight requests)
        appBuilder.UseMiddleware<CorsMiddleware>();
        
        // Add authentication middleware AFTER CORS
        appBuilder.UseAuthentication();
    })
    .ConfigureServices((context, services) =>
    {
        // Add Azure AD authentication
        services.AddAzureAdAuthentication(context.Configuration);
    })
    .Build();

await host.RunAsync();