using Authzed.Api.V0;
using Grpc.Core;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace hitscord_net.Services;

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
    private readonly IChannelService _channelService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IRoleService _roleService;

    public ServerService(HitsContext hitsContext, IChannelService channelService, IAuthorizationService authorizationService, IRoleService roleService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
    }

    public async Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels)
    {
        try
        {
            var server = includeChannels ? await _hitsContext.Server.Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId) :
            await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Check server for existing", "Server id", 404);
            }
            return server;
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

    public async Task<ServerDbModel> GetServerFullModelAsync(Guid serverId)
    {
        try
        {
            var server = await _hitsContext.Server
                .Include(s => s.Channels)
                    .ThenInclude(c => c.RolesCanView)
                .Include(s => s.Channels)
                    .ThenInclude(c => c.RolesCanWrite)
                .FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Get server with full model", "Server id", 404);
            }
            return server;
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

    public async Task CreateServerAsync(string token, string severName)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

            var adminRole = await _roleService.CheckRoleExistAsync("Admin");

            var newServer = new ServerDbModel()
            {
                Name = severName,
                CreatorId = user.Id
            };
            await _hitsContext.Server.AddAsync(newServer);
            _hitsContext.SaveChanges();

            var newSub = new UserServerDbModel
            {
                UserId = user.Id,
                ServerId = newServer.Id,
                RoleId = adminRole.Id,
                UserServerName = user.AccountName
            };
            await _hitsContext.UserServer.AddAsync(newSub);
            _hitsContext.SaveChanges();

            await _channelService.CreateChannelAsync(newServer.Id, token, "Основной", ChannelTypeEnum.Text);
            await _channelService.CreateChannelAsync(newServer.Id, token, "Основной", ChannelTypeEnum.Voice);
            await _channelService.CreateChannelAsync(newServer.Id, token, "Основной", ChannelTypeEnum.Announcement);

            _hitsContext.SaveChanges();
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

    public async Task SubscribeAsync(Guid serverId, string token, string? userName)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

            var server = await CheckServerExistAsync(serverId, false);

            var existingSubscription = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == serverId);
            if (existingSubscription != null)
            {
                throw new CustomException("User is already subscribed to this server", "Subscribe", "User", 400);
            }

            var uncertainRole = await _roleService.CheckRoleExistAsync("Uncertain");

            var newSub = new UserServerDbModel
            {
                UserId = user.Id,
                ServerId = serverId,
                RoleId = uncertainRole.Id,
                UserServerName = userName != null ? userName : user.AccountName
            };

            await _hitsContext.UserServer.AddAsync(newSub);
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

    public async Task UnsubscribeAsync(Guid serverId, string token)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

            var server = await CheckServerExistAsync(serverId, false);

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == serverId);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Unsubscribe", "User", 400);
            }

            if (server.CreatorId == user.Id)
            {
                throw new CustomException("User is the creator of this server", "Unsubscribe", "User", 400);
            }

            var voiceChannel = await _hitsContext.Channel.Include(c => ((VoiceChannelDbModel)c).Users).FirstOrDefaultAsync(c => c.ServerId == serverId && c is VoiceChannelDbModel && ((VoiceChannelDbModel)c).Users.Contains(user));
            if(voiceChannel != null)
            {
                ((VoiceChannelDbModel)voiceChannel).Users.Remove(user);
            }

            _hitsContext.UserServer.Remove(sub);
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

    public async Task UnsubscribeForCreatorAsync(Guid serverId, string token, Guid newCreatorId)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

            var server = await CheckServerExistAsync(serverId, false);

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == serverId);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Unsubscribe", "User", 400);
            }
            var newCreator = await _authorizationService.GetUserByIdAsync(newCreatorId);
            var newCreatorSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == newCreator.Id && us.ServerId == serverId);
            if (newCreatorSub == null)
            {
                throw new CustomException("User for new creator not subscriber of this server", "Unsubscribe", "User for new creator", 400);
            }

            var adminRole = await _roleService.CheckRoleExistAsync("Admin");

            var voiceChannel = await _hitsContext.Channel.Include(c => ((VoiceChannelDbModel)c).Users).FirstOrDefaultAsync(c => c.ServerId == serverId && c is VoiceChannelDbModel && ((VoiceChannelDbModel)c).Users.Contains(user));
            if (voiceChannel != null)
            {
                ((VoiceChannelDbModel)voiceChannel).Users.Remove(user);
            }

            _hitsContext.UserServer.Remove(sub);
            server.CreatorId = newCreator.Id;
            _hitsContext.Server.Update(server);
            newCreatorSub.RoleId = adminRole.Id;
            _hitsContext.UserServer.Update(newCreatorSub);
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

    public async Task DeleteServerAsync(Guid serverId, string token)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

            var server = await CheckServerExistAsync(serverId, true);
            if (server.CreatorId != user.Id)
            {
                throw new CustomException("User is not the creator of this server", "Delete server", "User", 401);
            }

            var userServerRelations = _hitsContext.UserServer.Where(us => us.ServerId == server.Id);
            _hitsContext.UserServer.RemoveRange(userServerRelations);

            var voiceChannels = server.Channels.OfType<VoiceChannelDbModel>().ToList();
            foreach (var voiceChannel in voiceChannels)
            {
                voiceChannel.Users.Clear();
            }

            var channelsToDelete = server.Channels.ToList();
            _hitsContext.Channel.RemoveRange(channelsToDelete);

            _hitsContext.Server.Remove(server);

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

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

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
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, Guid roleId)
    {
        try
        {
            await _authorizationService.CheckUserAuthAsync(token);

            var owner = await _authorizationService.GetUserByTokenAsync(token);

            var server = await CheckServerExistAsync(serverId, false);

            var adminRole = await _roleService.CheckRoleExistAsync("Admin");

            var ownerSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == owner.Id && us.ServerId == server.Id && us.RoleId == adminRole.Id);
            if(ownerSub == null)
            {
                throw new CustomException("Owner not admin of this server", "Change role", "Owner", 401);
            }

            await _authorizationService.GetUserByIdAsync(userId);

            var userSub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.UserId == userId && s.ServerId == serverId && s.UserId != owner.Id);
            if (userSub == null)
            {
                throw new CustomException("User not subscriber of this server", "Change role", "User", 400);
            }

            var role = await _roleService.CheckRoleExistByIdAsync(roleId);

            userSub.RoleId = role.Id;
            _hitsContext.UserServer.Update(userSub);
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
            await _authorizationService.CheckUserAuthAsync(token);

            var user = await _authorizationService.GetUserByTokenAsync(token);

            var server = await CheckServerExistAsync(serverId, false);

            var userServer = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => (us.UserId == user.Id && us.ServerId == serverId));
            if (userServer == null)
            {
                throw new CustomException("User not subscriber of this server", "Get server info", "User", 401);
            }

            return new ServerInfoDTO
            {
                ServerId = serverId,
                ServerName = server.Name,
                Roles = await _hitsContext.Role.ToListAsync(),
                UserRoleId = userServer.RoleId,
                UserRole = userServer.Role.Name,
                Users = await _hitsContext.UserServer
                    .Where(us => us.ServerId == serverId)
                    .Join(_hitsContext.User,
                          us => us.UserId,
                          u => u.Id,
                          (us, u) => new ServerUserDTO
                          {
                              UserId = u.Id,
                              UserName = u.AccountName,
                              UserTag = u.AccountTag,
                              RoleName = us.Role.Name
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