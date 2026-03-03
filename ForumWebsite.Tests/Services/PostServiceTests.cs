using AutoMapper;
using FluentAssertions;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Mappings;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Implementations;
using ForumWebsite.Tests.Helpers;
using Moq;

namespace ForumWebsite.Tests.Services;

public class PostServiceTests
{
    private readonly Mock<IPostRepository> _postRepoMock = new();
    // Use the real AutoMapper profile — tests the mapping contract too
    private readonly IMapper _mapper =
        new MapperConfiguration(c => c.AddProfile<AutoMapperProfile>()).CreateMapper();

    private PostService CreateSut() => new(_postRepoMock.Object, _mapper, new Ganss.Xss.HtmlSanitizer());

    // ── GetPostsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPostsAsync_ReturnsMappedPagedResult()
    {
        var user  = TestDataFactory.CreateUser();
        var posts = new List<Post>
        {
            TestDataFactory.CreatePost(user: user),
            TestDataFactory.CreatePost(user: user)
        };

        _postRepoMock.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync((posts, 25));

        var result = await CreateSut().GetPostsAsync(1, 10);

        result.Items     .Should().HaveCount(2);
        result.TotalCount.Should().Be(25);
        result.Page      .Should().Be(1);
        result.PageSize  .Should().Be(10);
        result.TotalPages.Should().Be(3);   // ceil(25/10)
    }

