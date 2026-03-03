namespace ForumWebsite.Services.Interfaces
{
    /// <summary>
    /// Determines whether a given page visit should be counted as a valid view.
    ///
    /// Design goals:
    ///   - Skip bots/crawlers detected via User-Agent
    ///   - Deduplicate repeated views from the same user or IP within a time window
    ///   - Thread-safe and allocation-light (IMemoryCache backing)
    /// </summary>
    public interface IViewCountService
    {
        /// <summary>
        /// Returns true if this visit qualifies as a countable view AND marks it
        /// as counted so that subsequent calls within the window return false.
        /// </summary>
        /// <param name="postId">The post being viewed.</param>
        /// <param name="viewerKey">
        ///   "u:{userId}" for authenticated users, "g:{remoteIp}" for guests.
        /// </param>
        /// <param name="userAgent">The raw User-Agent header value (may be null/empty).</param>
        bool ShouldCount(int postId, string viewerKey, string? userAgent);
    }
}
