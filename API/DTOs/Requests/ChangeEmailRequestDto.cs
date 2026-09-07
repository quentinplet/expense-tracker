using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class ChangeEmailRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(256, ErrorMessage = "The New Email must be 256 characters or fewer")]
    public string NewEmail { get; set; } = "";

    [Required]
    [StringLength(100, ErrorMessage = "The Current Password must be 100 characters or fewer")]
    public string CurrentPassword { get; set; } = "";
}
