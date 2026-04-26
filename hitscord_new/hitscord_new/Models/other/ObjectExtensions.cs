namespace hitscord.Models.other;
using System.Text.Json;

public static class ObjectExtensions
{
	public static Dictionary<string, string> ToDictionary(this object obj)
	{
		var json = JsonSerializer.Serialize(obj);
		return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
			   ?? new Dictionary<string, string>();
	}
}