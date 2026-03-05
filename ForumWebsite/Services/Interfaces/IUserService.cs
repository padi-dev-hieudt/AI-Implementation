using ForumWebsite.Models.Common;
using ForumWebsite.Models.DTOs.User;

namespace ForumWebsite.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto>  RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto>  LoginAsync(LoginDto loginDto);
        Task<UserProfileDto>   GetProfileAsync(int userId);

        /// <summary>Returns a paged list of all users for the admin panel.</summary>
        Task<PagedResult<AdminUserDto>> GetAllUsersAsync(int page, int pageSize);

        /// <summary>
        /// Toggles IsActive for <paramref name="targetUserId"/>.
        /// Throws <see cref="BusinessRuleException"/> if an admin tries to deactivate themselves.
        /// </summary>
        Task ToggleUserActiveAsync(int targetUserId, int requestingUserId);
    }
}
