using hitscord_net.Models.DBModels;

namespace hitscord_net.Models.DTOModels.ResponseDTO;

public class ProfileDTO
{
    public string Name { get; set; }
    public string Tag { get; set; }
    public string Mail { get; set; }
    public DateOnly AccontCreateDate { get; set; }
    public ProfileDTO(UserDbModel user)
    {
        Name = user.AccountName;
        Tag = user.AccountTag;
        Mail = user.Mail;
        AccontCreateDate = DateOnly.FromDateTime((DateTime)user.AccountCreateDate);
    }
}