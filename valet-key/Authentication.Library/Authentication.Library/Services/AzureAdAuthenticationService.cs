using Authentication.Library.Exceptions;
using Authentication.Library.Interfaces;
using Authentication.Library.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HttpRequestData = Microsoft.Azure.Functions.Worker.Http.HttpRequestData;

namespace Authentication.Library.Services
{
    /// <summary>
    /// Service for handling Azure AD/Entra ID authentication in Azure Functions
    /// </summary>
    public class AzureAdAuthenticationService : IAuthenticationService
    {
        private readonly AzureAdOptions _azureAdOptions;
        private readonly ILogger<AzureAdAuthenticationService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

        public AzureAdAuthenticationService(
            IOptions<AzureAdOptions> azureAdOptions,
            ILogger<AzureAdAuthenticationService> logger)
        {
            _azureAdOptions = azureAdOptions.Value ?? throw new ArgumentNullException(nameof(azureAdOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenHandler = new JwtSecurityTokenHandler();

            // Create configuration manager for OpenID Connect metadata
            string metadataAddress;
            if (_azureAdOptions.Instance.Contains("ciamlogin.com") || _azureAdOptions.Instance.Contains("b2clogin.com"))
            {
                // B2C/CIAM configuration - uses v2.0 endpoint
                metadataAddress = $"{_azureAdOptions.Instance.TrimEnd('/')}/{_azureAdOptions.TenantId}/v2.0/.well-known/openid-configuration";
            }
            else
            {
                // Standard Azure AD configuration
                metadataAddress = $"{_azureAdOptions.Instance.TrimEnd('/')}/{_azureAdOptions.TenantId}/v2.0/.well-known/openid_configuration";
            }
            
            _logger.LogInformation("Using OpenID Connect metadata URL: {MetadataAddress}", metadataAddress);
            
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
        }

        /// <summary>
        /// Validates authentication and extracts user information from HTTP request
        /// </summary>
        public async Task<UserInformation> ValidateAndExtractUserAsync(HttpRequestData request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                // First try EasyAuth (Azure App Service authentication)
                var easyAuthUser = ExtractUserFromEasyAuth(request);
                if (easyAuthUser != null && easyAuthUser.IsAuthenticated)
                {
                    _logger.LogDebug("User authenticated via EasyAuth: {UserId}", easyAuthUser.UserId);
                    return easyAuthUser;
                }

                // Then try JWT token validation
                if (request.Headers.TryGetValues("Authorization", out var authHeaders))
                {
                    var authHeader = authHeaders.FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var token = authHeader["Bearer ".Length..];
                        var jwtUser = await ValidateTokenAsync(token, cancellationToken);
                        _logger.LogDebug("User authenticated via JWT: {UserId}", jwtUser.UserId);
                        return jwtUser;
                    }
                }

                _logger.LogWarning("No valid authentication found in request");
                throw new AuthenticationException("No valid authentication found");
            }
            catch (Exception ex) when (!(ex is AuthenticationException))
            {
                _logger.LogError(ex, "Unexpected error during authentication");
                throw new AuthenticationException("Authentication failed due to unexpected error", ex);
            }
        }

