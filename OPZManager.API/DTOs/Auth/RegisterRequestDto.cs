using System.ComponentModel.DataAnnotations;

namespace OPZManager.API.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć od 3 do 100 znaków.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Imię i nazwisko jest wymagane.")]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwa firmy jest wymagana.")]
        [StringLength(200)]
        public string Company { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Position { get; set; }

        [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
        [Phone(ErrorMessage = "Nieprawidłowy format numeru telefonu.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(20)]
        public string? NIP { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Zgoda na przetwarzanie danych jest wymagana.")]
        public bool MarketingConsent { get; set; }

        public string? Role { get; set; }
    }
}
