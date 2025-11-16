using Authentication.Library.Exceptions;
using Authentication.Library.Interfaces;
using Authentication.Library.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace Authentication.Library.Middleware
{
    /// <summary>
    /// Middleware for handling authentication in Azure Functions
    /// </summary>
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(
            IAuthenticationService authenticationService,
            ILogger<AuthenticationMiddleware> logger)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                // Check if the function requires authentication
                var functionDefinition = context.FunctionDefinition;
                var methodInfo = GetMethodInfo(functionDefinition);
                
                if (methodInfo == null)
                {
                    _logger.LogWarning("Could not determine method info for function: {FunctionName}", functionDefinition.Name);
                    await next(context);
                    return;
                }

                // Check for AllowAnonymous attribute
                if (HasAttribute<AllowAnonymousAttribute>(methodInfo))
                {
                    _logger.LogDebug("Function {FunctionName} allows anonymous access", functionDefinition.Name);
                    await next(context);
                    return;
                }

                // Check for RequireAuthentication attribute
                var authAttribute = GetAttribute<RequireAuthenticationAttribute>(methodInfo);
                if (authAttribute?.AllowAnonymous == true)
                {
                    _logger.LogDebug("Function {FunctionName} configured to allow anonymous access", functionDefinition.Name);
                    await next(context);
                    return;
                }

                // Get HTTP request data
                var httpRequestData = await context.GetHttpRequestDataAsync();
                if (httpRequestData == null)
                {
                    // Not an HTTP function, skip authentication
                    await next(context);
                    return;
                }

                // Perform authentication
                UserInformation userInfo;
                try
                {
                    userInfo = await _authenticationService.ValidateAndExtractUserAsync(httpRequestData, context.CancellationToken);
                    _logger.LogInformation("User authenticated successfully: {UserId}", userInfo.UserId);
                }
                catch (AuthenticationException ex)
                {
                    _logger.LogWarning(ex, "Authentication failed for function: {FunctionName}", functionDefinition.Name);
                    await SetUnauthorizedResponse(context, "Authentication required");
                    return;
                }

                // Check role-based authorization
                if (authAttribute?.RequiredRoles?.Length > 0)
                {
                    var userHasRequiredRole = authAttribute.RequiredRoles.Any(role => 
                        userInfo.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));

                    if (!userHasRequiredRole)
                    {
                        _logger.LogWarning("User {UserId} lacks required roles for function: {FunctionName}. Required: {RequiredRoles}, User roles: {UserRoles}", 
                            userInfo.UserId, functionDefinition.Name, string.Join(", ", authAttribute.RequiredRoles), string.Join(", ", userInfo.Roles));
                        await SetForbiddenResponse(context, "Insufficient privileges");
                        return;
                    }
                }

                // Store user information in context for use in the function
                context.Items["User"] = userInfo;

                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in authentication middleware");
                await SetInternalServerErrorResponse(context, "Authentication service error");
            }
        }

        private static MethodInfo? GetMethodInfo(FunctionDefinition functionDefinition)
        {
            try
            {
                // Extract the entry point information
                var entryPoint = functionDefinition.EntryPoint;
                if (string.IsNullOrEmpty(entryPoint))
                    return null;

                // Parse the entry point to get type and method names
                var parts = entryPoint.Split('.');
                if (parts.Length < 2)
                    return null;

                var methodName = parts.Last();
                var typeName = string.Join(".", parts.Take(parts.Length - 1));

                // Find the type in all loaded assemblies
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    // Try to find the type in all loaded assemblies
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(typeName);
                        if (type != null)
                            break;
                    }
                }

                if (type == null)
                    return null;

                // Get the method
                return type.GetMethod(methodName);
            }
            catch
            {
                return null;
            }
        }

        private static bool HasAttribute<T>(MethodInfo methodInfo) where T : Attribute
        {
            return methodInfo.GetCustomAttribute<T>() != null || 
                   methodInfo.DeclaringType?.GetCustomAttribute<T>() != null;
        }

        private static T? GetAttribute<T>(MethodInfo methodInfo) where T : Attribute
        {
            return methodInfo.GetCustomAttribute<T>() ?? 
                   methodInfo.DeclaringType?.GetCustomAttribute<T>();
        }

        private async Task SetUnauthorizedResponse(FunctionContext context, string message)
        {
            var response = context.GetHttpResponseData();
            if (response != null)
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.Headers.Add("Content-Type", "application/json");
                
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(new { error = message, statusCode = 401 });
                await response.Body.WriteAsync(jsonBytes);
            }
        }

        private async Task SetForbiddenResponse(FunctionContext context, string message)
        {
            var response = context.GetHttpResponseData();
            if (response != null)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                response.Headers.Add("Content-Type", "application/json");
                
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(new { error = message, statusCode = 403 });
                await response.Body.WriteAsync(jsonBytes);
            }
        }

        private async Task SetInternalServerErrorResponse(FunctionContext context, string message)
        {
            var response = context.GetHttpResponseData();
            if (response != null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Headers.Add("Content-Type", "application/json");
                
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(new { error = message, statusCode = 500 });
                await response.Body.WriteAsync(jsonBytes);
            }
        }
    }

    /// <summary>
    /// Extension methods for FunctionContext to work with authentication
    /// </summary>
    public static class FunctionContextExtensions
    {
        /// <summary>
        /// Gets the authenticated user information from the function context
        /// </summary>
        /// <param name="context">The function context</param>
        /// <returns>User information if available, null otherwise</returns>
        public static UserInformation? GetAuthenticatedUser(this FunctionContext context)
        {
            if (context.Items.TryGetValue("User", out var userObj) && userObj is UserInformation user)
            {
                return user;
            }
            return null;
        }

        /// <summary>
        /// Gets the authenticated user information from the function context, throwing an exception if not found
        /// </summary>
        /// <param name="context">The function context</param>
        /// <returns>User information</returns>
        /// <exception cref="AuthenticationException">Thrown when user information is not available</exception>
        public static UserInformation GetRequiredAuthenticatedUser(this FunctionContext context)
        {
            var user = GetAuthenticatedUser(context);
            if (user == null)
                throw new AuthenticationException("User information not available in context");
            return user;
        }
    }
}