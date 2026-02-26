namespace ForumWebsite.Models.Common
{
    /// <summary>
    /// Authentication failure (wrong credentials, missing token).
    /// Maps to HTTP 401 Unauthorized.
    /// Use this for login failures so clients can distinguish
    /// "not authenticated" (401) from "authenticated but not allowed" (403).
    /// </summary>
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
    }

    /// <summary>
    /// Authorisation failure — user is authenticated but lacks permission.
    /// Maps to HTTP 403 Forbidden.
    /// Replaces <see cref="UnauthorizedAccessException"/> to clarify intent
    /// (the .NET name is historically confusing).
    /// </summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }

    /// <summary>
    /// Business rule violation — semantically a 422 or 400.
    /// Maps to HTTP 400 Bad Request.
    /// Use instead of <see cref="InvalidOperationException"/> for domain-level
    /// rejections so infrastructure errors don't get masked as 400s.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}