        /// <summary>
        /// Validates a JWT token and extracts user information
        /// </summary>
        public async Task<UserInformation> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            try
            {
                // Get OpenID Connect configuration
                var config = await _configurationManager.GetConfigurationAsync(cancellationToken);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = _azureAdOptions.ValidateIssuer,
                    ValidIssuers = string.IsNullOrEmpty(_azureAdOptions.Issuer) 
                        ? (_azureAdOptions.Instance.Contains("ciamlogin.com") || _azureAdOptions.Instance.Contains("b2clogin.com")
                            ? new List<string>
                            {
                                // B2C/CIAM can use different issuer formats
                                $"{_azureAdOptions.Instance.TrimEnd('/')}/{_azureAdOptions.TenantId}/",
                                $"{_azureAdOptions.Instance.TrimEnd('/')}/{_azureAdOptions.TenantId}/v2.0",
                                $"https://{_azureAdOptions.TenantId}.ciamlogin.com/{_azureAdOptions.TenantId}/v2.0"
                            }
                            : new List<string> { $"{_azureAdOptions.Instance.TrimEnd('/')}/{_azureAdOptions.TenantId}/v2.0" })
                        : new List<string> { _azureAdOptions.Issuer },
                    
                    ValidateAudience = _azureAdOptions.ValidateAudience,
                    ValidAudiences = new List<string> 
                    { 
                        _azureAdOptions.ClientId, // B2C tokens often use client ID as audience
                        string.IsNullOrEmpty(_azureAdOptions.Audience) 
                            ? _azureAdOptions.ClientId 
                            : _azureAdOptions.Audience // API URI format
                    }.Distinct(),
                    
                    ValidateLifetime = _azureAdOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = _azureAdOptions.ValidateIssuerSigningKey,
                    IssuerSigningKeys = config.SigningKeys,
                    ClockSkew = _azureAdOptions.ClockSkew
                };

                var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null)
                    throw new TokenValidationException("Invalid token format");

                return ExtractUserInformationFromClaims(principal.Claims);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                throw new TokenValidationException("Token validation failed", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token validation");
                throw new TokenValidationException("Token validation failed due to unexpected error", ex);
            }
        }

        /// <summary>
        /// Extracts user information from EasyAuth headers
        /// </summary>
        public UserInformation? ExtractUserFromEasyAuth(HttpRequestData request)
        {
            if (request == null)
                return null;

            try
            {
                // Check for EasyAuth headers
                if (!request.Headers.Contains("X-MS-CLIENT-PRINCIPAL-ID"))
                    return null;

                var userId = request.Headers.GetValues("X-MS-CLIENT-PRINCIPAL-ID").FirstOrDefault();
                if (string.IsNullOrEmpty(userId))
                    return null;

                var userEmail = request.Headers.GetValues("X-MS-CLIENT-PRINCIPAL-NAME").FirstOrDefault();
                var userName = request.Headers.GetValues("X-MS-CLIENT-PRINCIPAL").FirstOrDefault();

                // Parse additional claims if available
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId),
                    new("oid", userId)
                };

                if (!string.IsNullOrEmpty(userEmail))
                {
                    claims.Add(new Claim(ClaimTypes.Email, userEmail));
                    claims.Add(new Claim("preferred_username", userEmail));
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, userName));
                }

                return new UserInformation
                {
                    UserId = userId,
                    Email = userEmail ?? string.Empty,
                    Name = userName ?? string.Empty,
                    PreferredUsername = userEmail ?? string.Empty,
                    Claims = claims
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract user from EasyAuth headers");
                return null;
            }
        }

        private UserInformation ExtractUserInformationFromClaims(IEnumerable<Claim> claims)
        {
            var claimsList = claims.ToList();

            // Extract standard claims
            var userId = GetClaimValue(claimsList, "oid", "sub", ClaimTypes.NameIdentifier);
            var email = GetClaimValue(claimsList, "email", "preferred_username", ClaimTypes.Email);
            var name = GetClaimValue(claimsList, "name", ClaimTypes.Name);
            var preferredUsername = GetClaimValue(claimsList, "preferred_username", "upn", ClaimTypes.Upn);
            var tenantId = GetClaimValue(claimsList, "tid");
            var appId = GetClaimValue(claimsList, "aud", "appid");

            // Extract roles
            var roles = claimsList
                .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (string.IsNullOrEmpty(userId))
                throw new TokenValidationException("Token does not contain a valid user identifier");

            return new UserInformation
            {
                UserId = userId,
                Email = email ?? string.Empty,
                Name = name ?? string.Empty,
                PreferredUsername = preferredUsername ?? email ?? string.Empty,
                TenantId = tenantId ?? string.Empty,
                AppId = appId ?? string.Empty,
                Roles = roles,
                Claims = claimsList
            };
        }

        private static string? GetClaimValue(IEnumerable<Claim> claims, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var claim = claims.FirstOrDefault(c => 
                    string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase));
                if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    return claim.Value;
            }
            return null;
        }
    }
}