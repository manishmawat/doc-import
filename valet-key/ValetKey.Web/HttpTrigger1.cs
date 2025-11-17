using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Authentication.Library.Middleware;

namespace ValetKey.Web;

public class HttpTrigger1
{
    private readonly ILogger<HttpTrigger1> _logger;

    public HttpTrigger1(ILogger<HttpTrigger1> logger)
    {
        _logger = logger;
    }

    [Function("HttpTrigger1")]
    [AllowAnonymous] // This endpoint allows anonymous access
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync("Welcome to Azure Functions!");
        return response;
    }

    [Function("ProtectedEndpoint")]
    [RequireAuthentication] // This endpoint requires authentication
    public async Task<HttpResponseData> ProtectedEndpoint(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Protected endpoint called.");
        
        // Get the authenticated user
        var user = executionContext.GetAuthenticatedUser();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new 
        { 
            message = "Hello authenticated user!",
            userId = user?.UserId,
            email = user?.Email
        });
        return response;
    }

    [Function("AdminOnlyEndpoint")]
    [RequireAuthentication("admin")] // This endpoint requires authentication and admin role
    public async Task<HttpResponseData> AdminOnlyEndpoint(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Admin endpoint called.");
        
        var user = executionContext.GetRequiredAuthenticatedUser();
        
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new 
        { 
            message = "Hello admin user!",
            userId = user.UserId,
            email = user.Email,
            roles = user.Roles
        });
        return response;
    }
}