using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class ChangePasswordRequestDto
{
    [Required]
    [StringLength(100, ErrorMessage = "The Current Password must be 100 characters or fewer")]
    public string CurrentPassword { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The New Password must be between 6 and 100 characters")]
    public string NewPassword { get; set; } = "";
}
