namespace hitscord.Redis.CashedDB.Models;

[Flags]
public enum ServerRights
{
	// Битовая маска серверных прав; используется кешем для быстрых проверок в сервисах.
	None = 0,
	ChangeRole = 1 << 0,
	WorkChannels = 1 << 1,
	DeleteUsers = 1 << 2,
	MuteOther = 1 << 3,
	DeleteOthersMessages = 1 << 4,
	IgnoreMaxCount = 1 << 5,
	CreateRoles = 1 << 6,
	CreateLessons = 1 << 7,
	CheckAttendance = 1 << 8,
	UseInvitations = 1 << 9
}
