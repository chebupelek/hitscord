using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using hitscord.Models.inTime;

namespace hitscord.Models.response;

public class BuildingDetailsListResponseDTO
{
	public required List<BuildingDetails>? BuildingDetails { get; set; }
}
