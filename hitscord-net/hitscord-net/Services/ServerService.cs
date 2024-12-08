using Authzed.Api.V0;
using Grpc.Core;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
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
    private readonly IAuthService _authService;

    public ServerService(HitsContext hitsContext, IChannelService channelService, IAuthService authService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
    }

    public async Task CreateServerAsync(string token, string severName)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var newServer = new ServerDbModel()
            {
                Name = severName,
                Admin = user
            };
            await _hitsContext.Server.AddAsync(newServer);
            _hitsContext.SaveChanges();

            await _channelService.CreateChannelAsync(newServer.Id, token, "основной", ChannelTypeEnum.Text);
            await _channelService.CreateChannelAsync(newServer.Id, token, "основной", ChannelTypeEnum.Voice);

            var newSub = new UserServerDbModel
            {
                UserId = (Guid)user.Id,
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
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var newSub = new UserServerDbModel
            {
                UserId = (Guid)user.Id,
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

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            return new ServersListDTO
            {
                ServersList = await _hitsContext.UserServer
                    .Where(us => us.UserId == user.Id)
                    .Join(_hitsContext.Server,
                        us => us.ServerId,
                        s => s.Id,
                        (us, s) => new ServersListItemDTO
                        {
                            ServerId = s.Id,
                            ServerName = s.Name
                        })
                    .ToListAsync()
            };
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
            await _authService.CheckUserAuthAsync(token);

            var owner = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if(server == null)
            {
                throw new CustomException("Server not found", "Change role", "Server id", 404);
            }
            if(server.Admin != owner)
            {
                throw new CustomException("Owner not admin of this server", "Change role", "Owner", 401);
            }

            await _authService.GetUserByIdAsync(userId);

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.UserId == userId);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Change role", "User", 400);
            }

            sub.Role = role;
            _hitsContext.UserServer.Update(sub);
            await _hitsContext.SaveChangesAsync();
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Get server info", "Server id", 404);
            }

            var userServer = await _hitsContext.UserServer.FirstOrDefaultAsync(us => (us.UserId == user.Id && us.ServerId == serverId));
            if (userServer == null)
            {
                throw new CustomException("User not subscriber of this server", "Get server info", "User", 401);
            }

            return new ServerInfoDTO
            {
                ServerId = serverId,
                ServerName = server.Name,
                UserRole = userServer.Role,
                Users = await _hitsContext.UserServer
                    .Where(us => us.ServerId == serverId)
                    .Join(_hitsContext.User,
                          us => us.UserId,
                          u => u.Id,
                          (us, u) => new ServerUserDTO
                          {
                              UserId = (Guid)u.Id,
                              UserName = u.AccountName,
                              UserTag = u.AccountTag
                          })
                    .ToListAsync(),
                Channels = await _channelService.GetChannelListAsync(serverId, token)
            };
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}