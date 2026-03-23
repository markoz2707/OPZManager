using OPZManager.API.DTOs.Auth;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IAuthService
    {
        Task<string?> AuthenticateAsync(string username, string password);
        Task<User?> RegisterAsync(string username, string email, string password, string role = "User");
        Task<User?> RegisterWithContactsAsync(RegisterRequestDto request);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> UpdateProfileAsync(int userId, UpdateProfileDto profile);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> ValidateTokenAsync(string token);
        Task LogoutAsync(string token);
        string GenerateJwtToken(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(int userId, string role);
    }
}
