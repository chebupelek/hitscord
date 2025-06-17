using hitscord.Models.db;
using hitscord.Models.inTime;
using hitscord.Models.other;
using hitscord.Models.response;

namespace hitscord.IServices;

public interface IScheduleService
{
	Task<List<Professor>> GetProfessorsAsync();
	Task<List<FacultyDetails>> GetFacultiesAsync();
	Task<List<Group>> GetGroupsAsync(Guid FacultyId);
	Task<List<BuildingDetails>> GetBuildingsAsync();
	Task<List<Audience>> GetAudiencesAsync(Guid BuildingId);
	Task<ScheduleGrid> GetScheduleAsync(ScheduleType Type, Guid Id, string dateFrom, string dateTo);
	Task<ScheduleGrid> GetScheduleOnChannelAsync(string token, ScheduleType Type, Guid Id, string dateFrom, string dateTo, Guid pairVoiceChannelId);
	Task<ScheduleGrid> GetScheduleOnServerAsync(string token, ScheduleType Type, Guid Id, string dateFrom, string dateTo, Guid serverId);
	Task<ScheduleGrid> GetScheduleForUserAsync(string token, ScheduleType Type, Guid Id, string dateFrom, string dateTo);
	Task CreatePairAsync(string token, Guid scheduleId, Guid pairVoiceChannelId, List<Guid> roleIds, string? note, ScheduleType Type, Guid Id, string date);
	Task UpdatePairAsync(string token, Guid pairId, List<Guid> roleIds, string? note);
	Task DeletePairAsync(string token, Guid pairId);
	Task<AttendanceListDTO> GetAttendanceAsync(string token, Guid pairId);
}