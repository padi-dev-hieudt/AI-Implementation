using ForumWebsite.Extensions;
using ForumWebsite.Middleware;

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

// ── In-memory cache (used by RateLimitFilter) ─────────────────────────────────
builder.Services.AddMemoryCache();

// ── HttpContext accessor ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────

// ── Security headers — first so every response carries them ──────────────────
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

app.Run();

// Expose the implicit Program class so WebApplicationFactory<Program>
// can be used from the test project.
public partial class Program { }
