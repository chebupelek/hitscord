using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace hitscord_net.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly HitsContext _hitsContext;

    public AuthenticationService(HitsContext hitsContext, ITokenService tokenService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
    }

    public async Task CheckSubscriptionNotExistAsync(Guid ServerId, Guid UserId)
    {
        try
        {
            var existingSubscription = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == ServerId);
            if (existingSubscription != null)
            {
                throw new CustomException("User is already subscribed to this server", "Check subscription is not exist", "User", 400);
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

    public async Task<UserServerDbModel> CheckSubscriptionExistAsync(Guid ServerId, Guid UserId)
    {
        try
        {
            var existingSubscription = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == ServerId);
            if (existingSubscription == null)
            {
                throw new CustomException("User not subscriber of this server", "Check subscription is exist", "User", 400);
            }
            return existingSubscription;
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

    public async Task<UserServerDbModel> CheckUserNotCreatorAsync(Guid ServerId, Guid UserId)
    {
        try
        {
            var existingSubscription = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == ServerId);
            if (existingSubscription == null)
            {
                throw new CustomException("User not subscriber of this server", "Check user is not creator", "User", 400);
            }
            if(await _hitsContext.Server.FirstOrDefaultAsync(s => s.CreatorId == UserId && s.Id == ServerId) != null)
            {
                throw new CustomException("User is creator of this server", "Check user is not creator", "User", 400);
            }
            return existingSubscription;
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

    public async Task<UserServerDbModel> CheckUserCreatorAsync(Guid ServerId, Guid UserId)
    {
        try
        {
            var existingSubscription = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == ServerId);
            if (existingSubscription == null)
            {
                throw new CustomException("User not subscriber of this server", "Check user is creator", "User", 400);
            }
            if (await _hitsContext.Server.FirstOrDefaultAsync(s => s.CreatorId == UserId && s.Id == ServerId) == null)
            {
                throw new CustomException("User is not creator of this server", "Check user is creator", "User", 400);
            }
            return existingSubscription;
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

    public async Task CheckUserRightsChangeRoles(Guid ServerId, Guid UserId)
    {
        try
        {
            var existingSubscription = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == ServerId);
            if (existingSubscription == null)
            {
                throw new CustomException("User not subscriber of this server", "Check user rights for changing roles", "User", 400);
            }
            var server = await _hitsContext.Server.Include(s => s.RolesCanChangeRolesUsers).FirstOrDefaultAsync(s => s.Id == ServerId);
            if (server == null || server.RolesCanChangeRolesUsers.Contains(existingSubscription.Role))
            {
                throw new CustomException("User doesnt has rights to change roles", "Check user rights for changing roles", "User", 400);
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

    public async Task CheckUserRightsWorkWithChannels(Guid ServerId, Guid UserId)
    {
        try
        {
            var existingSubscription = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == ServerId);
            if (existingSubscription == null)
            {
                throw new CustomException("User not subscriber of this server", "Check user rights for work with channels", "User", 400);
            }
            var server = await _hitsContext.Server.Include(s => s.RolesCanWorkWithChannels).FirstOrDefaultAsync(s => s.Id == ServerId);
            if (server == null || server.RolesCanWorkWithChannels.Contains(existingSubscription.Role))
            {
                throw new CustomException("User doesnt has rights to change roles", "Check user rights for work with channels", "User", 400);
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

    public async Task CheckUserRightsWriteInChannel(Guid channelId, Guid UserId)
    {
        try
        {
            var channel = await _hitsContext.Channel.Include(c => c.RolesCanView).Include(c => c.RolesCanWrite).FirstOrDefaultAsync(c => c.Id == channelId);
            if(channel == null)
            {
                throw new CustomException("Channel not exist", "Check user rights for write in channel", "Channel", 400);
            }
            var server = await _hitsContext.Server.Include(s => s.Channels).FirstOrDefaultAsync(s => s.Channels.Contains(channel));
            if (server == null)
            {
                throw new CustomException("Server not exist", "Check user rights for write in channel", "Server", 400);
            }
            var existingSubscription = await _hitsContext.UserServer.Include(us => us.Role).FirstOrDefaultAsync(us => us.UserId == UserId && us.ServerId == server.Id);
            if (existingSubscription == null)
            {
                throw new CustomException("User not subscriber of this server", "Check user rights for write in channel", "User", 400);
            }
            if (channel.RolesCanView.Contains(existingSubscription.Role))
            {
                throw new CustomException("Role of user cant see this channel", "Check user rights for write in channel", "User", 401);
            }
            if (channel.RolesCanWrite.Contains(existingSubscription.Role))
            {
                throw new CustomException("Role of user cant join to this channel", "Check user rights for write in channel", "User", 401);
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
}
