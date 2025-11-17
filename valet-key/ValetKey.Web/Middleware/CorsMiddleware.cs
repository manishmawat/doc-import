using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ValetKey.Web.Middleware
{
    /// <summary>
    /// CORS middleware for Azure Functions that handles preflight requests and adds CORS headers
    /// </summary>
    public class CorsMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<CorsMiddleware> _logger;

        // Configure allowed origins for your client applications
        private readonly string[] _allowedOrigins = new[]
        {
            "http://localhost:5168",   // Your SPA client
            "http://localhost:3000",   // Common React dev server  
            "http://localhost:4200",   // Common Angular dev server
            "http://localhost:8080",   // Common Vue dev server
            "https://localhost:5168",  // HTTPS variants
            "https://localhost:3000",
            "https://localhost:4200", 
            "https://localhost:8080",
            "null"  // For local file:// protocol testing
            // Add your production domains here:
            // "https://yourapp.azurestaticapps.net",
            // "https://yourapp.com"
        };

        private readonly string[] _allowedMethods = new[]
        {
            "GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"
        };

        private readonly string[] _allowedHeaders = new[]
        {
            "Content-Type",
            "Authorization",
            "X-Requested-With",
            "Accept",
            "Origin",
            "Access-Control-Request-Method",
            "Access-Control-Request-Headers"
        };

        public CorsMiddleware(ILogger<CorsMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var httpRequestData = await context.GetHttpRequestDataAsync();
            
            if (httpRequestData == null)
            {
                // Not an HTTP function, skip CORS processing
                await next(context);
                return;
            }

            var origin = GetRequestOrigin(httpRequestData);
            var isAllowedOrigin = IsOriginAllowed(origin);

            _logger.LogDebug("CORS request from origin: {Origin}, Allowed: {IsAllowed}", origin, isAllowedOrigin);

            // Handle preflight requests (OPTIONS)
            if (string.Equals(httpRequestData.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Handling CORS preflight request from origin: {Origin}", origin);
                
                var response = httpRequestData.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(response, origin, isAllowedOrigin);
                
                // Set the response in context to bypass the actual function execution
                context.GetInvocationResult().Value = response;
                return;
            }

            // Process the actual request
            await next(context);

            // Add CORS headers to the response
            var httpResponseData = context.GetHttpResponseData();
            if (httpResponseData != null)
            {
                AddCorsHeaders(httpResponseData, origin, isAllowedOrigin);
            }
        }

        private string? GetRequestOrigin(HttpRequestData request)
        {
            if (request.Headers.TryGetValues("Origin", out var origins))
            {
                return origins.FirstOrDefault();
            }
            
            // Fallback to Referer header if Origin is not present
            if (request.Headers.TryGetValues("Referer", out var referers))
            {
                var referer = referers.FirstOrDefault();
                if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
                {
                    return $"{uri.Scheme}://{uri.Host}{(uri.Port != 80 && uri.Port != 443 ? $":{uri.Port}" : "")}";
                }
            }

            return null;
        }

        private bool IsOriginAllowed(string? origin)
        {
            if (string.IsNullOrEmpty(origin))
                return false;

            return _allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        }

        private void AddCorsHeaders(HttpResponseData response, string? origin, bool isAllowedOrigin)
        {
            if (isAllowedOrigin && !string.IsNullOrEmpty(origin))
            {
                // Add CORS headers
                response.Headers.Add("Access-Control-Allow-Origin", origin);
                response.Headers.Add("Access-Control-Allow-Credentials", "true");
                response.Headers.Add("Access-Control-Allow-Methods", string.Join(", ", _allowedMethods));
                response.Headers.Add("Access-Control-Allow-Headers", string.Join(", ", _allowedHeaders));
                response.Headers.Add("Access-Control-Max-Age", "86400"); // 24 hours
                
                // Additional headers for better browser compatibility
                response.Headers.Add("Vary", "Origin");
            }
            else if (!string.IsNullOrEmpty(origin))
            {
                _logger.LogWarning("CORS request from disallowed origin: {Origin}", origin);
                // Don't add CORS headers for disallowed origins
            }
        }
    }
}