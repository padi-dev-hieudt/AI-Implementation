using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.Post;
using ForumWebsite.Models.DTOs.User;
using ForumWebsite.Models.Entities;

namespace ForumWebsite.Tests.Integration;

/// <summary>
/// End-to-end tests for the /api/post endpoints.
/// Uses <see cref="TestJwt.IssueToken"/> to generate Bearer tokens out-of-band
/// for scenarios where a full register/login flow would be redundant noise.
/// The <see cref="ForumWebApplicationFactory"/> is shared across all tests in the
/// class via <see cref="IClassFixture{TFixture}"/>.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class PostApiTests
{
    private readonly ForumWebApplicationFactory _factory;

    public PostApiTests(ForumWebApplicationFactory factory) =>
        _factory = factory;

    // ── Helpers ────────────────────────────────────────────────────────────────

    private HttpClient NewClient() => _factory.CreateClient();

    private static string Uid() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Registers a fresh user and returns an authenticated HttpClient + userId.
    /// </summary>
    private async Task<(HttpClient client, int userId, string username)> RegisteredClientAsync()
    {
        var suffix   = Uid();
        var client   = NewClient();

        var registerPayload = new
        {
            username        = $"poster_{suffix}",
            email           = $"poster_{suffix}@example.com",
            password        = "Password1!",
            confirmPassword = "Password1!"
        };

        var response = await client.PostAsJsonAsync("/api/user/register", registerPayload);
        response.EnsureSuccessStatusCode();

        var body   = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        var token  = body!.Data!.Token;

        // Fetch the user profile to get the database-assigned userId
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var meResponse = await client.GetAsync("/api/user/me");
        var meBody     = await meResponse.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        var userId     = meBody!.Data!.Id;

        return (client, userId, body.Data.Username);
    }

    private static object ValidPostPayload() => new
    {
        title   = "Integration Test Post Title",
        content = "This is the content of the integration test post, it is long enough."
    };

    // ── GET /api/post ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPosts_Anonymous_Returns200WithPagedResult()
    {
        var response = await NewClient().GetAsync("/api/post");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PostDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPosts_PageSizeClamped_AcceptsLargePageSize()
    {
        // pageSize=100 should be clamped to 50 server-side, but not error
        var response = await NewClient().GetAsync("/api/post?pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/post ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePost_Unauthenticated_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/post", ValidPostPayload());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePost_Authenticated_Returns201WithPost()
    {
        var (client, _, _) = await RegisteredClientAsync();

        var response = await client.PostAsJsonAsync("/api/post", ValidPostPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PostDetailDto>>();
        body!.Success        .Should().BeTrue();
        body.Data!.Title     .Should().Be("Integration Test Post Title");
    }

    [Fact]
    public async Task CreatePost_InvalidPayload_Returns400()
    {
        var (client, _, _) = await RegisteredClientAsync();

        var bad      = new { title = "Hi", content = "Short" };   // fails validation
        var response = await client.PostAsJsonAsync("/api/post", bad);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/post/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPostById_ExistingPost_Returns200()
    {
        var (client, _, _) = await RegisteredClientAsync();

        // Create a post first
        var createResponse = await client.PostAsJsonAsync("/api/post", ValidPostPayload());
        var createBody     = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PostDetailDto>>();
        var postId = createBody!.Data!.Id;

        // Anonymous fetch
        var response = await NewClient().GetAsync($"/api/post/{postId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PostDetailDto>>();
        body!.Data!.Id.Should().Be(postId);
    }

    [Fact]
    public async Task GetPostById_NonExistent_Returns404()
    {
        var response = await NewClient().GetAsync("/api/post/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/post/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task DeletePost_Unauthenticated_Returns401()
    {
        var response = await NewClient().DeleteAsync("/api/post/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePost_Owner_Returns200()
    {
        var (client, _, _) = await RegisteredClientAsync();

        // Create a post
        var createResponse = await client.PostAsJsonAsync("/api/post", ValidPostPayload());
        var createBody     = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PostDetailDto>>();
        var postId = createBody!.Data!.Id;

        // Delete it as the owner
        var deleteResponse = await client.DeleteAsync($"/api/post/{postId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePost_NonOwner_Returns403()
    {
        var (ownerClient, ownerId, _)      = await RegisteredClientAsync();
        var (nonOwnerClient, _, _) = await RegisteredClientAsync();

        // Owner creates a post
        var createResponse = await ownerClient.PostAsJsonAsync("/api/post", ValidPostPayload());
        var createBody     = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<PostDetailDto>>();
        var postId = createBody!.Data!.Id;

        // Non-owner tries to delete
        var deleteResponse = await nonOwnerClient.DeleteAsync($"/api/post/{postId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePost_NonExistent_Returns404()
    {
        var (client, _, _) = await RegisteredClientAsync();

        var response = await client.DeleteAsync("/api/post/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
