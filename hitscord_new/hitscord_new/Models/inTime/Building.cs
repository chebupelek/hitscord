namespace hitscord.Models.inTime;

public class Building
{
    public required Guid id { get; set; }
    public required string name { get; set; }
	public string? address { get; set; }
	public double? latitude { get; set; }
	public double? longitude { get; set; }
}
