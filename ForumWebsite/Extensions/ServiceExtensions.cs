using FluentValidation;
using FluentValidation.AspNetCore;
using ForumWebsite.Data.Context;
using ForumWebsite.Data.Repositories.Implementations;
using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Mappings;
using ForumWebsite.Services.Implementations;
using ForumWebsite.Services.Interfaces;
using ForumWebsite.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ForumWebsite.Extensions
{
    /// <summary>
    /// Extension methods that group related DI registrations.
    /// Keeps Program.cs clean and each cross-cutting concern modular.
    /// </summary>
    public static class ServiceExtensions
    {
        // ── Database ──────────────────────────────────────────────────────────────
        public static IServiceCollection AddDatabase(
            this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Validate at startup — fail fast rather than at first DB call
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is missing or empty.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                ));

            return services;
        }

        // ── Repositories ──────────────────────────────────────────────────────────
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository,    UserRepository>();
            services.AddScoped<IPostRepository,    PostRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            return services;
        }

        // ── Application Services ──────────────────────────────────────────────────
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton(BuildHtmlSanitizer());
            services.AddScoped<IJwtService,     JwtService>();
            services.AddScoped<IUserService,    UserService>();
            services.AddScoped<IPostService,    PostService>();
            services.AddScoped<ICommentService, CommentService>();
            return services;
        }

        private static Ganss.Xss.HtmlSanitizer BuildHtmlSanitizer()
        {
            var san = new Ganss.Xss.HtmlSanitizer();

            // Tags produced by Quill snow toolbar
            san.AllowedTags.UnionWith(new[] {
                "p", "br", "strong", "em", "u", "s",
                "h2", "h3", "ul", "ol", "li",
                "blockquote", "pre", "code",
                "a", "img", "span"
            });

            san.AllowedAttributes.UnionWith(new[] { "href", "src", "alt", "class", "target", "rel" });

            // http/https only — data: is intentionally excluded.
            // Allowing data: globally would permit <a href="data:text/html,<script>..."> XSS via
            // data-URI links that execute outside this page's CSP. Clipboard-pasted images from
            // Quill should be handled via a dedicated /api/upload endpoint in production.
            san.AllowedSchemes.UnionWith(new[] { "http", "https" });

            return san;
        }

        // ── JWT Authentication ────────────────────────────────────────────────────
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("JwtSettings");
            var secretKey  = jwtSection["SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

            // Enforce minimum key length for HMAC-SHA256 (256 bits = 32 bytes)
            if (Encoding.UTF8.GetByteCount(secretKey) < 32)
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey must be at least 32 characters.");

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = jwtSection["Issuer"],
                        ValidAudience            = jwtSection["Audience"],
                        IssuerSigningKey         = new SymmetricSecurityKey(
                                                       Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew                = TimeSpan.Zero   // no grace period on expiry
                    };

                    // Support JWT from HttpOnly cookie (browser sessions) AND
                    // from the Authorization: Bearer header (Postman / API clients).
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            // Use cookie as fallback only when no Bearer header is present
                            if (!ctx.Request.Headers.ContainsKey("Authorization"))
                                ctx.Token = ctx.Request.Cookies["jwt_token"];

                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        // ── CORS ──────────────────────────────────────────────────────────────────
        public static IServiceCollection AddCorsPolicy(
            this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy("ForumCorsPolicy", policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();   // required for cookie transport
                    }
                    else
                    {
                        // No explicit origins configured — lock down completely
                        policy.SetIsOriginAllowed(_ => false);
                    }
                });
            });

            return services;
        }

        // ── FluentValidation ──────────────────────────────────────────────────────
        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            return services;
        }

        // ── AutoMapper ────────────────────────────────────────────────────────────
        public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(AutoMapperProfile));
            return services;
        }
    }
}
