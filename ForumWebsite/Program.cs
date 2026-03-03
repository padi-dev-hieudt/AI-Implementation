using ForumWebsite.Data.Context;
using ForumWebsite.Data.Seed;
using ForumWebsite.Extensions;
using ForumWebsite.Middleware;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Database (EF Core + SQL Server) ──────────────────────────────────────────
builder.Services.AddDatabase(builder.Configuration);

// ── Data access layer ─────────────────────────────────────────────────────────
builder.Services.AddRepositories();

// ── Business logic layer ──────────────────────────────────────────────────────
builder.Services.AddApplicationServices();

// ── JWT Bearer authentication ─────────────────────────────────────────────────
builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Role-based authorization ──────────────────────────────────────────────────
builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCorsPolicy(builder.Configuration);

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddValidation();

// ── AutoMapper ────────────────────────────────────────────────────────────────
builder.Services.AddAutoMapperProfiles();

// ── In-memory cache (used by RateLimitFilter + ViewCountService) ──────────────
builder.Services.AddMemoryCache();

// ── HttpContext accessor ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Forwarded headers (correct RemoteIpAddress behind nginx / IIS ARR) ────────
// Without this, Connection.RemoteIpAddress is the proxy's loopback IP, making
// all guest view-count keys identical and defeating per-client deduplication.
//
// Security note: KnownNetworks/KnownProxies are cleared to trust all upstream
// proxies in Phase 1. For Phase 2 production, restrict to your proxy subnet:
//   options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();  // Phase 2: restrict to known proxy IPs/subnets
    options.KnownProxies.Clear();
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────

// ── Forwarded headers — absolute first, before any middleware reads RemoteIpAddress ──
app.UseForwardedHeaders();

// ── Security headers — after forwarding so headers reflect the real origin ───
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    // HTTPS redirection only in non-development. In development, the launchSettings.json
    // profile starts on HTTPS directly, so no redirect is needed. Keeping this in
    // development also breaks integration tests: HttpClient strips the Authorization
    // header when following an HTTP→HTTPS redirect (Bearer token leakage prevention).
    app.UseHttpsRedirection();
}

// ── Global JSON error handler — before routing ────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();

app.UseRouting();

// CORS must be between UseRouting and UseAuthentication
app.UseCors("ForumCorsPolicy");

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── Seed fake data in Development ─────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

app.Run();

// Expose the implicit Program class so WebApplicationFactory<Program>
// can be used from the test project.
public partial class Program { }
