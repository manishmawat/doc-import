using Authentication.Library.Models;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace Authentication.Library.Interfaces
{
    /// <summary>
    /// Interface for authentication services
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Validates authentication and extracts user information from HTTP request
        /// </summary>
        /// <param name="request">The HTTP request data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User information if authentication is successful</returns>
        /// <exception cref="Exceptions.AuthenticationException">Thrown when authentication fails</exception>
        Task<UserInformation> ValidateAndExtractUserAsync(HttpRequestData request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a JWT token and extracts user information
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User information if token is valid</returns>
        /// <exception cref="Exceptions.TokenValidationException">Thrown when token validation fails</exception>
        Task<UserInformation> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts user information from EasyAuth headers (Azure App Service authentication)
        /// </summary>
        /// <param name="request">The HTTP request data</param>
        /// <returns>User information if EasyAuth headers are present and valid</returns>
        UserInformation? ExtractUserFromEasyAuth(HttpRequestData request);
    }
}