using ForumWebsite.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace ForumWebsite.Filters
{
    /// <summary>
    /// Declarative IP-based rate-limiting attribute backed by IMemoryCache.
    ///
    /// Usage:
    ///   [RateLimit(maxAttempts: 5, windowSeconds: 900)]  // 5 per 15 min per IP
    ///
    /// Fixed-window implementation
    /// ───────────────────────────
    /// The counter entry is created ONCE with an absolute TTL.  On subsequent
    /// requests within the window the cached object's Count is mutated IN PLACE —
    /// we never call cache.Set again, so the TTL is not reset.
    ///
    /// This matters because if we called cache.Set on every increment the window
    /// would slide: 4 requests/14 min would NEVER trigger the limit.
    ///
    /// Limitations (acceptable for Phase 1):
    ///   • In-memory only — resets on restart, not shared across instances.
    ///     Upgrade to IDistributedCache (Redis) for multi-node deployments.
    ///   • Count mutation is not Interlocked — under extreme concurrency a few
    ///     extra requests may slip through. For auth endpoints this is acceptable.
    ///   • IP spoofing is possible; combine with account-level lockout for defence-in-depth.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RateLimitAttribute : TypeFilterAttribute
    {
        public RateLimitAttribute(int maxAttempts = 5, int windowSeconds = 900)
            : base(typeof(RateLimitFilter))
        {
            Arguments = new object[] { maxAttempts, windowSeconds };
        }
    }

    public class RateLimitFilter : IActionFilter
    {
        private readonly IMemoryCache _cache;
        private readonly int          _maxAttempts;
        private readonly int          _windowSeconds;

        public RateLimitFilter(IMemoryCache cache, int maxAttempts, int windowSeconds)
        {
            _cache         = cache;
            _maxAttempts   = maxAttempts;
            _windowSeconds = windowSeconds;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var ip  = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"rl:{context.ActionDescriptor.DisplayName}:{ip}";

            // GetOrCreate is called on every request but only CREATES an entry on
            // the first call — subsequent calls return the SAME reference object.
            // By mutating Counter.Count on the returned reference we update the
            // cached value without calling Set, which would reset the TTL (sliding window bug).
            var counter = _cache.GetOrCreate(key, entry =>
            {
                // TTL is set ONLY on first creation — this is the fixed-window boundary
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_windowSeconds);
                return new RateLimitCounter();
            })!;

            counter.Count++;

            if (counter.Count > _maxAttempts)
            {
                context.HttpContext.Response.Headers["Retry-After"] = _windowSeconds.ToString();

                context.Result = new ObjectResult(
                    ApiResponse<object>.Fail(
                        $"Too many requests. Please try again in {_windowSeconds / 60} minute(s)."))
                {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        /// <summary>
        /// Mutable reference type used as the cache value.
        /// Because it is a class (not a struct), the cache holds a reference and
        /// mutations are visible to all subsequent cache lookups without re-calling Set.
        /// </summary>
        private sealed class RateLimitCounter
        {
            public int Count { get; set; }
        }
    }
}
