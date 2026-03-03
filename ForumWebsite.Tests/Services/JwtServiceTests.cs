using FluentAssertions;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Implementations;
using ForumWebsite.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ForumWebsite.Tests.Services;

public class JwtServiceTests
{
    // A 256-bit (32-byte) key is the minimum for HMAC-SHA256.
    private const string TestSecret = "super-secret-test-key-32-bytes!!";

    private static IConfiguration BuildConfig(
        string? secret      = TestSecret,
        string  issuer      = "TestIssuer",
        string  audience    = "TestAudience",
        string  expiryHours = "1") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]    = secret,
                ["JwtSettings:Issuer"]       = issuer,
                ["JwtSettings:Audience"]     = audience,
                ["JwtSettings:ExpiryHours"]  = expiryHours,
            })
            .Build();

    private static JwtService CreateSut(IConfiguration? cfg = null) =>
        new(cfg ?? BuildConfig());

    // ── GenerateToken ──────────────────────────────────────────────────────────

    [Fact]
    public void GenerateToken_ValidUser_ReturnsNonEmptyJwtString()
    {
        var user   = TestDataFactory.CreateUser(id: 1, username: "alice");
        var token  = CreateSut().GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        // A JWT has exactly three Base64url segments separated by dots.
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_ContainsNameIdentifierClaim()
    {
        var user  = TestDataFactory.CreateUser(id: 42, username: "bob");
        var token = CreateSut().GenerateToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Claims
              .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
              ?.Value
              .Should().Be("42");
    }

    [Fact]
    public void GenerateToken_ContainsUsernameClaim()
    {
        var user  = TestDataFactory.CreateUser(id: 1, username: "charlie");
        var token = CreateSut().GenerateToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Claims
              .FirstOrDefault(c => c.Type == ClaimTypes.Name)
              ?.Value
              .Should().Be("charlie");
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var user  = TestDataFactory.CreateUser(id: 1, email: "test@example.com");
        var token = CreateSut().GenerateToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Claims
              .FirstOrDefault(c => c.Type == ClaimTypes.Email)
              ?.Value
              .Should().Be("test@example.com");
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaim()
    {
        var user  = TestDataFactory.CreateUser(id: 1, role: UserRoles.Admin);
        var token = CreateSut().GenerateToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Claims
              .FirstOrDefault(c => c.Type == ClaimTypes.Role)
              ?.Value
              .Should().Be(UserRoles.Admin);
    }

    [Fact]
    public void GenerateToken_ContainsUniqueJtiClaim()
    {
        var user   = TestDataFactory.CreateUser(id: 1);
        var sut    = CreateSut();
        var token1 = sut.GenerateToken(user);
        var token2 = sut.GenerateToken(user);

        var jti1 = new JwtSecurityTokenHandler().ReadJwtToken(token1)
                        .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = new JwtSecurityTokenHandler().ReadJwtToken(token2)
                        .Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);  // each token must have a unique ID
    }

    [Fact]
    public void Constructor_MissingSecretKey_ThrowsInvalidOperationException()
    {
        // Config is validated at construction so misconfiguration fails fast at startup.
        var cfg = BuildConfig(secret: null);   // SecretKey not present

        var act = () => new JwtService(cfg);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*SecretKey*");
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        var user  = TestDataFactory.CreateUser(id: 1);
        var token = CreateSut(BuildConfig(issuer: "MyIssuer", audience: "MyAudience")).GenerateToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsed.Issuer          .Should().Be("MyIssuer");
        parsed.Audiences.First().Should().Be("MyAudience");
    }

    // ── GetTokenExpiry ─────────────────────────────────────────────────────────

    [Fact]
    public void GetTokenExpiry_ConfiguredHours_ReturnsCorrectExpiry()
    {
        var sut    = CreateSut(BuildConfig(expiryHours: "48"));
        var before = DateTime.UtcNow.AddHours(47).AddMinutes(59);
        var after  = DateTime.UtcNow.AddHours(48).AddMinutes(1);

        var expiry = sut.GetTokenExpiry();

        expiry.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void GetTokenExpiry_MissingConfig_DefaultsTo24Hours()
    {
        // ExpiryHours key absent — should fall back to 24 h
        var cfg    = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = TestSecret
            }).Build();

        var sut    = new JwtService(cfg);
        var before = DateTime.UtcNow.AddHours(23).AddMinutes(59);
        var after  = DateTime.UtcNow.AddHours(24).AddMinutes(1);

        var expiry = sut.GetTokenExpiry();

        expiry.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void GetTokenExpiry_NonNumericConfig_DefaultsTo24Hours()
    {
        var sut    = CreateSut(BuildConfig(expiryHours: "not-a-number"));
        var before = DateTime.UtcNow.AddHours(23).AddMinutes(59);
        var after  = DateTime.UtcNow.AddHours(24).AddMinutes(1);

        var expiry = sut.GetTokenExpiry();

        expiry.Should().BeAfter(before).And.BeBefore(after);
    }
}
