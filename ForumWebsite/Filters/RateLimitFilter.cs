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
    /// requests within the window the cached object's counter is mutated IN PLACE
    /// via Interlocked.Increment — we never call cache.Set again, so the TTL is
    /// not reset (avoids the sliding-window bug).
    ///
    /// Limitations (acceptable for Phase 1):
    ///   • In-memory only — resets on restart, not shared across instances.
    ///     Upgrade to IDistributedCache (Redis) for multi-node deployments.
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

            // GetOrCreate returns the SAME reference object on subsequent calls.
            // By using Interlocked.Increment on the returned reference we update the
            // cached value atomically without calling Set (which would reset the TTL).
            var counter = _cache.GetOrCreate(key, entry =>
            {
                // TTL is set ONLY on first creation — this is the fixed-window boundary.
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_windowSeconds);
                return new RateLimitCounter();
            })!;

            var current = counter.Increment();

            if (current > _maxAttempts)
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
        /// Interlocked.Increment makes increments atomic — safe under concurrent requests.
        /// </summary>
        private sealed class RateLimitCounter
        {
            private int _count;

            /// <summary>Atomically increments the counter and returns the new value.</summary>
            public int Increment() => Interlocked.Increment(ref _count);

            public int Count => Volatile.Read(ref _count);
        }
    }
}
