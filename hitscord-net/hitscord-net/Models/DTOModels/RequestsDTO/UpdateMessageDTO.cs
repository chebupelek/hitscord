using hitscord_net.Models.DBModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hitscord_net.Models.DTOModels.RequestsDTO;

public class UpdateMessageDTO
{
    public required Guid MessageId { get; set; }
    [Required]
    [MinLength(1)]
    [MaxLength(5000)]
    public required string Text { get; set; }

    public List<Guid>? Roles { get; set; }
    public List<string>? Tags { get; set; }
}
