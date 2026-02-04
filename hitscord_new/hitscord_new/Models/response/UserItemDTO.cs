using hitscord.Models.db;
using hitscord.Models.other;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hitscord.Models.response;

public class UserItemDTO
{
	public required Guid Id { get; set; }
	public required string Mail { get; set; }
	public required string AccountName { get; set; }
	public required string AccountTag { get; set; }
	public required int AccountNumber { get; set; }
	public DateTime AccountCreateDate { get; set; }
	public List<ServersShortListItemDTO>? WhereCreator { get; set; }
	public required bool Notifiable { get; set; }
	public required bool FriendshipApplication { get; set; }
	public required bool NonFriendMessage { get; set; }
	public FileMetaResponseDTO? Icon { get; set; }
	public required List<SystemRoleShortItemDTO> SystemRoles { get; set; }
}