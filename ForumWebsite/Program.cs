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
}

// ── Global JSON error handler — before routing ────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
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
