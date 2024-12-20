using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;

namespace hitscord_net.IServices;

public interface IMessageService
{
    Task CreateNormalMessageAsync(Guid channelId, string token, string text, List<Guid>? roles, List<string>? tags);
    Task UpdateNormalMessageAsync(Guid messageId, string token, string text, List<Guid>? roles, List<string>? tags);
    Task DeleteNormalMessageAsync(Guid messageId, string token);
}
