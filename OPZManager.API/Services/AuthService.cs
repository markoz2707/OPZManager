using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using OPZManager.API.Data;
using OPZManager.API.DTOs.Auth;
using OPZManager.API.Models;

namespace OPZManager.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);

            var session = new UserSession
            {
                UserId = user.Id,
                JwtToken = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return token;
        }

        public async Task<User?> RegisterAsync(string username, string email, string password, string role = "User")
        {
            if (await _context.Users.AnyAsync(u => u.Username == username || u.Email == email))
                return null;

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> RegisterWithContactsAsync(RegisterRequestDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
                return null;

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role ?? "User",
                FullName = request.FullName,
                Company = request.Company,
                Position = request.Position,
                Phone = request.Phone,
                NIP = request.NIP,
                Address = request.Address,
                MarketingConsent = request.MarketingConsent,
                MarketingConsentDate = request.MarketingConsent ? DateTime.UtcNow : null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> UpdateProfileAsync(int userId, UpdateProfileDto profile)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (profile.Email != null)
            {
                var emailTaken = await _context.Users.AnyAsync(u => u.Email == profile.Email && u.Id != userId);
                if (emailTaken) return null;
                user.Email = profile.Email;
            }

            if (profile.FullName != null) user.FullName = profile.FullName;
            if (profile.Company != null) user.Company = profile.Company;
            if (profile.Position != null) user.Position = profile.Position;
            if (profile.Phone != null) user.Phone = profile.Phone;
            if (profile.NIP != null) user.NIP = profile.NIP;
            if (profile.Address != null) user.Address = profile.Address;

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserSessions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string role)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.JwtToken == token && s.ExpiresAt > DateTime.UtcNow);

            return session != null;
        }

        public async Task LogoutAsync(string token)
        {
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.JwtToken == token);
            if (session != null)
            {
                _context.UserSessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
