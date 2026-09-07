using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class DeleteAccountRequestDto
{
    [Required]
    [StringLength(100, ErrorMessage = "The Current Password must be 100 characters or fewer")]
    public string CurrentPassword { get; set; } = "";
}
