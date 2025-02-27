﻿using Grpc.Core;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using hitscord_net.OtherFunctions.WebSockets;
using Microsoft.EntityFrameworkCore;

namespace hitscord_net.Services;

public class ChannelService : IChannelService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;
    private readonly IServerService _serverService;
    private readonly IRoleService _roleService;
    private readonly IAuthenticationService _authenticationService;
    private readonly WebSocketsManager _webSocketManager;

    public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthorizationService authService, IRoleService roleService, IServerService serverService, IAuthenticationService authenticationService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serverService = serverService ?? throw new ArgumentNullException(nameof(serverService));
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
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

    public async Task<ChannelDbModel> CheckTextChannelExistAsync(Guid channelId)
    {
        try
        {
            var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && c is TextChannelDbModel);
            if (channel == null)
            {
                throw new CustomException("Text channel not found", "Check text channel for existing", "Text channel", 404);
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
            var owner = await _authService.GetUserByTokenAsync(token);
            var server = await _serverService.CheckServerExistAsync(serverId, false);
            await _authenticationService.CheckUserRightsWorkWithChannels(server.Id, owner.Id);
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
                        IsMessage = false,
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

            var newChannelResponse = new NewChannelResponseDTO
            {
                Create = true,
                ServerId = serverId,
                ChannelId = newChannel.Id,
                ChannelName = newChannel.Name,
                ChannelType = channelType
            };
            var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == serverId).Select(us => us.UserId).ToListAsync();
            if (alertedUsers != null && alertedUsers.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newChannelResponse, alertedUsers, "New channel");
            }
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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
            await _authenticationService.CheckUserRightsWriteInChannel(channel.Id, user.Id);
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

            var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
            {
                ServerId = channel.ServerId,
                isEnter = true,
                UserId = user.Id,
                ChannelId = channel.Id,
                UserName = (await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == channel.ServerId)).UserServerName
            };
            var usersServer = await _hitsContext.UserServer.Where(us => us.ServerId == channel.ServerId).Select(us => us.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, usersServer, "New user in voice channel");
            }

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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
            await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
            if(!((VoiceChannelDbModel)channel).Users.Contains(user))
            {
                throw new CustomException("User noty on this channel", "Remove from voice channel", "Voice channel - User", 400);
            }
            ((VoiceChannelDbModel)channel).Users.Remove(user);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
            {
                ServerId = channel.ServerId,
                isEnter = false,
                UserId = user.Id,
                ChannelId = channel.Id,
                UserName = (await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == user.Id && us.ServerId == channel.ServerId)).UserServerName
            };
            var usersServer = await _hitsContext.UserServer.Where(us => us.ServerId == channel.ServerId).Select(us => us.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, usersServer, "User remove from voice channel");
            }

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

    public async Task<bool> RemoveUserFromVoiceChannelAsync(Guid chnnelId, string token, Guid UserId)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var removedUser = await _authService.GetUserByIdAsync(UserId);
            var channel = await CheckVoiceChannelExistAsync(chnnelId, true);
            var server = await _serverService.CheckServerExistAsync(channel.ServerId, true);
            await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
            if (!((VoiceChannelDbModel)channel).Users.Contains(removedUser))
            {
                throw new CustomException("User not on this channel", "Remove user from voice channel", "Voice channel - User", 400);
            }
            ((VoiceChannelDbModel)channel).Users.Remove(removedUser);
            _hitsContext.Channel.Update(channel);
            await _hitsContext.SaveChangesAsync();

            var newUserInVoiceChannel = new UserVoiceChannelResponseDTO
            {
                ServerId = channel.ServerId,
                isEnter = false,
                UserId = user.Id,
                ChannelId = channel.Id,
                UserName = (await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == removedUser.Id && us.ServerId == channel.ServerId)).UserServerName
            };
            var usersServer = await _hitsContext.UserServer.Where(us => us.ServerId == channel.ServerId).Select(us => us.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, usersServer, "User removed from voice channel");
            }

            await _webSocketManager.BroadcastMessageAsync(newUserInVoiceChannel, new List<Guid> { removedUser.Id }, "You removed from voice channel");

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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckChannelExistAsync(chnnelId, false);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
            if (channel is VoiceChannelDbModel)
            {
                ((VoiceChannelDbModel)channel).Users.Clear();
                _hitsContext.Channel.Update(channel);
            } 
            _hitsContext.Channel.Remove(channel);
            await _hitsContext.SaveChangesAsync();

            var deletedChannelResponse = new NewChannelResponseDTO
            {
                Create = false,
                ServerId = channel.ServerId,
                ChannelId = channel.Id,
                ChannelName = channel.Name,
                ChannelType = channel is VoiceChannelDbModel ? ChannelTypeEnum.Voice : (channel is TextChannelDbModel ? ChannelTypeEnum.Text : ChannelTypeEnum.Announcement)
            };
            var usersServer = await _hitsContext.UserServer.Where(us => us.ServerId == channel.ServerId).Select(us => us.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(deletedChannelResponse, usersServer, "Channel deleted");
            }

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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckChannelExistAsync(chnnelId, true);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
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

    public async Task<MessageListResponseDTO> MessagesListAsync(Guid channelId, string token, int number, int fromStart)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckTextChannelExistAsync(channelId);
            await _authenticationService.CheckUserRightsSeeChannel(channel.Id, user.Id);
            var userCoordinates = await _hitsContext.UserCoordinates.FirstOrDefaultAsync(uc => uc.UserId == user.Id);
            if(userCoordinates != null) 
            {
                userCoordinates.ServerId = channel.ServerId;
                userCoordinates.ChannelId = channelId;
                _hitsContext.UserCoordinates.Update(userCoordinates);
                await _hitsContext.SaveChangesAsync();
            }
            return new MessageListResponseDTO
            {
                Messages = await _hitsContext.Messages
                    .Where(m => m.TextChannelId == channel.Id)
		    .OrderByDescending(m => m.CreatedAt)
		    .Skip(fromStart)
		    .Take(number)
		    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MessageResponceDTO
                    {
                        ServerId = channel.ServerId,
                        ChannelId = channel.Id,
                        Id = m.Id,
                        Text = m.Text,
                        AuthorId = m.UserId,
                        AuthorName = (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == m.UserId && us.ServerId == channel.ServerId)) != null ? (_hitsContext.UserServer.FirstOrDefault(us => us.UserId == m.UserId && us.ServerId == channel.ServerId)).UserServerName : "Неизвестен",
                        CreatedAt = m.CreatedAt,
                        ModifiedAt = m.UpdatedAt,
                        NestedChannelId = m.NestedChannelId,
                        ReplyToMessage = m.ReplyToMessageId != null
                            ? _hitsContext.Messages
                                .Where(r => r.Id == m.ReplyToMessageId)
                                .Select(r => new MessageResponceDTO
                                {
                                    ServerId = channel.ServerId,
                                    ChannelId = channel.Id,
                                    Id = r.Id,
                                    Text = r.Text,
                                    AuthorId = r.UserId,
                                    AuthorName = _hitsContext.UserServer
                                        .Where(us => us.UserId == r.UserId && us.ServerId == channel.ServerId)
                                        .Select(us => us.UserServerName)
                                        .FirstOrDefault() ?? "Неизвестен",
                                    CreatedAt = r.CreatedAt,
                                    ModifiedAt = r.UpdatedAt,
                                    NestedChannelId = r.NestedChannelId,
                                    ReplyToMessage = null
                                })
                                .FirstOrDefault()
                            : null
                    })
                    .ToListAsync(),
                NumberOfMessages = number,
                NumberOfStarterMessage = fromStart
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

    public async Task<bool> AddRoleToCanReadSettingAsync(Guid chnnelId, string token, Guid roleId)
    {
        try
        {
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckChannelExistAsync(chnnelId, true);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
            var role = await _roleService.CheckRoleExistByIdAsync(roleId);
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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckChannelExistAsync(chnnelId, true);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
            var role = await _roleService.CheckRoleExistByIdAsync(roleId);
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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckChannelExistAsync(chnnelId, true);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
            var role = await _roleService.CheckRoleExistByIdAsync(roleId);
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
            var user = await _authService.GetUserByTokenAsync(token);
            var channel = await CheckChannelExistAsync(chnnelId, true);
            await _authenticationService.CheckUserRightsWorkWithChannels(channel.ServerId, user.Id);
            var role = await _roleService.CheckRoleExistByIdAsync(roleId);
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
