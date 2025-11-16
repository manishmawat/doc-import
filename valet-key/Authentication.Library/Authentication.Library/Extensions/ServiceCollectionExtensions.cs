using Authentication.Library.Interfaces;
using Authentication.Library.Middleware;
using Authentication.Library.Models;
using Authentication.Library.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Authentication.Library.Extensions
{
    /// <summary>
    /// Extension methods for configuring authentication in Azure Functions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Azure AD authentication services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureAdAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure Azure AD options
            services.Configure<AzureAdOptions>(configuration.GetSection(AzureAdOptions.SectionName));

            // Register authentication service
            services.AddSingleton<IAuthenticationService, AzureAdAuthenticationService>();

            return services;
        }

        /// <summary>
        /// Adds Azure AD authentication services to the service collection with custom options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure Azure AD options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAzureAdAuthentication(
            this IServiceCollection services,
            Action<AzureAdOptions> configureOptions)
        {
            // Configure Azure AD options
            services.Configure(configureOptions);

            // Register authentication service
            services.AddSingleton<IAuthenticationService, AzureAdAuthenticationService>();

            return services;
        }
    }

    /// <summary>
    /// Extension methods for configuring authentication middleware in Azure Functions
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Configures Azure Functions with authentication middleware
        /// </summary>
        /// <param name="hostBuilder">The host builder</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The host builder for chaining</returns>
        public static IHostBuilder ConfigureFunctionsAuthentication(
            this IHostBuilder hostBuilder,
            IConfiguration? configuration = null)
        {
            return hostBuilder.ConfigureFunctionsWorkerDefaults(builder =>
            {
                // Add authentication middleware
                builder.UseMiddleware<AuthenticationMiddleware>();
            })
            .ConfigureServices((context, services) =>
            {
                var config = configuration ?? context.Configuration;
                services.AddAzureAdAuthentication(config);
            });
        }

        /// <summary>
        /// Configures Azure Functions with authentication middleware and custom options
        /// </summary>
        /// <param name="hostBuilder">The host builder</param>
        /// <param name="configureOptions">Action to configure Azure AD options</param>
        /// <returns>The host builder for chaining</returns>
        public static IHostBuilder ConfigureFunctionsAuthentication(
            this IHostBuilder hostBuilder,
            Action<AzureAdOptions> configureOptions)
        {
            return hostBuilder.ConfigureFunctionsWorkerDefaults(builder =>
            {
                // Add authentication middleware
                builder.UseMiddleware<AuthenticationMiddleware>();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddAzureAdAuthentication(configureOptions);
            });
        }
    }

    /// <summary>
    /// Extension methods for IFunctionsWorkerApplicationBuilder
    /// </summary>
    public static class FunctionsWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds authentication middleware to the Functions Worker application
        /// </summary>
        /// <param name="builder">The Functions Worker application builder</param>
        /// <returns>The builder for chaining</returns>
        public static IFunctionsWorkerApplicationBuilder UseAuthentication(
            this IFunctionsWorkerApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}