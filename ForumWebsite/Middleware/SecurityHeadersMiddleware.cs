namespace ForumWebsite.Middleware
{
    /// <summary>
    /// Injects security-related HTTP response headers on every response.
    ///
    /// Header reference
    /// ────────────────
    /// X-Content-Type-Options: nosniff
    ///   Prevents MIME-type sniffing — browsers must honour the declared Content-Type.
    ///
    /// X-Frame-Options: DENY
    ///   Prevents the site being embedded in an iframe — mitigates clickjacking.
    ///
    /// X-XSS-Protection: 0
    ///   Disabled in favour of a strong CSP. The legacy XSS filter is actually
    ///   dangerous on modern browsers and can introduce XSS vectors (per OWASP).
    ///
    /// Referrer-Policy: strict-origin-when-cross-origin
    ///   Sends full URL only on same-origin; only origin on cross-origin HTTPS.
    ///
    /// Content-Security-Policy
    ///   Phase 1 conservative policy — tighten per-feature as assets are added.
    ///
    /// Permissions-Policy
    ///   Disables browser APIs the forum does not need (camera, microphone, geolocation).
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"]        = "DENY";
            headers["X-XSS-Protection"]       = "0";   // disabled — rely on CSP instead
            headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";

            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                // cdn.jsdelivr.net: Quill.js rich-text editor
                "script-src 'self' https://cdn.jsdelivr.net; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                // Bootstrap Icons + Quill fonts are served from jsDelivr CDN.
                "font-src 'self' https://cdn.jsdelivr.net; " +
                // data: URIs needed for Quill clipboard-pasted images
                "img-src 'self' data: blob:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none';";

            headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=()";

            await _next(context);
        }
    }
}
