namespace Authentication.Library.Models
{
    /// <summary>
    /// Configuration options for Azure AD authentication
    /// </summary>
    public class AzureAdOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "AzureAd";

        /// <summary>
        /// Azure AD instance (e.g., https://login.microsoftonline.com/)
        /// </summary>
        public string Instance { get; set; } = "https://login.microsoftonline.com/";

        /// <summary>
        /// Azure AD tenant ID
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Application (client) ID
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client secret (for confidential clients)
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Audience for token validation
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Issuer for token validation
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether to validate the issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Indicates whether to validate the audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Indicates whether to validate the token lifetime
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Indicates whether to validate the issuer signing key
        /// </summary>
        public bool ValidateIssuerSigningKey { get; set; } = true;

        /// <summary>
        /// Clock skew tolerance for token validation
        /// </summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    }
}