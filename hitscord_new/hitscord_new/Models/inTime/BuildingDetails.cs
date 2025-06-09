namespace hitscord.Models.inTime;

public class BuildingDetails
{
    public required Guid id { get; set; }
    public required string name { get; set; }
	public string? address { get; set; }
	public double? latitude { get; set; }
	public double? longitude { get; set; }
	public required string image { get; set; }
}
