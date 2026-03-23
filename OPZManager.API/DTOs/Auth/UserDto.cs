namespace OPZManager.API.DTOs.Auth
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Company { get; set; }
        public string? Position { get; set; }
        public string? Phone { get; set; }
        public string? NIP { get; set; }
        public string? Address { get; set; }
        public bool MarketingConsent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Company { get; set; }
        public string? Position { get; set; }
        public string? Phone { get; set; }
        public string? NIP { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
    }
}
