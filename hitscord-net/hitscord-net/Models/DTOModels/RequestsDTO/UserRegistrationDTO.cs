using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class UserRegistrationDTO
{
    [Required(ErrorMessage = "Mail address is required.")]
    [EmailAddress(ErrorMessage = "Invalid mail address format.")]
    [MinLength(1, ErrorMessage = "Mail address must have at least 1 character.")]
    [MaxLength(50, ErrorMessage = "Mail address cannot exceed 50 characters.")]
    public required string Mail { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must have at least 6 characters.")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Account name is required.")]
    [MinLength(1, ErrorMessage = "Account name must have at least 1 character.")]
    [MaxLength(50, ErrorMessage = "Account name cannot exceed 50 characters.")]
    public required string AccountName { get; set; }
}