namespace ForumWebsite.Models.DTOs.Post
{
    public class CreatePostDto
    {
        public string    Title      { get; set; } = string.Empty;
        public string    Content    { get; set; } = string.Empty;

        /// <summary>
        /// Category to assign. 0 (or omitted) = use the default "Uncategorized" category.
        /// </summary>
        public int       CategoryId { get; set; } = 0;

        /// <summary>
        /// IDs of existing tags to apply. Empty list = no tags. Max 5 tags per post.
        /// </summary>
        public List<int> TagIds     { get; set; } = new();
    }
}
