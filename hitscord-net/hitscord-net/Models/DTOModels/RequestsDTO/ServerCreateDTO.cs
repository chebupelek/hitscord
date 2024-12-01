using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class ServerCreateDTO
{
    [Required(ErrorMessage = "Name is required.")]
    [MinLength(1, ErrorMessage = "Name must have at least 1 character.")]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
    public required string Name { get; set; }
}