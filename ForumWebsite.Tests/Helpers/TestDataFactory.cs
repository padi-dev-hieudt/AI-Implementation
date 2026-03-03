using ForumWebsite.Models.Entities;

namespace ForumWebsite.Tests.Helpers;

/// <summary>
/// Central factory for creating entity instances in tests.
/// Uses workFactor:4 for BCrypt to keep unit tests fast.
/// </summary>
public static class TestDataFactory
{
    private static int _seq;

    private static int Next() => Interlocked.Increment(ref _seq);

    // ── Users ─────────────────────────────────────────────────────────────────

    public static User CreateUser(
        int?    id           = null,
        string? username     = null,
        string? email        = null,
        string? passwordHash = null,
        string? role         = null,
        bool    isActive     = true)
    {
        var n = Next();
        return new User
        {
            Id           = id ?? n,
            Username     = username ?? $"user{n}",
            Email        = email    ?? $"user{n}@example.com",
            PasswordHash = passwordHash
                           ?? BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 4),
            Role         = role      ?? UserRoles.User,
            IsActive     = isActive,
            CreatedAt    = DateTime.UtcNow
        };
    }

    public static User CreateAdmin(int? id = null, string? username = null)
        => CreateUser(id: id, username: username ?? "admin", role: UserRoles.Admin);

    // ── Posts ─────────────────────────────────────────────────────────────────

    public static Post CreatePost(
        int?    id        = null,
        User?   user      = null,
        int?    userId    = null,
        string? title     = null,
        string? content   = null,
        bool    isDeleted = false,
        int     viewCount = 0)
    {
        var n = Next();
        var post = new Post
        {
            Id        = id      ?? n,
            Title     = title   ?? $"Test Post Title {n}",
            Content   = content ?? $"Test post content body for post {n}",
            UserId    = userId  ?? user?.Id ?? n,
            IsDeleted = isDeleted,
            ViewCount = viewCount,
            CreatedAt = DateTime.UtcNow,
            Comments  = new List<Comment>()
        };

        if (user != null)
        {
            post.User   = user;
            post.UserId = user.Id;
        }
        return post;
    }

    // ── Comments ──────────────────────────────────────────────────────────────

    public static Comment CreateComment(
        int?    id        = null,
        Post?   post      = null,
        User?   user      = null,
        int?    postId    = null,
        int?    userId    = null,
        string? content   = null,
        bool    isDeleted = false)
    {
        var n = Next();
        var comment = new Comment
        {
            Id        = id      ?? n,
            Content   = content ?? $"Test comment content {n}",
            PostId    = postId  ?? post?.Id ?? n,
            UserId    = userId  ?? user?.Id ?? n,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

        if (post != null) { comment.Post   = post;   comment.PostId   = post.Id; }
        if (user != null) { comment.User   = user;   comment.UserId   = user.Id; }
        return comment;
    }
}
