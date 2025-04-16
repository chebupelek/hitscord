namespace Message.Models.Response;

public class UsersResponse
{
    public required Guid AuthorId { get; set; }
    public required List<Guid> NotificatedUsers { get; set; }
}
