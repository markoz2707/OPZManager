using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public interface IAuthService
    {
        Task<string?> AuthenticateAsync(string username, string password);
        Task<User?> RegisterAsync(string username, string email, string password, string role = "User");
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> ValidateTokenAsync(string token);
        Task LogoutAsync(string token);
        string GenerateJwtToken(User user);
    }
}
