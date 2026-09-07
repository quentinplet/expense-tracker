using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class RegisterRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The First Name must be between 2 and 100 characters")]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The Last Name must be between 2 and 100 characters")]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(256, ErrorMessage = "The Email must be 256 characters or fewer")]
    public string Email { get; set; } = "";

    // Minimum aligné sur PasswordOptions.RequiredLength (défaut Identity, non
    // reconfiguré dans Program.cs) — le vrai contrôle reste UserManager.CreateAsync,
    // ceci évite juste un aller-retour serveur pour un mot de passe trivialement court.
    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The Password must be between 6 and 100 characters")]
    public string Password { get; set; } = "";
}
