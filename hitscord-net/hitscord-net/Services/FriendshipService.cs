using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace hitscord_net.Services;

public class FriendshipService : IFriendshipService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authService;

    public FriendshipService(HitsContext hitsContext, IAuthorizationService authService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task CreateFriendshipApplicationAsync(string token, Guid userApplicationTo)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);
            var user = await _authService.GetUserByIdAsync(userApplicationTo);

            var friendshipApplication = await _hitsContext.FriendshipApplication
                .FirstOrDefaultAsync(application => 
                    (application.UserFromId == owner.Id && application.UserToId == user.Id) ||
                    (application.UserToId == owner.Id && application.UserFromId == user.Id)
                );

            if (friendshipApplication != null) 
            {
                throw new CustomException("Application with owner and user already exist", "Create friendship application", "Friendship application", 400);
            }

            var friendship = await _hitsContext.Friendship
                .FirstOrDefaultAsync(friendship =>
                    (friendship.UserFirstId == owner.Id && friendship.UserSecondId == user.Id) ||
                    (friendship.UserSecondId == owner.Id && friendship.UserFirstId == user.Id)
                );

            if (friendship != null)
            {
                throw new CustomException("Friendship between users already exist", "Create friendship application", "Friendship application", 400);
            }

            var newFriendshipApplication = new FriendshipApplicationDbModel()
            {
                UserFrom = owner,
                UserTo = user
            };

            await _hitsContext.FriendshipApplication.AddAsync(newFriendshipApplication);
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

    public async Task DeleteFriendshipApplicationAsync(string token, Guid userApplicationTo)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);
            var user = await _authService.GetUserByIdAsync(userApplicationTo);

            var friendshipApplication = await _hitsContext.FriendshipApplication
                .FirstOrDefaultAsync(application =>
                    (application.UserFromId == owner.Id && application.UserToId == user.Id) ||
                    (application.UserToId == owner.Id && application.UserFromId == user.Id)
                );

            if (friendshipApplication == null)
            {
                throw new CustomException("Application with owner and user doesnt exist", "Delete friendship application", "Friendship application", 400);
            }

            _hitsContext.FriendshipApplication.Remove(friendshipApplication);
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

    public async Task AccessFriendshipApplicationAsync(string token, Guid userId)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);
            var user = await _authService.GetUserByIdAsync(userId);

            var friendshipApplication = await _hitsContext.FriendshipApplication.
                FirstOrDefaultAsync(application => application.UserFromId == user.Id && application.UserToId == owner.Id);

            if (friendshipApplication == null)
            {
                throw new CustomException("Application with owner and user doesnt exist", "Access friendship application", "Friendship application", 400);
            }

            var newFriendship = new FriendshipDbModel
            {
                UserFirst = owner,
                UserSecond = user
            };

            _hitsContext.FriendshipApplication.Remove(friendshipApplication);
            await _hitsContext.Friendship.AddAsync(newFriendship);
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

    public async Task<List<FriendshipApplicationDTO>> GetFriendshipApplicationsListFromMeAsync(string token)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);

            var friendshipApplicationList = await _hitsContext.FriendshipApplication
                .Include(application => application.UserTo)
                .Where(application => application.UserFromId == owner.Id)
                .Select(application => new FriendshipApplicationDTO
                {
                    User = new UserFriendshipDTO
                    {
                        UserId = application.UserTo.Id,
                        UserName = application.UserTo.AccountName,
                        UserTag = application.UserTo.AccountTag
                    },
                    CreateDate = (DateTime)application.CreateTime
                })
                .ToListAsync();

            return (friendshipApplicationList == null ? new List<FriendshipApplicationDTO>() : friendshipApplicationList);
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

    public async Task<List<FriendshipApplicationDTO>> GetFriendshipApplicationsListToMeAsync(string token)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);

            var friendshipApplicationList = await _hitsContext.FriendshipApplication
                .Include(application => application.UserFrom)
                .Where(application => application.UserToId == owner.Id)
                .Select(application => new FriendshipApplicationDTO
                {
                    User = new UserFriendshipDTO
                    {
                        UserId = application.UserFrom.Id,
                        UserName = application.UserFrom.AccountName,
                        UserTag = application.UserFrom.AccountTag
                    },
                    CreateDate = (DateTime)application.CreateTime
                })
                .ToListAsync();

            return (friendshipApplicationList == null ? new List<FriendshipApplicationDTO>() : friendshipApplicationList);
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

    public async Task<List<FriendshipApplicationDTO>> GetFriendshipListAsync(string token)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);

            var friendshipList = await _hitsContext.Friendship
                .Include(friendship => friendship.UserFirst)
                .Include(friendship => friendship.UserSecond)
                .Where(friendship => friendship.UserFirstId == owner.Id || friendship.UserSecondId == owner.Id)
                .Select(frindship => new FriendshipApplicationDTO
                {
                    User = new UserFriendshipDTO
                    {
                        UserId = frindship.UserFirst == owner ? frindship.UserSecond.Id : frindship.UserFirst.Id,
                        UserName = frindship.UserFirst == owner ? frindship.UserSecond.AccountName : frindship.UserFirst.AccountName,
                        UserTag = frindship.UserFirst == owner ? frindship.UserSecond.AccountTag : frindship.UserFirst.AccountTag,
                    },
                    CreateDate = (DateTime)frindship.CreateTime
                })
                .ToListAsync();

            return (friendshipList == null ? new List<FriendshipApplicationDTO>() : friendshipList);
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

    public async Task RemoveFriendShipAsync(string token, Guid userId)
    {
        try
        {
            var owner = await _authService.GetUserByTokenAsync(token);
            var user = await _authService.GetUserByIdAsync(userId);

            var friendship = await _hitsContext.Friendship
                .FirstOrDefaultAsync(friendship =>
                    (friendship.UserFirstId == owner.Id && friendship.UserSecondId == user.Id) ||
                    (friendship.UserSecondId == owner.Id && friendship.UserFirstId == user.Id)
                );

            if (friendship == null)
            {
                throw new CustomException("Friendship between users doesnt exost", "Remove friendship", "Friendship", 400);
            }

            _hitsContext.Friendship.Remove(friendship);
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
}
