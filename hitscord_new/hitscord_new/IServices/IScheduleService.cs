using hitscord.Models.db;
using hitscord.Models.inTime;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IScheduleService
{
	Task<ProfessorsListResponseDTO> GetProfessorsAsync();
	Task<FacultyListResponseDTO> GetFacultiesAsync();
	Task<GroupListResponseDTO> GetGroupsAsync(Guid FacultyId);
	Task<BuildingDetailsListResponseDTO> GetBuildingsAsync();
	Task<AudienceListResponseDTO> GetAudiencesAsync(Guid BuildingId);
	Task<ScheduleGrid> GetScheduleAsync(ScheduleType Type, Guid Id, string dateFrom, string dateTo);
	Task<ScheduleGrid> GetScheduleOnChannelAsync(Guid UserId, ScheduleType Type, Guid Id, string dateFrom, string dateTo, Guid pairVoiceChannelId);
	Task<ScheduleGrid> GetScheduleOnServerAsync(Guid UserId, ScheduleType Type, Guid Id, string dateFrom, string dateTo, Guid serverId);
	Task<ScheduleGrid> GetScheduleForUserAsync(Guid UserId, ScheduleType Type, Guid Id, string dateFrom, string dateTo);
	Task CreatePairAsync(Guid UserId, Guid scheduleId, Guid pairVoiceChannelId, List<Guid> roleIds, string? note, ScheduleType Type, Guid Id, string date);
	Task UpdatePairAsync(Guid UserId, Guid pairId, List<Guid> roleIds, string? note);
	Task DeletePairAsync(Guid UserId, Guid pairId);
	Task<AttendanceListDTO> GetAttendanceAsync(Guid UserId, Guid pairId);
}