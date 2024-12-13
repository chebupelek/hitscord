using Authzed.Api.V0;
using Grpc.Core;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Channels;

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

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
            if(role == null) 
            {
                throw new CustomException("Admin role not found", "Create server", "Role", 404);
            }

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
                RoleId = role.Id,
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
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Subscribe", "Server id", 404);
            }

            var existingSubscription = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == serverId);
            if (existingSubscription != null)
            {
                throw new CustomException("User is already subscribed to this server", "Subscribe", "User", 400);
            }

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Name == "Uncertain");
            if (role == null)
            {
                throw new CustomException("Uncertain role not found", "Subscribe", "Role", 404);
            }

            var newSub = new UserServerDbModel
            {
                UserId = user.Id,
                ServerId = serverId,
                RoleId = role.Id,
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
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Unsubscribe", "Server id", 404);
            }

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

    public async Task DeleteServerAsync(Guid serverId, string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Delete server", "Server id", 404);
            }
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
            await _authService.CheckUserAuthAsync(token);

            var owner = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if(server == null)
            {
                throw new CustomException("Server not found", "Change role", "Server id", 404);
            }

            var adminRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                throw new CustomException("Admin role not found", "Change role", "Role", 404);
            }

            var ownerSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == owner.Id && us.ServerId == server.Id && us.RoleId == adminRole.Id);
            if(ownerSub == null)
            {
                throw new CustomException("Owner not admin of this server", "Change role", "Owner", 401);
            }

            await _authService.GetUserByIdAsync(userId);

            var userSub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.UserId == userId && s.ServerId == serverId);
            if (userSub == null)
            {
                throw new CustomException("User not subscriber of this server", "Change role", "User", 400);
            }

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                throw new CustomException("Role not found", "Change role", "Role id", 404);
            }

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
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Get server info", "Server id", 404);
            }

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

    public async Task CreateRolesAsync()
    {
        try
        {
            var roles = await _hitsContext.Role.ToListAsync();
            if(roles != null && roles.Count > 0) 
            {
                throw new CustomException("Roles already created", "Create roles", "Roles", 400);
            }
            var adminRole = new RoleDbModel
            {
                Name = "Admin",
            };
            var teacherRole = new RoleDbModel
            {
                Name = "Teacher",
            };
            var studentRole = new RoleDbModel
            {
                Name = "Student",
            };
            var uncertainRole = new RoleDbModel
            {
                Name = "Uncertain",
            };
            _hitsContext.Role.Add(adminRole);
            await _hitsContext.SaveChangesAsync();
            _hitsContext.Role.Add(teacherRole);
            await _hitsContext.SaveChangesAsync();
            _hitsContext.Role.Add(studentRole);
            await _hitsContext.SaveChangesAsync();
            _hitsContext.Role.Add(uncertainRole);
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

    public async Task<List<RoleDbModel>> GetRolesAsync()
    {
        try
        {
            return (await _hitsContext.Role.ToListAsync());
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