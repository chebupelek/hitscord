namespace hitscord.Models.other;

public enum ChangeRoleTypeEnum
{
	// Канальные права: изменяются для пары «роль — канал» через ChannelRoleDTO.
	CanSee,
	CanJoin,
	CanWrite,
	CanWriteSub,
	CanUse,
	Notificated,
	CanCreateTask,
	CanJoinQueue,
	CanTakeQueue
}
