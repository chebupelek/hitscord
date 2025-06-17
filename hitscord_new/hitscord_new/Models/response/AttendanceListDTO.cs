using hitscord.Models.db;

namespace hitscord.Models.response;

public class AttendanceListDTO
{
	public required List<PairUserDbModel> Attendance { get; set; }
}