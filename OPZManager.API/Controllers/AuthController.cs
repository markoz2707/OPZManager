using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using OPZManager.API.DTOs.Auth;
using OPZManager.API.Services;

namespace OPZManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public AuthController(IAuthService authService, IMapper mapper)
        {
            _authService = authService;
            _mapper = mapper;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            var token = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (token == null)
            {
                return Unauthorized(new { message = "Nieprawidłowe dane logowania." });
            }

            var user = await _authService.GetUserByUsernameAsync(request.Username);

            return Ok(new LoginResponseDto
            {
                Token = token,
                User = _mapper.Map<UserDto>(user)
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<object>> Register([FromBody] RegisterRequestDto request)
        {
            if (!request.MarketingConsent)
            {
                return BadRequest(new { message = "Zgoda na przetwarzanie danych osobowych jest wymagana." });
            }

            var user = await _authService.RegisterWithContactsAsync(request);

            if (user == null)
            {
                return BadRequest(new { message = "Użytkownik o podanej nazwie lub adresie email już istnieje." });
            }

            // Auto-login after registration
            var token = await _authService.AuthenticateAsync(request.Username, request.Password);

            return Ok(new LoginResponseDto
            {
                Token = token!,
                User = _mapper.Map<UserDto>(user)
            });
        }

        [HttpGet("me")]
        [Authorize]
        [DisableRateLimiting]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null) return NotFound();

            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPut("me")]
        [Authorize]
        [DisableRateLimiting]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto profile)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _authService.UpdateProfileAsync(userId, profile);

            if (user == null)
                return BadRequest(new { message = "Nie udało się zaktualizować profilu. Adres email może być już zajęty." });

            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpDelete("me")]
        [Authorize]
        [DisableRateLimiting]
        public async Task<ActionResult> DeleteAccount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _authService.DeleteUserAsync(userId);

            if (!result) return NotFound();

            return Ok(new { message = "Konto zostało usunięte. Wszystkie Twoje dane zostały skasowane." });
        }

        // Admin: list all users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users.Select(u => _mapper.Map<UserDto>(u)).ToList());
        }

        // Admin: update user role
        [HttpPut("users/{id}/role")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var result = await _authService.UpdateUserRoleAsync(id, dto.Role);
            if (!result) return NotFound();

            return Ok(new { message = "Rola użytkownika została zmieniona." });
        }

        // Admin: delete user
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        [DisableRateLimiting]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var result = await _authService.DeleteUserAsync(id);
            if (!result) return NotFound();

            return Ok(new { message = "Użytkownik został usunięty." });
        }

        [HttpPost("logout")]
        public async Task<ActionResult<object>> Logout()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                await _authService.LogoutAsync(token);
            }

            return Ok(new { message = "Wylogowano pomyślnie." });
        }

        [HttpGet("test")]
        [DisableRateLimiting]
        public IActionResult Test()
        {
            return Ok(new { message = "Auth API is working", timestamp = DateTime.UtcNow });
        }
    }

    public class UpdateRoleDto
    {
        public string Role { get; set; } = "User";
    }
}
