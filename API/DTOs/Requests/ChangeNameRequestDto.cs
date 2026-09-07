using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class ChangeNameRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The First Name must be between 2 and 100 characters")]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The Last Name must be between 2 and 100 characters")]
    public string LastName { get; set; } = "";
}
