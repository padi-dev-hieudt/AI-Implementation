using ForumWebsite.Models.DTOs.User;

namespace ForumWebsite.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto>  RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto>  LoginAsync(LoginDto loginDto);
        Task<UserProfileDto>   GetProfileAsync(int userId);
    }
}
