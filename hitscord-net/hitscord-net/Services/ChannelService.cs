using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace hitscord_net.Services;

public class ChannelService : IChannelService
{
    private readonly HitsContext _hitsContext;

    public ChannelService(HitsContext hitsContext, ITokenService tokenService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
    }

    public async Task<ChannelDbModel> CreateChannelAsync(Guid serverId, string token, string name)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new KeyNotFoundException("User не найден");
            }
            Guid userIdGuid = Guid.Parse(userId);

            var owner = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (owner == null)
            {
                throw new ArgumentException("owner not found");
            }

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new ArgumentException("server not found");
            }
            if (server.Admin != owner)
            {
                throw new ArgumentException("owner not admin");
            }

            var newChannel = new ChannelDbModel()
            {
                Name = name,
                CanRead = new List<RoleEnum>() { RoleEnum.Admin, RoleEnum.Teacher }
            };

            await _hitsContext.Channel.AddAsync(newChannel);
            await _hitsContext.SaveChangesAsync();

            return newChannel;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<ChannelListDTO> GetChannelListAsync(Guid serverId, string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new KeyNotFoundException("User не найден");
            }
            Guid userIdGuid = Guid.Parse(userId);

            var owner = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (owner == null)
            {
                throw new ArgumentException("owner not found");
            }

            var server = await _hitsContext.Server.Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new ArgumentException("server not found");
            }
            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.ServerId == serverId && s.UserId == owner.Id);
            if (sub == null)
            {
                throw new ArgumentException("owner not subscriber");
            }

            var channelsList = new ChannelListDTO
            {
                Channels = server.Channels.Where(c => c.CanRead.Contains(sub.Role) || server.Admin == owner)
                .Select(c => new ChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                })
                .ToList()
            };

            return(channelsList);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}