    [Fact]
    public async Task GetPostsAsync_EmptyRepository_ReturnsEmptyPagedResult()
    {
        _postRepoMock.Setup(r => r.GetPagedAsync(1, 20))
                     .ReturnsAsync((new List<Post>(), 0));

        var result = await CreateSut().GetPostsAsync(1, 20);

        result.Items     .Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);  // guarded against divide-by-zero
    }

    [Fact]
    public async Task GetPostsAsync_MapsUsernameFromNavigationProperty()
    {
        var user = TestDataFactory.CreateUser(username: "alice");
        var post = TestDataFactory.CreatePost(user: user);

        _postRepoMock.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync((new[] { post }, 1));

        var result = await CreateSut().GetPostsAsync(1, 10);

        result.Items.First().Username.Should().Be("alice");
    }

    [Fact]
    public async Task GetPostsAsync_CountsOnlyNonDeletedComments()
    {
        var user    = TestDataFactory.CreateUser();
        var post    = TestDataFactory.CreatePost(user: user);
        post.Comments.Add(TestDataFactory.CreateComment(post: post, user: user));
        post.Comments.Add(TestDataFactory.CreateComment(post: post, user: user, isDeleted: true));

        _postRepoMock.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync((new[] { post }, 1));

        var result = await CreateSut().GetPostsAsync(1, 10);

        result.Items.First().CommentCount.Should().Be(1);  // deleted comment excluded
    }

    // ── GetPostByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPostByIdAsync_ExistingPost_ReturnsDetailDtoAndIncrementsView()
    {
        var user    = TestDataFactory.CreateUser();
        var post    = TestDataFactory.CreatePost(id: 42, user: user, viewCount: 5);
        var comment = TestDataFactory.CreateComment(post: post, user: user);
        post.Comments.Add(comment);

        _postRepoMock.Setup(r => r.GetByIdWithDetailsAsync(42)).ReturnsAsync(post);
        _postRepoMock.Setup(r => r.IncrementViewCountAsync(42, default)).Returns(Task.CompletedTask);

        var result = await CreateSut().GetPostByIdAsync(42);

        result.Id          .Should().Be(42);
        result.ViewCount   .Should().Be(6);   // incremented in service (+1 optimistic)
        result.CommentCount.Should().Be(1);

        _postRepoMock.Verify(r => r.IncrementViewCountAsync(42, default), Times.Once);
    }

    [Fact]
    public async Task GetPostByIdAsync_NonExistentPost_ThrowsKeyNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((Post?)null);

        await CreateSut().Invoking(s => s.GetPostByIdAsync(99))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    // ── CreatePostAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePostAsync_ValidDto_CreatesAndReturnsPost()
    {
        var user = TestDataFactory.CreateUser(id: 3);
        var post = TestDataFactory.CreatePost(id: 10, user: user);

        _postRepoMock.Setup(r => r.CreateAsync(It.IsAny<Post>()))
                     .ReturnsAsync((Post p) => { p.Id = 10; return p; });
        _postRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10)).ReturnsAsync(post);

        var dto    = new CreatePostDto { Title = "  Hello World  ", Content = "  Some content  " };
        var result = await CreateSut().CreatePostAsync(userId: 3, dto: dto);

        result.Id    .Should().Be(10);
        result.UserId.Should().Be(3);

        // Verify title/content were trimmed before persisting
        _postRepoMock.Verify(r => r.CreateAsync(
            It.Is<Post>(p => p.Title == "Hello World" && p.Content == "Some content")), Times.Once);
    }

    // ── UpdatePostAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePostAsync_Owner_UpdatesSuccessfully()
    {
        var user = TestDataFactory.CreateUser(id: 5);
        var post = TestDataFactory.CreatePost(id: 20, user: user);
        var updated = TestDataFactory.CreatePost(id: 20, user: user,
            title: "New Title", content: "New content");

        _postRepoMock.Setup(r => r.GetByIdAsync(20))              .ReturnsAsync(post);
        _postRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Post>())) .ReturnsAsync((Post p) => p);
        _postRepoMock.Setup(r => r.GetByIdWithDetailsAsync(20))   .ReturnsAsync(updated);

        var dto    = new UpdatePostDto { Title = "New Title", Content = "New content" };
        var result = await CreateSut().UpdatePostAsync(
            postId: 20, requestingUserId: 5, requestingUserRole: UserRoles.User, dto: dto);

        result.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdatePostAsync_Admin_UpdatesOtherUsersPost()
    {
        var owner = TestDataFactory.CreateUser(id: 1);
        var post  = TestDataFactory.CreatePost(id: 30, user: owner);
        var updated = TestDataFactory.CreatePost(id: 30, user: owner);

        _postRepoMock.Setup(r => r.GetByIdAsync(30))              .ReturnsAsync(post);
        _postRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Post>())) .ReturnsAsync((Post p) => p);
        _postRepoMock.Setup(r => r.GetByIdWithDetailsAsync(30))   .ReturnsAsync(updated);

        var dto = new UpdatePostDto { Title = "Admin Edit", Content = "Admin content" };
        // Admin user (id: 99, role: Admin) updating post owned by user id 1
        var result = await CreateSut().UpdatePostAsync(
            postId: 30, requestingUserId: 99, requestingUserRole: UserRoles.Admin, dto: dto);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePostAsync_NonOwnerNonAdmin_ThrowsForbiddenException()
    {
        var owner = TestDataFactory.CreateUser(id: 1);
        var post  = TestDataFactory.CreatePost(id: 40, user: owner);

        _postRepoMock.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(post);

        var dto = new UpdatePostDto { Title = "Hijack", Content = "Hijacked" };
        await CreateSut().Invoking(s =>
                s.UpdatePostAsync(postId: 40, requestingUserId: 99,
                    requestingUserRole: UserRoles.User, dto: dto))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*permission*");
    }

    [Fact]
    public async Task UpdatePostAsync_SoftDeletedPost_ThrowsKeyNotFoundException()
    {
        var post = TestDataFactory.CreatePost(id: 50, isDeleted: true);
        _postRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(post);

        var dto = new UpdatePostDto { Title = "T", Content = "C" };
        await CreateSut().Invoking(s =>
                s.UpdatePostAsync(postId: 50, requestingUserId: 1,
                    requestingUserRole: UserRoles.User, dto: dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdatePostAsync_NonExistentPost_ThrowsKeyNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        var dto = new UpdatePostDto { Title = "T", Content = "C" };
        await CreateSut().Invoking(s =>
                s.UpdatePostAsync(999, 1, UserRoles.User, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeletePostAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePostAsync_Owner_SoftDeletesPost()
    {
        var user = TestDataFactory.CreateUser(id: 2);
        var post = TestDataFactory.CreatePost(id: 60, user: user);

        _postRepoMock.Setup(r => r.GetByIdAsync(60))              .ReturnsAsync(post);
        _postRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Post>())) .ReturnsAsync((Post p) => p);

        await CreateSut().DeletePostAsync(postId: 60, requestingUserId: 2,
            requestingUserRole: UserRoles.User);

        _postRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Post>(p => p.IsDeleted && p.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_Admin_SoftDeletesOtherUsersPost()
    {
        var owner = TestDataFactory.CreateUser(id: 1);
        var post  = TestDataFactory.CreatePost(id: 70, user: owner);

        _postRepoMock.Setup(r => r.GetByIdAsync(70))              .ReturnsAsync(post);
        _postRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Post>())) .ReturnsAsync((Post p) => p);

        // Should not throw
        await CreateSut().Invoking(s =>
                s.DeletePostAsync(postId: 70, requestingUserId: 999,
                    requestingUserRole: UserRoles.Admin))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeletePostAsync_NonOwner_ThrowsForbiddenException()
    {
        var owner = TestDataFactory.CreateUser(id: 1);
        var post  = TestDataFactory.CreatePost(id: 80, user: owner);

        _postRepoMock.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(post);

        await CreateSut().Invoking(s =>
                s.DeletePostAsync(80, requestingUserId: 55, requestingUserRole: UserRoles.User))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeletePostAsync_AlreadyDeleted_ThrowsKeyNotFoundException()
    {
        var post = TestDataFactory.CreatePost(id: 90, isDeleted: true);
        _postRepoMock.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(post);

        await CreateSut().Invoking(s =>
                s.DeletePostAsync(90, 1, UserRoles.User))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
