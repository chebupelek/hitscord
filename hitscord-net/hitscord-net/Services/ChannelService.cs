using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
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
    private readonly IAuthService _authService;

    public ChannelService(HitsContext hitsContext, ITokenService tokenService, IAuthService authService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task CreateChannelAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType)
    {
        try
        {
            await _authService.CheckUserAuthAsync(token);

            var owner = await _authService.GetUserByTokenAsync(token);

            var server = await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Create channel", "Server", 404);
            }
            if (server.Admin != owner)
            {
                throw new CustomException("Owner not admin of this server", "Create channel", "Owner", 401);
            }

            var newChannel = new ChannelDbModel()
            {
                Name = name,
                Type = channelType,
                CanRead = new List<RoleEnum>() { RoleEnum.Student, RoleEnum.Teacher, RoleEnum.Admin, RoleEnum.Uncertain },
                CanWrite = new List<RoleEnum>() { RoleEnum.Student, RoleEnum.Teacher, RoleEnum.Admin, RoleEnum.Uncertain }
            };


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

            var server = await _hitsContext.Server.Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId);
            if (server == null)
            {
                throw new CustomException("Server not found", "Get channels list", "Server", 404);
            }
            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.ServerId == serverId && s.UserId == user.Id);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Get channels list", "User", 404);
            }

            var channelsList = new ChannelListDTO
            {
                TextChannels = server.Channels
                    .Where(c => (c.CanRead.Contains(sub.Role) || server.Admin == user) && c.Type == ChannelTypeEnum.Text)
                    .Select(c => new TextChannelResponseDTO
                    {
                        ChannelName = c.Name,
                        ChannelId = c.Id,
                        CanWrite = c.CanWrite.Contains(sub.Role)
                    })
                    .ToList(),
                VoiceChannels = server.Channels
                    .Where(c => (c.CanRead.Contains(sub.Role) || server.Admin == user) && c.Type == ChannelTypeEnum.Voice)
                    .Select(c => new VoiceChannelResponseDTO
                    {
                        ChannelName = c.Name,
                        ChannelId = c.Id,
                        CanJoin = c.CanWrite.Contains(sub.Role),
                        Users = _hitsContext.UserVoiceChannel
                            .Where(vcu => vcu.VoiceChannelId == c.Id)
                            .Join(_hitsContext.User,
                                  vcu => vcu.UserId,
                                  u => u.Id,
                                  (vcu, u) => new VoiceChannelUserDTO
                                  {
                                      UserId = (Guid)u.Id,
                                      UserName = u.AccountName
                                  })
                            .ToList()
                    })
                    .ToList()
            };

            return(channelsList);
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

            var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id ==  chnnelId);
            if (channel == null)
            {
                throw new CustomException("Channel not found", "Join to voice channel", "Channel", 404);
            }
            if (channel.Type != ChannelTypeEnum.Voice)
            {
                throw new CustomException("Type of channel not Voice", "Join to voice channel", "Channel", 400);
            }

            var server = await _hitsContext.Server.Include(s => s.Channels).FirstOrDefaultAsync(s => s.Channels.Contains(channel));
            if (server == null)
            {
                throw new CustomException("Server not found", "Join to voice channel", "Server", 404);
            }

            var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(s => s.ServerId == server.Id && s.UserId == user.Id);
            if (sub == null)
            {
                throw new CustomException("User not subscriber of this server", "Join to voice channel", "User", 401);
            }

            if(!channel.CanRead.Contains(sub.Role))
            {
                throw new CustomException("Role of user cant see this channel", "Join to voice channel", "User", 401);
            }
            if (!channel.CanWrite.Contains(sub.Role))
            {
                throw new CustomException("Role of user cant join to this channel", "Join to voice channel", "User", 401);
            }

            var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.VoiceChannelId == channel.Id && uvc.UserId == user.Id);
            if (userthischannel != null)
            {
                throw new CustomException("User is already on this channel", "Join to voice channel", "Voice channel - User", 401);
            }

            var userchannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.VoiceChannelId != channel.Id && uvc.UserId == user.Id);
            if (userchannel != null)
            {
                _hitsContext.UserVoiceChannel.Remove(userchannel);
                await _hitsContext.SaveChangesAsync();
            }

            var UserVoiceChannelNew = new VoiceChannelUserDbModel
            {
                Id = Guid.NewGuid(),
                UserId = (Guid)user.Id,
                VoiceChannelId = channel.Id,
            };
            _hitsContext.UserVoiceChannel.Add(UserVoiceChannelNew);
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

            var userthischannel = await _hitsContext.UserVoiceChannel.FirstOrDefaultAsync(uvc => uvc.VoiceChannelId == chnnelId && uvc.UserId == user.Id);
            if (userthischannel == null)
            {
                throw new CustomException("User is not on this channel", "Remove from voice channel", "Voice channel - User", 401);
            }

            _hitsContext.UserVoiceChannel.Remove(userthischannel);
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
}