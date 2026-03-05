using AutoMapper;
using FluentAssertions;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Mappings;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Comment;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Implementations;
using ForumWebsite.Tests.Helpers;
using Moq;

namespace ForumWebsite.Tests.Services;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepoMock = new();
    private readonly Mock<IPostRepository>    _postRepoMock    = new();
    private readonly IMapper _mapper =
        new MapperConfiguration(c => c.AddProfile<AutoMapperProfile>()).CreateMapper();

    private CommentService CreateSut() =>
        new(_commentRepoMock.Object, _postRepoMock.Object, _mapper);

    // ── GetCommentsByPostAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCommentsByPostAsync_ExistingPost_ReturnsMappedComments()
    {
        var user = TestDataFactory.CreateUser();
        var post = TestDataFactory.CreatePost(id: 1, user: user);
        var comments = new List<Comment>
        {
            TestDataFactory.CreateComment(post: post, user: user),
            TestDataFactory.CreateComment(post: post, user: user)
        };

        _postRepoMock   .Setup(r => r.GetByIdAsync(1))          .ReturnsAsync(post);
        _commentRepoMock.Setup(r => r.GetByPostIdAsync(1))      .ReturnsAsync(comments);

        var result = await CreateSut().GetCommentsByPostAsync(1);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCommentsByPostAsync_MapsUsernameFromNavigationProperty()
    {
        var user    = TestDataFactory.CreateUser(username: "bob");
        var post    = TestDataFactory.CreatePost(id: 2, user: user);
        var comment = TestDataFactory.CreateComment(post: post, user: user);

        _postRepoMock   .Setup(r => r.GetByIdAsync(2))    .ReturnsAsync(post);
        _commentRepoMock.Setup(r => r.GetByPostIdAsync(2)).ReturnsAsync(new[] { comment });

        var result = (await CreateSut().GetCommentsByPostAsync(2)).ToList();

        result.Single().Username.Should().Be("bob");
    }

    [Fact]
    public async Task GetCommentsByPostAsync_NonExistentPost_ThrowsKeyNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Post?)null);

        await CreateSut().Invoking(s => s.GetCommentsByPostAsync(99))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    [Fact]
    public async Task GetCommentsByPostAsync_SoftDeletedPost_ThrowsKeyNotFoundException()
    {
        var post = TestDataFactory.CreatePost(id: 5, isDeleted: true);
        _postRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(post);

        await CreateSut().Invoking(s => s.GetCommentsByPostAsync(5))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── AddCommentAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_ValidInput_TrimsContentAndReturnsDto()
    {
        var user    = TestDataFactory.CreateUser(id: 7);
        var post    = TestDataFactory.CreatePost(id: 10, user: user);
        var created = TestDataFactory.CreateComment(id: 100, post: post, user: user,
            content: "Hello world");

        _postRepoMock   .Setup(r => r.GetByIdAsync(10))                         .ReturnsAsync(post);
        _commentRepoMock.Setup(r => r.CreateAsync(It.IsAny<Comment>()))          .ReturnsAsync((Comment c) => c);
        _commentRepoMock.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>()))  .ReturnsAsync(created);

        var dto    = new CreateCommentDto { PostId = 10, Content = "  Hello world  " };
        var result = await CreateSut().AddCommentAsync(userId: 7, dto: dto);

        result.Should().NotBeNull();
        result.PostId.Should().Be(10);

        // Content must be trimmed before storing
        _commentRepoMock.Verify(r => r.CreateAsync(
            It.Is<Comment>(c => c.Content == "Hello world" && c.UserId == 7)), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_NonExistentPost_ThrowsKeyNotFoundException()
    {
        _postRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Post?)null);

        var dto = new CreateCommentDto { PostId = 999, Content = "Nice post!" };
        await CreateSut().Invoking(s => s.AddCommentAsync(1, dto))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task AddCommentAsync_SoftDeletedPost_ThrowsKeyNotFoundException()
    {
        var post = TestDataFactory.CreatePost(id: 20, isDeleted: true);
        _postRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(post);

        var dto = new CreateCommentDto { PostId = 20, Content = "Nice post!" };
        await CreateSut().Invoking(s => s.AddCommentAsync(1, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateCommentAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCommentAsync_Owner_UpdatesSuccessfully()
    {
        var user    = TestDataFactory.CreateUser(id: 3);
        var post    = TestDataFactory.CreatePost(user: user);
        var comment = TestDataFactory.CreateComment(id: 50, post: post, user: user);
        var updated = TestDataFactory.CreateComment(id: 50, post: post, user: user,
            content: "Edited content");

        _commentRepoMock.Setup(r => r.GetByIdAsync(50))                       .ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Comment>()))       .ReturnsAsync((Comment c) => c);
        _commentRepoMock.Setup(r => r.GetByIdWithDetailsAsync(50))            .ReturnsAsync(updated);

        var dto    = new UpdateCommentDto { Content = "Edited content" };
        var result = await CreateSut().UpdateCommentAsync(
            commentId: 50, requestingUserId: 3, requestingUserRole: UserRoles.User, dto: dto);

        result.Content.Should().Be("Edited content");
    }

    [Fact]
    public async Task UpdateCommentAsync_Admin_ThrowsForbiddenException_SpecOwnerOnly()
    {
        // Phase-01 spec: "Edit comment (only owner)" — admins may delete but NOT edit.
        var owner   = TestDataFactory.CreateUser(id: 1);
        var post    = TestDataFactory.CreatePost(user: owner);
        var comment = TestDataFactory.CreateComment(id: 60, post: post, user: owner);

        _commentRepoMock.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(comment);

        var dto = new UpdateCommentDto { Content = "Admin attempt" };
        // Admin user (id: 99) cannot edit a comment they don't own
        await CreateSut().Invoking(s =>
                s.UpdateCommentAsync(60, requestingUserId: 99,
                    requestingUserRole: UserRoles.Admin, dto: dto))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*owner*");
    }

    [Fact]
    public async Task UpdateCommentAsync_NonOwnerNonAdmin_ThrowsForbiddenException()
    {
        var owner   = TestDataFactory.CreateUser(id: 1);
        var post    = TestDataFactory.CreatePost(user: owner);
        var comment = TestDataFactory.CreateComment(id: 70, post: post, user: owner);

        _commentRepoMock.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(comment);

        var dto = new UpdateCommentDto { Content = "Hijack" };
        await CreateSut().Invoking(s =>
                s.UpdateCommentAsync(70, requestingUserId: 55,
                    requestingUserRole: UserRoles.User, dto: dto))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*owner*");
    }

    [Fact]
    public async Task UpdateCommentAsync_SoftDeletedComment_ThrowsKeyNotFoundException()
    {
        var comment = TestDataFactory.CreateComment(id: 80, isDeleted: true);
        _commentRepoMock.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(comment);

        var dto = new UpdateCommentDto { Content = "X" };
        await CreateSut().Invoking(s =>
                s.UpdateCommentAsync(80, 1, UserRoles.User, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateCommentAsync_NonExistentComment_ThrowsKeyNotFoundException()
    {
        _commentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Comment?)null);

        var dto = new UpdateCommentDto { Content = "X" };
        await CreateSut().Invoking(s =>
                s.UpdateCommentAsync(999, 1, UserRoles.User, dto))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteCommentAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCommentAsync_Owner_SoftDeletesComment()
    {
        var user    = TestDataFactory.CreateUser(id: 4);
        var post    = TestDataFactory.CreatePost(user: user);
        var comment = TestDataFactory.CreateComment(id: 90, post: post, user: user);

        _commentRepoMock.Setup(r => r.GetByIdAsync(90))                 .ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Comment>())) .ReturnsAsync((Comment c) => c);

        await CreateSut().DeleteCommentAsync(commentId: 90,
            requestingUserId: 4, requestingUserRole: UserRoles.User);

        _commentRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Comment>(c => c.IsDeleted && c.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_Admin_SoftDeletesOtherUsersComment()
    {
        var owner   = TestDataFactory.CreateUser(id: 1);
        var post    = TestDataFactory.CreatePost(user: owner);
        var comment = TestDataFactory.CreateComment(id: 95, post: post, user: owner);

        _commentRepoMock.Setup(r => r.GetByIdAsync(95))                 .ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Comment>())) .ReturnsAsync((Comment c) => c);

        await CreateSut().Invoking(s =>
                s.DeleteCommentAsync(95, requestingUserId: 999, requestingUserRole: UserRoles.Admin))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteCommentAsync_NonOwner_ThrowsForbiddenException()
    {
        var owner   = TestDataFactory.CreateUser(id: 1);
        var post    = TestDataFactory.CreatePost(user: owner);
        var comment = TestDataFactory.CreateComment(id: 100, post: post, user: owner);

        _commentRepoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(comment);

        await CreateSut().Invoking(s =>
                s.DeleteCommentAsync(100, requestingUserId: 55, requestingUserRole: UserRoles.User))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task DeleteCommentAsync_AlreadyDeleted_ThrowsKeyNotFoundException()
    {
        var comment = TestDataFactory.CreateComment(id: 105, isDeleted: true);
        _commentRepoMock.Setup(r => r.GetByIdAsync(105)).ReturnsAsync(comment);

        await CreateSut().Invoking(s =>
                s.DeleteCommentAsync(105, 1, UserRoles.User))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
