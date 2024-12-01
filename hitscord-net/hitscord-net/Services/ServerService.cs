using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace hitscord_net.Services;

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
    private readonly IChannelService _channelService;

    public ServerService(HitsContext hitsContext, IChannelService channelService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
    }

    public async Task CreateServerAsync(string token, string severName)
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
            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);

            var newServer = new ServerDbModel()
            {
                Name = severName,
                Admin = user
            };
            await _hitsContext.Server.AddAsync(newServer);
            _hitsContext.SaveChanges();
            var channel = await _channelService.CreateChannelAsync(newServer.Id, token, "основной");
            newServer.Channels.Add(channel);
            _hitsContext.Server.Update(newServer);

            var newSub = new UserServerDbModel
            {
                UserId = userIdGuid,
                ServerId = newServer.Id,
                Role = RoleEnum.Admin,
            };
            await _hitsContext.UserServer.AddAsync(newSub);

            _hitsContext.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task SubscribeAsync(Guid serverId, string token)
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

            var newSub = new UserServerDbModel
            {
                UserId = userIdGuid,
                ServerId = serverId,
                Role = RoleEnum.Uncertain,
            };
            await _hitsContext.UserServer.AddAsync(newSub);
            _hitsContext.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, RoleEnum role)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var ownerId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (ownerId == null)
            {
                throw new KeyNotFoundException("User не найден");
            }
            Guid ownerIdGuid = Guid.Parse(ownerId);
            var owner = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == ownerIdGuid);
            if (owner == null)
            {
                throw new ArgumentException("owner not found");
            }

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if(server == null)
            {
                throw new ArgumentException("server not found");
            }
            if(server.Admin != owner)
            {
                throw new ArgumentException("owner not admin");
            }

            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                throw new ArgumentException("user not admin");
            }

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.UserId == userId);
            if (sub != null)
            {
                throw new ArgumentException("sub not admin");
            }

            sub.Role = role;
            _hitsContext.UserServer.Update(sub);
            await _hitsContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}