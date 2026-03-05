using ForumWebsite.Data.Repositories.Interfaces;
using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.User;
using ForumWebsite.Models.Entities;
using ForumWebsite.Services.Interfaces;

namespace ForumWebsite.Services.Implementations
{
    /// <summary>
    /// Handles user registration, login, and profile retrieval.
    ///
    /// SECURITY NOTES
    /// ──────────────
    /// • Timing-attack mitigation: BCrypt.Verify is ALWAYS called, even when the
    ///   user is not found. A dummy hash is used as the comparand so that response
    ///   time is constant whether or not the email exists. This prevents
    ///   timing-based email enumeration (OWASP A07).
    ///
    /// • AuthenticationException (→ 401) is thrown for credential failures.
    ///   401 = "tell me who you are"; 403 = "I know who you are, you cannot do this".
    ///
    /// • Passwords are hashed with BCrypt work-factor 12 (≈250 ms on modern HW).
    ///   Never store or log plaintext passwords.
    /// </summary>
    public class UserService : IUserService
    {
        // Dummy BCrypt hash used for constant-time comparison when user is not found.
        // Pre-computed ONCE at startup — BCrypt is intentionally slow so we must NOT
        // compute a fresh hash on every request with an unknown email address.
        private static readonly string _dummyHash =
            BCrypt.Net.BCrypt.HashPassword("dummy_timing_safety_value", workFactor: 12);

        private readonly IUserRepository _userRepository;
        private readonly IJwtService     _jwtService;

        public UserService(IUserRepository userRepository, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _jwtService     = jwtService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // BusinessRuleException → 400 Bad Request (domain rejection, not server error)
            if (await _userRepository.EmailExistsAsync(dto.Email))
                throw new BusinessRuleException("Email is already registered.");

            if (await _userRepository.UsernameExistsAsync(dto.Username))
                throw new BusinessRuleException("Username is already taken.");

            var user = new User
            {
                Username     = dto.Username.Trim(),
                Email        = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                Role         = UserRoles.User,
                CreatedAt    = DateTime.UtcNow,
                IsActive     = true
            };

            await _userRepository.CreateAsync(user);
            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email.ToLower().Trim());

            // ── Timing-attack mitigation ─────────────────────────────────────────
            // ALWAYS call BCrypt.Verify regardless of whether the user was found.
            // Without this, a null-user branch would be ~10× faster than a BCrypt
            // comparison, letting an attacker enumerate valid emails via timing.
            var hashToVerify  = user?.PasswordHash ?? _dummyHash;
            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, hashToVerify);
            // ────────────────────────────────────────────────────────────────────

            if (user == null || !passwordValid)
                throw new AuthenticationException("Invalid email or password."); // → 401

            if (!user.IsActive)
                throw new ForbiddenException("This account has been disabled."); // → 403

            return BuildAuthResponse(user);
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User {userId} not found.");

            // Dedicated count queries — no longer loads all posts/comments into
            // memory just to .Count() them (was an N+1 / memory waste).
            var (postCount, commentCount) =
                await _userRepository.GetActivityCountsAsync(userId);

            return new UserProfileDto
            {
                Id           = user.Id,
                Username     = user.Username,
                Email        = user.Email,
                Role         = user.Role,
                CreatedAt    = user.CreatedAt,
                PostCount    = postCount,
                CommentCount = commentCount
            };
        }

        public async Task<PagedResult<AdminUserDto>> GetAllUsersAsync(int page, int pageSize)
        {
            var (users, total) = await _userRepository.GetAllPagedAsync(page, pageSize);

            return new PagedResult<AdminUserDto>
            {
                Items      = users.Select(u => new AdminUserDto
                {
                    Id        = u.Id,
                    Username  = u.Username,
                    Email     = u.Email,
                    Role      = u.Role,
                    IsActive  = u.IsActive,
                    CreatedAt = u.CreatedAt
                }),
                Page       = page,
                PageSize   = pageSize,
                TotalCount = total
            };
        }

        public async Task ToggleUserActiveAsync(int targetUserId, int requestingUserId)
        {
            if (targetUserId == requestingUserId)
                throw new BusinessRuleException("Bạn không thể vô hiệu hoá tài khoản của chính mình.");

            var user = await _userRepository.GetByIdAsync(targetUserId)
                ?? throw new KeyNotFoundException($"User {targetUserId} not found.");

            user.IsActive  = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private AuthResponseDto BuildAuthResponse(User user)
            => new()
            {
                Token     = _jwtService.GenerateToken(user),
                Username  = user.Username,
                Email     = user.Email,
                Role      = user.Role,
                ExpiresAt = _jwtService.GetTokenExpiry()
            };
    }
}
