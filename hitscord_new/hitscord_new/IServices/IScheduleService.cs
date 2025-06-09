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
}