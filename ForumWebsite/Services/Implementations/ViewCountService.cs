using ForumWebsite.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ForumWebsite.Services.Implementations
{
    /// <summary>
    /// Discourse-style view counting guard.
    ///
    /// Rules (ALL must pass for ShouldCount to return true):
    ///   1. Non-empty User-Agent with no known bot/crawler substring.
    ///   2. No prior counted view from the same (postId, viewerKey) within the time window.
    ///
    /// Registered as Singleton — the IMemoryCache entries must survive across HTTP requests.
    ///
    /// Known Phase 1 limitation:
    ///   GetOrCreate can invoke the factory more than once under very high concurrent load
    ///   (documented .NET behavior). The result is a rare, minor over-count on a post's
    ///   very first view. Acceptable for view counting; upgrade to Redis + Lua CAS in Phase 2.
    /// </summary>
    public class ViewCountService : IViewCountService
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan     _window;

        // Lower-case substrings whose presence in a User-Agent signals a non-browser client.
        // Covers major search crawlers, HTTP libraries, and headless/automation browsers.
        // C# 10 / .NET 6 compatible array initializer (NOT C# 12 collection expression).
        private static readonly string[] _botSignals =
        {
            "bot", "crawl", "spider", "slurp", "fetch", "scan",
            "python", "curl", "wget", "axios", "java/", "go-http",
            "headless", "phantom", "selenium", "puppeteer", "playwright"
        };

        public ViewCountService(IMemoryCache cache, IConfiguration configuration)
        {
            _cache = cache;

            var minutes = configuration.GetValue<int>("ViewCount:WindowMinutes", 10);
            // Clamp to a sensible range: 1–60 min
            _window = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 60));
        }

        /// <inheritdoc/>
        public bool ShouldCount(int postId, string viewerKey, string? userAgent)
        {
            // ── 1. Bot / headless / empty UA check ────────────────────────────────
            if (IsBot(userAgent)) return false;

            // ── 2. Deduplication within the absolute window ────────────────────────
            // Use GetOrCreate (idiomatic IMemoryCache pattern) which has better internal
            // coordination than the previous TryGetValue + Set two-step.
            // The factory signals "this is the first view" via a closure-captured bool.
            // Cache key: "vc:{postId}:{viewerKey}" — unique per viewer per post.
            var cacheKey = $"vc:{postId}:{viewerKey}";
            bool isFirstView = false;

            _cache.GetOrCreate(cacheKey, entry =>
            {
                // Absolute expiry — no sliding reset on repeated access within the window.
                entry.AbsoluteExpirationRelativeToNow = _window;
                // Low priority: evict these first under memory pressure over business data.
                entry.Priority = CacheItemPriority.Low;
                isFirstView = true;
                return true;    // stored value (not used beyond existence check)
            });

            return isFirstView;
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private static bool IsBot(string? ua)
        {
            // Missing or whitespace-only UA → treat as bot / background prefetch
            if (string.IsNullOrWhiteSpace(ua)) return true;

            var lower = ua.ToLowerInvariant();
            foreach (var signal in _botSignals)
                if (lower.Contains(signal, StringComparison.Ordinal)) return true;

            return false;
        }
    }
}
