using Microsoft.Azure.Functions.Worker;

namespace Authentication.Library.Middleware
{
    /// <summary>
    /// Attribute to mark functions that require authentication
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireAuthenticationAttribute : Attribute
    {
        /// <summary>
        /// Required roles for accessing the function
        /// </summary>
        public string[] RequiredRoles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Whether to allow anonymous access (bypass authentication)
        /// </summary>
        public bool AllowAnonymous { get; set; } = false;

        /// <summary>
        /// Custom authentication policy name
        /// </summary>
        public string? Policy { get; set; }

        public RequireAuthenticationAttribute() { }

        public RequireAuthenticationAttribute(params string[] requiredRoles)
        {
            RequiredRoles = requiredRoles ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Attribute to allow anonymous access (bypass authentication)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AllowAnonymousAttribute : Attribute
    {
    }
}