using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using hitscord.Models.inTime;

namespace hitscord.Models.response;

public class InvitationDataResponseDTO
{
	public List<InvitationResponseDTO>? InvitationList { get; set; }
	public List<UserInvitationDTO>? UserInvitationList { get; set; }
}
