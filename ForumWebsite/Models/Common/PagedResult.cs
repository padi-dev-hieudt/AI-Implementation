namespace ForumWebsite.Models.Common
{
    /// <summary>
    /// Generic wrapper returned by any paginated list endpoint.
    /// Carries both the data slice and the metadata a client needs to render pager controls.
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items      { get; set; } = Enumerable.Empty<T>();
        public int            Page       { get; set; }
        public int            PageSize   { get; set; }
        public int            TotalCount { get; set; }

        // Computed helpers — no storage in DB
        // Guard PageSize > 0: controller clamps it, but this type should be self-defending.
        public int  TotalPages      => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage     => Page < TotalPages;
    }
}
