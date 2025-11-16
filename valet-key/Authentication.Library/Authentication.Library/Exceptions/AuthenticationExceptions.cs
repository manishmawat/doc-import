namespace Authentication.Library.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication fails
    /// </summary>
    public class AuthenticationException : Exception
    {
        public AuthenticationException() : base("Authentication failed") { }

        public AuthenticationException(string message) : base(message) { }

        public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when authorization fails
    /// </summary>
    public class AuthorizationException : Exception
    {
        public AuthorizationException() : base("Authorization failed") { }

        public AuthorizationException(string message) : base(message) { }

        public AuthorizationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when token validation fails
    /// </summary>
    public class TokenValidationException : AuthenticationException
    {
        public TokenValidationException() : base("Token validation failed") { }

        public TokenValidationException(string message) : base(message) { }

        public TokenValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}