namespace ForumWebsite.Models.DTOs.Post
{
    public class UpdatePostDto
    {
        public string    Title      { get; set; } = string.Empty;
        public string    Content    { get; set; } = string.Empty;

        /// <summary>
        /// Category to assign. 0 (or omitted) = keep the current category unchanged.
        /// </summary>
        public int       CategoryId { get; set; } = 0;

        /// <summary>
        /// Full replacement list of tag IDs. Empty list = remove all tags. Max 5.
        /// Null = keep current tags unchanged.
        /// </summary>
        public List<int>? TagIds    { get; set; } = null;
    }
}
