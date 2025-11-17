using System.Security.Claims;

namespace Authentication.Library.Models
{
    /// <summary>
    /// Represents user information extracted from authentication tokens
    /// </summary>
    public class UserInformation
    {
        /// <summary>
        /// Unique user identifier from Azure AD (oid claim)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User's preferred username
        /// </summary>
        public string PreferredUsername { get; set; } = string.Empty;

        /// <summary>
        /// Tenant ID from Azure AD
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Application ID that was used for authentication
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Roles assigned to the user
        /// </summary>
        public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// All claims associated with the user
        /// </summary>
        public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();

        /// <summary>
        /// Indicates if the user information was extracted successfully
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }
}