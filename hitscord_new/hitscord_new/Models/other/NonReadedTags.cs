namespace hitscord.Models.other;

public class NonReadedTags
{
	public required List<Guid> TaggedUsers { get; set; }
	public required List<Guid> TaggedRoles { get; set; }
}
