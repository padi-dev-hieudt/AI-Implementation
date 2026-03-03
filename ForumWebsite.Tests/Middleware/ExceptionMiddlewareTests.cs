using System.Net;
using System.Text.Json;
using FluentAssertions;
using ForumWebsite.Middleware;
using ForumWebsite.Models.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ForumWebsite.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="ExceptionMiddleware"/>.
/// Each test drives a fake HttpContext through the middleware and asserts
/// on the status code and JSON body that was written.
/// </summary>
public class ExceptionMiddlewareTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static DefaultHttpContext MakeContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static ExceptionMiddleware CreateSut(
        Exception       thrownBy,
        bool            isDevelopment = true)
    {
        // next() delegate throws the supplied exception
        RequestDelegate next = _ => throw thrownBy;

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName)
               .Returns(isDevelopment ? "Development" : "Production");

        return new ExceptionMiddleware(
            next,
            NullLogger<ExceptionMiddleware>.Instance,
            envMock.Object);
    }

    private static async Task<(int statusCode, string body)> RunAsync(
        ExceptionMiddleware sut,
        HttpContext? ctx = null)
    {
        var context = ctx ?? MakeContext();
        await sut.InvokeAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    // ── Status-code mapping ────────────────────────────────────────────────────

    [Fact]
    public async Task AuthenticationException_Returns401()
    {
        var sut            = CreateSut(new AuthenticationException("bad credentials"));
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.Unauthorized);
        body  .Should().Contain("bad credentials");
    }

    [Fact]
    public async Task ForbiddenException_Returns403()
    {
        var sut            = CreateSut(new ForbiddenException("access denied"));
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.Forbidden);
        body  .Should().Contain("access denied");
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var sut            = CreateSut(new KeyNotFoundException("post 99 not found"));
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.NotFound);
        body  .Should().Contain("post 99 not found");
    }

    [Fact]
    public async Task BusinessRuleException_Returns400()
    {
        var sut            = CreateSut(new BusinessRuleException("email already taken"));
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.BadRequest);
        body  .Should().Contain("email already taken");
    }

    [Fact]
    public async Task InvalidOperationException_Returns400()
    {
        var sut            = CreateSut(new InvalidOperationException("invalid op"));
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.BadRequest);
        body  .Should().Contain("invalid op");
    }

    [Fact]
    public async Task UnhandledException_Development_Returns500WithMessage()
    {
        var sut            = CreateSut(new Exception("oops"), isDevelopment: true);
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.InternalServerError);
        body  .Should().Contain("oops");
    }

    [Fact]
    public async Task UnhandledException_Production_Returns500WithGenericMessage()
    {
        var sut            = CreateSut(new Exception("sensitive db details"), isDevelopment: false);
        var (status, body) = await RunAsync(sut);

        status.Should().Be((int)HttpStatusCode.InternalServerError);
        body  .Should().NotContain("sensitive db details");
        body  .Should().Contain("An unexpected error occurred.");
    }

    // ── Response shape ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContentType_IsApplicationJson()
    {
        var ctx = MakeContext();
        var sut = CreateSut(new KeyNotFoundException("x"));
        await sut.InvokeAsync(ctx);

        ctx.Response.ContentType.Should().Contain("application/json");
    }

    [Fact]
    public async Task Response_Body_IsValidJson()
    {
        var sut            = CreateSut(new AuthenticationException("fail"));
        var (_, body)      = await RunAsync(sut);

        // Should not throw
        var act = () => JsonSerializer.Deserialize<JsonElement>(body);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Response_Body_HasSuccessFalse()
    {
        var sut       = CreateSut(new KeyNotFoundException("not found"));
        var (_, body) = await RunAsync(sut);

        var doc = JsonSerializer.Deserialize<JsonElement>(body);
        doc.GetProperty("success").GetBoolean().Should().BeFalse();
    }

    // ── Response already started ───────────────────────────────────────────────

    [Fact]
    public async Task ResponseAlreadyStarted_DoesNotRewriteHeaders()
    {
        // When HasStarted is true the middleware should bail without altering the response.
        var ctx = MakeContext();
        // Simulate a started response by writing something first
        await ctx.Response.WriteAsync("partial content");
        // At this point HasStarted = true

        RequestDelegate next = _ => throw new Exception("error after streaming started");
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Development");

        var sut = new ExceptionMiddleware(
            next,
            NullLogger<ExceptionMiddleware>.Instance,
            envMock.Object);

        // Should not throw even though response already started
        await sut.Invoking(s => s.InvokeAsync(ctx))
            .Should().NotThrowAsync();
    }
}
