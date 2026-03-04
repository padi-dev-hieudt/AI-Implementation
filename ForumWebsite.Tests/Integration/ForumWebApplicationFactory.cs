using ForumWebsite.Data.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ForumWebsite.Tests.Integration;

/// <summary>
/// Spins up the full ASP.NET Core pipeline against a shared SQLite in-memory database.
///
/// SQLite is used instead of EF InMemory because <see cref="PostRepository"/> calls
/// <c>ExecuteSqlInterpolatedAsync</c> (raw SQL), which InMemory does not support.
///
/// The <see cref="_keepAlive"/> connection is held open for the lifetime of the factory
/// to prevent SQLite from destroying the in-memory database between service-scope creations.
///
/// JWT note: AddJwtAuthentication bakes TokenValidationParameters at startup using
/// the appsettings.json values. ConfigureAppConfiguration adds overrides that are visible
/// to IConfiguration at request-time (e.g. JwtService.GenerateToken) but NOT at startup.
/// PostConfigure&lt;JwtBearerOptions&gt; aligns the validation params with what JwtService generates.
/// </summary>
public class ForumWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keepAlive;

    public ForumWebApplicationFactory()
    {
        _keepAlive = new SqliteConnection("DataSource=:memory:");
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ── Override configuration so startup validation helpers don't throw ─────
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Provides a non-empty string so AddDatabase() doesn't throw.
                // The DbContext is replaced in ConfigureTestServices below.
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",

                // JWT — same key/issuer/audience used in TestJwt.IssueToken helpers.
                // These are read by JwtService.GenerateToken at request-time via IConfiguration.
                ["JwtSettings:SecretKey"]   = TestJwt.Secret,
                ["JwtSettings:Issuer"]      = TestJwt.Issuer,
                ["JwtSettings:Audience"]    = TestJwt.Audience,
                ["JwtSettings:ExpiryHours"] = "1",

                // Satisfy the CORS extension (must have at least one origin or it locks down)
                ["Cors:AllowedOrigins:0"]   = "http://localhost"
            });
        });

        // ── Replace SQL Server DbContext with SQLite in-memory ───────────────────
        builder.ConfigureTestServices(services =>
        {
            // Remove the SQL-Server-backed DbContextOptions registered by AddDatabase()
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_keepAlive));

            // ── Align JWT validation with test token generation ───────────────────
            // AddJwtAuthentication bakes TokenValidationParameters at startup from
            // appsettings.json (before ConfigureAppConfiguration overrides apply).
            // JwtService.GenerateToken reads IConfiguration at request-time and DOES
            // see the test overrides, so tokens are signed with TestJwt.Secret.
            // PostConfigure runs after all Configure<> calls and realigns the validator.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer      = TestJwt.Issuer;
                options.TokenValidationParameters.ValidAudience     = TestJwt.Audience;
                options.TokenValidationParameters.IssuerSigningKey  =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwt.Secret));
            });
        });

        // Use the test environment so the app doesn't load appsettings.Production.json
        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Creates the in-memory schema BEFORE the host starts.
    ///
    /// Order matters: Program.cs calls DatabaseSeeder.SeedAsync during startup,
    /// which queries db.Posts.Any(). If EnsureCreated runs after Start() the
    /// seeder hits "no such table: Posts".  Building without starting first lets
    /// us create the schema while the host is still idle.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // 1. Build the host (applies ConfigureTestServices overrides) but don't start yet.
        var host = builder.Build();

        // 2. Create SQLite schema before any startup code (seeder) runs.
        using (var scope = host.Services.CreateScope())
        {
            scope.ServiceProvider
                 .GetRequiredService<ApplicationDbContext>()
                 .Database.EnsureCreated();
        }

        // 3. Now start — Program.cs seeder runs and finds tables already created.
        host.Start();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Close();
            _keepAlive.Dispose();
        }
    }
}

/// <summary>
/// Shared JWT constants — integration tests use these to issue tokens out-of-band
/// (avoiding a real login when testing non-auth endpoints).
/// Must match the values injected via <see cref="ForumWebApplicationFactory"/>.
/// </summary>
public static class TestJwt
{
    // 32-byte minimum for HMAC-SHA256. 32 chars × 1 byte/char (ASCII) = 32 bytes.
    public const string Secret   = "integration-test-key-32-bytes!!!";   // 32 chars
    public const string Issuer   = "TestIssuer";
    public const string Audience = "TestAudience";

    /// <summary>Issues a short-lived token for the supplied user claims.</summary>
    public static string IssueToken(int userId, string username, string role)
    {
        var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(Secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                        key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name,           username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,           role),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
