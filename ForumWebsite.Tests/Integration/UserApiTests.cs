using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.User;

namespace ForumWebsite.Tests.Integration;

/// <summary>
/// End-to-end tests for the /api/user endpoints.
/// All tests share a single <see cref="ForumWebApplicationFactory"/> instance
/// (and therefore a single SQLite in-memory database) via IClassFixture.
/// Each test creates users with unique suffixes to avoid cross-test conflicts.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class UserApiTests
{
    private readonly ForumWebApplicationFactory _factory;

    public UserApiTests(ForumWebApplicationFactory factory) =>
        _factory = factory;

    // ── Helpers ────────────────────────────────────────────────────────────────

    private HttpClient NewClient() => _factory.CreateClient();

    private static object RegisterPayload(string suffix) => new
    {
        username        = $"user_{suffix}",
        email           = $"user_{suffix}@example.com",
        password        = "Password1!",
        confirmPassword = "Password1!"
    };

    private static string Uid() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Registers a new unique user and returns their JWT token from the response body.
    /// </summary>
    private async Task<(string token, string username)> RegisterAsync(HttpClient client)
    {
        var suffix   = Uid();
        var response = await client.PostAsJsonAsync("/api/user/register", RegisterPayload(suffix));
        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();

        return (body!.Data!.Token, body.Data.Username);
    }

    // ── POST /api/user/register ────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidPayload_Returns201WithAuthResponse()
    {
        var suffix   = Uid();
        var client   = NewClient();
        var response = await client.PostAsJsonAsync("/api/user/register", RegisterPayload(suffix));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        body!.Success         .Should().BeTrue();
        body.Data!.Username   .Should().Be($"user_{suffix}");
        body.Data.Token       .Should().NotBeNullOrWhiteSpace();
        body.Data.Role        .Should().Be("User");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var suffix = Uid();
        var client = NewClient();

        // First registration — must succeed
        await client.PostAsJsonAsync("/api/user/register", RegisterPayload(suffix));

        // Second registration with the same email — must fail
        var duplicate = new
        {
            username        = $"other_{suffix}",   // different username
            email           = $"user_{suffix}@example.com",  // same email
            password        = "Password1!",
            confirmPassword = "Password1!"
        };

        var response = await client.PostAsJsonAsync("/api/user/register", duplicate);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400WithValidationErrors()
    {
        var client = NewClient();
        var bad    = new { username = "x", email = "not-an-email", password = "weak" };

        var response = await client.PostAsJsonAsync("/api/user/register", bad);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/user/login ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var suffix = Uid();
        var client = NewClient();

        // Register first
        await client.PostAsJsonAsync("/api/user/register", RegisterPayload(suffix));

        // Now login
        var loginPayload = new
        {
            email    = $"user_{suffix}@example.com",
            password = "Password1!"
        };
        var response = await client.PostAsJsonAsync("/api/user/login", loginPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>();
        body!.Success     .Should().BeTrue();
        body.Data!.Token  .Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var suffix = Uid();
        var client = NewClient();
        await client.PostAsJsonAsync("/api/user/register", RegisterPayload(suffix));

        var loginPayload = new
        {
            email    = $"user_{suffix}@example.com",
            password = "WrongPassword1!"
        };
        var response = await client.PostAsJsonAsync("/api/user/login", loginPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client       = NewClient();
        var loginPayload = new { email = "nobody@nowhere.com", password = "Password1!" };

        var response = await client.PostAsJsonAsync("/api/user/login", loginPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/user/me ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var client   = NewClient();
        var response = await client.GetAsync("/api/user/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_Authenticated_Returns200WithProfile()
    {
        var client           = NewClient();
        var (token, username) = await RegisterAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/user/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
        body!.Success          .Should().BeTrue();
        body.Data!.Username    .Should().Be(username);
    }
}
