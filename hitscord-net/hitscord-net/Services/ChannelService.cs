using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.EntityFrameworkCore;

namespace hitscord_net.Services;

public class ChannelService : IChannelService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IServerService _serverService;

    public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthorizationService authService, IServerService serverService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
    }

    public async Task<ChannelDbModel> CheckChannelExistAsync(Guid channelId, bool fullInfo)
    {
        try
        {
            var channel = fullInfo ? await _hitsContext.Channel.Include(c => c.RolesCanView).Include(c => c.RolesCanWrite).FirstOrDefaultAsync(c => c.Id == channelId) : 
                await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId);
            if (channel == null)
            {
                throw new CustomException("Channel not found", "Check channel for existing", "Channel", 404);
            }
            return channel;
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

    public async Task<ChannelDbModel> CheckVoiceChannelExistAsync(Guid channelId, bool joinedUsers)
    {
        try
        {
            var channel = joinedUsers ? await _hitsContext.Channel.Include(c => ((VoiceChannelDbModel)c).Users).FirstOrDefaultAsync(c => c.Id == channelId && c is VoiceChannelDbModel) :
                await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is VoiceChannelDbModel);
            if (channel == null)
            {
                throw new CustomException("Voice channel not found", "Check voice channel for existing", "Voice channel", 404);
            }
            return channel;
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

    public async Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var owner = await _authService.GetUserByTokenAsync(token);

            var server = await _serverService.CheckServerExistAsync(serverId, false);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == owner.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == owner.Id));
            if (sub == null)
            {
                throw new CustomException("Owner not admin or creator of this server", "Create channel", "Owner", 401);
            }

            ChannelDbModel newChannel;
            switch (channelType)
            {
                case ChannelTypeEnum.Text:
                    newChannel = new TextChannelDbModel
                    {
                        Name = name,
                        ServerId = serverId,
                        IsMessage = false,
                        RolesCanView = await _hitsContext.Role.ToListAsync(),
                        RolesCanWrite = await _hitsContext.Role.ToListAsync()
                    };
                    break;

                case ChannelTypeEnum.Voice:
                    newChannel = new VoiceChannelDbModel
                    {
                        Name = name,
                        ServerId = serverId,
                        RolesCanView = await _hitsContext.Role.ToListAsync(),
                        RolesCanWrite = await _hitsContext.Role.ToListAsync()
                    };
                    break;

                case ChannelTypeEnum.Announcement:
                    newChannel = new AnnouncementChannelDbModel
                    {
                        Name = name,
                        ServerId = serverId,
                        RolesCanView = await _hitsContext.Role.ToListAsync(),
                        RolesCanWrite = await _hitsContext.Role.ToListAsync()
                    };
                    break;

                default:
                    throw new CustomException("Invalid channel type", "Create channel", "Channel type", 400);
            }

            await _hitsContext.Channel.AddAsync(newChannel);
            await _hitsContext.SaveChangesAsync();

            server.Channels.Add(newChannel);
            _hitsContext.Server.Update(server);
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

    public async Task<ChannelListDTO> GetChannelListAsync(Guid serverId, string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var server = await _serverService.GetServerFullModelAsync(serverId);
            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.ServerId == serverId && s.UserId == user.Id);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Get channels list", "User", 404);
            }

            var announcementChannels = await _hitsContext.Channel
                .Include(c => c.RolesCanView)
                .Include(c => c.RolesCanWrite)
                .Include(c => ((AnnouncementChannelDbModel)c).RolesToNotify)
                .Where(c => c.ServerId == serverId && c is AnnouncementChannelDbModel)
                .ToListAsync();

            var channelsList = new ChannelListDTO
            {
                TextChannels = server.Channels
                .Where(c => 
                    (
                        c.RolesCanView.Contains(sub.Role) || 
                        server.CreatorId == user.Id
                    ) && 
                    c is TextChannelDbModel && 
                    ((TextChannelDbModel)c).IsMessage == false)
                .Select(c => new TextChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                    CanWrite = c.RolesCanWrite.Contains(sub.Role) || server.CreatorId == user.Id
                })
                .ToList(),

                VoiceChannels = server.Channels
                .Where(c => 
                    (
                        c.RolesCanView.Contains(sub.Role) || 
                        server.CreatorId == user.Id
                    ) && 
                    c is VoiceChannelDbModel)
                .Select(c => new VoiceChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                    CanJoin = c.RolesCanWrite.Contains(sub.Role) || server.CreatorId == user.Id,
                    Users = ((VoiceChannelDbModel)c).Users
                        .Select(u => new VoiceChannelUserDTO
                        {
                            UserId = u.Id,
                            UserName = u.AccountName
                        })
                        .ToList()
                })
                .ToList(),

                AnnouncementChannels = announcementChannels
                .Where(c => 
                    (
                        c.RolesCanView.Contains(sub.Role) || 
                        server.CreatorId == user.Id
                    ) && 
                    c is AnnouncementChannelDbModel)
                .Select(c => new AnnouncementChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                    CanWrite = c.RolesCanWrite.Contains(sub.Role) || server.CreatorId == user.Id,
                    AnnoucementRoles = ((AnnouncementChannelDbModel)c).RolesToNotify.ToList()
                })
                .ToList()
                /*
                AnnouncementChannels = server.Channels
                .Where(c => (c.RolesCanView.Contains(sub.Role) || server.CreatorId == user.Id) && c is AnnouncementChannelDbModel)
                .Select(c => new AnnouncementChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                    CanWrite = c.RolesCanWrite.Contains(sub.Role) || server.CreatorId == user.Id,
                    AnnoucementRoles = ((AnnouncementChannelDbModel)c).RolesToNotify.ToList()
                })
                .ToList()*/
            };

            return (channelsList);
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

    public async Task<bool> JoinToVoiceChannelAsync(Guid chnnelId, string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckVoiceChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.ServerId == server.Id && s.UserId == user.Id);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Join to voice channel", "User", 401);
            }

            if(channel.RolesCanView.Contains(sub.Role))
            {
                throw new CustomException("Role of user cant see this channel", "Join to voice channel", "User", 401);
            }
            if (channel.RolesCanWrite.Contains(sub.Role))
            {
                throw new CustomException("Role of user cant join to this channel", "Join to voice channel", "User", 401);
            }

            var userthischannel = ((VoiceChannelDbModel)channel).Users.FirstOrDefault(u => u.Id == user.Id);
            if (userthischannel != null)
            {
                throw new CustomException("User is already on this channel", "Join to voice channel", "Voice channel - User", 400);
            }

            var userchannel = await _hitsContext.Channel.Include(c => ((VoiceChannelDbModel)c).Users).FirstOrDefaultAsync(c => c is VoiceChannelDbModel && ((VoiceChannelDbModel)c).Users.Contains(user));
            if (userchannel != null)
            {
                ((VoiceChannelDbModel)userchannel).Users.Remove(user);
                _hitsContext.Channel.Update(userchannel);
                await _hitsContext.SaveChangesAsync();
            }

            ((VoiceChannelDbModel)channel).Users.Add(user);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            return (true);
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

    public async Task<bool> RemoveFromVoiceChannelAsync(Guid chnnelId, string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckVoiceChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.ServerId == server.Id && s.UserId == user.Id);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Remove from voice channel", "User", 400);
            }

            ((VoiceChannelDbModel)channel).Users.Remove(user);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            return (true);
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

    public async Task<bool> DeleteChannelAsync(Guid chnnelId, string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckChannelExistAsync(chnnelId, false);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == user.Id));
            if (sub == null)
            {
                throw new CustomException("User not admin or creator of this server", "Delete channel", "Owner", 401);
            }

            if (channel is VoiceChannelDbModel)
            {
                ((VoiceChannelDbModel)channel).Users.Clear();
                _hitsContext.Channel.Update(channel);
            }
            
            _hitsContext.Channel.Remove(channel);
            await _hitsContext.SaveChangesAsync();

            return (true);
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

    public async Task<ChannelSettingsDTO> GetChannelSettingsAsync(Guid chnnelId, string token)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == user.Id));
            if (sub == null)
            {
                throw new CustomException("User not admin or creator of this server", "Get channel settings", "Owner", 401);
            }

            return new ChannelSettingsDTO { CanRead = channel.RolesCanView.ToList(), CanWrite = channel.RolesCanWrite.ToList() };
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

    public async Task<bool> AddRoleToCanReadSettingAsync(Guid chnnelId, string token, Guid roleId)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == user.Id));
            if (sub == null)
            {
                throw new CustomException("User not admin or creator of this server", "Add new role to Can read settings", "Owner", 401);
            }

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                throw new CustomException("Role not found", "Add new role to Can read settings", "Role", 404);
            }

            if (channel.RolesCanView.Contains(role))
            {
                throw new CustomException("This role already in this setting", "Add new role to Can read settings", "Role", 400);
            }

            channel.RolesCanView.Add(role);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            return true;
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

    public async Task<bool> RemoveRoleFromCanReadSettingAsync(Guid chnnelId, string token, Guid roleId)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == user.Id));
            if (sub == null)
            {
                throw new CustomException("User not admin or creator of this server", "Remove role from Can read settings", "Owner", 401);
            }

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                throw new CustomException("Role not found", "Remove role from Can read settings", "Role", 404);
            }

            if (!channel.RolesCanView.Contains(role))
            {
                throw new CustomException("This role isnt in this setting", "Remove role from Can read settings", "Role", 400);
            }

            channel.RolesCanView.Remove(role);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            return true;
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

    public async Task<bool> AddRoleToCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == user.Id));
            if (sub == null)
            {
                throw new CustomException("User not admin or creator of this server", "Add new role to Can write settings", "Owner", 401);
            }

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                throw new CustomException("Role not found", "Add new role to Can write settings", "Role", 404);
            }

            if (channel.RolesCanWrite.Contains(role))
            {
                throw new CustomException("This role already in this setting", "Add new role to Can write settings", "Role", 400);
            }

            channel.RolesCanWrite.Add(role);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            return true;
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

    public async Task<bool> RemoveRoleFromCanWriteSettingAsync(Guid chnnelId, string token, Guid roleId)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var user = await _authService.GetUserByTokenAsync(token);

            var channel = await CheckChannelExistAsync(chnnelId, true);

            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);

            var sub = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == server.Id && (us.Role.Name == "Admin" || server.CreatorId == user.Id));
            if (sub == null)
            {
                throw new CustomException("User not admin or creator of this server", "Remove role from Can write settings", "Owner", 401);
            }

            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                throw new CustomException("Role not found", "Remove role from Can write settings", "Role", 404);
            }

            if (!channel.RolesCanWrite.Contains(role))
            {
                throw new CustomException("This role isnt in this setting", " role from Can write settings", "Role", 400);
            }

            channel.RolesCanWrite.Remove(role);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            return true;
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