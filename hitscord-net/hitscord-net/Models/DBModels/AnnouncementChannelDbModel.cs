namespace hitscord_net.Models.DBModels;

public class AnnouncementChannelDbModel : ChannelDbModel
{
    public AnnouncementChannelDbModel()
    {
        RolesToNotify = new List<RoleDbModel>();
    }

    public ICollection<RoleDbModel> RolesToNotify { get; set; }
}
