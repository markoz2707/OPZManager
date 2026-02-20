using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
            var user = await _authService.RegisterAsync(
                request.Username,
                request.Email,
                request.Password,
                request.Role ?? "User"
            );

            if (user == null)
            {
                return BadRequest(new { message = "Użytkownik o podanej nazwie lub adresie email już istnieje." });
            }

            return Ok(new
            {
                message = "Użytkownik został zarejestrowany pomyślnie.",
                user = _mapper.Map<UserDto>(user)
            });
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
}
