using Authzed.Api.V0;
using Grpc.Core;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using hitscord_net.OtherFunctions.WebSockets;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Channels;

namespace hitscord_net.Services;

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IRoleService _roleService;
    private readonly IAuthenticationService _authenticationService;
    private readonly WebSocketsManager _webSocketManager;

    public ServerService(HitsContext hitsContext, IAuthorizationService authorizationService, IRoleService roleService, IAuthenticationService authenticationService, WebSocketsManager webSocketManager)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
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
                .Include(s => s.RolesCanChangeRolesUsers)
                .Include(s => s.RolesCanDeleteUsers)
                .Include(s => s.RolesCanWorkWithChannels)
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

    public async Task CreateServerAsync(string token, string serverName)
    {
        try
        {
            var user = await _authorizationService.GetUserByTokenAsync(token);
            var adminRole = await _roleService.CheckRoleExistAsync("Admin");

            var newServer = new ServerDbModel()
            {
                Name = serverName,
                CreatorId = user.Id,
                RolesCanDeleteUsers = new List<RoleDbModel>() { adminRole },
                RolesCanWorkWithChannels = new List<RoleDbModel>() { adminRole },
                RolesCanChangeRolesUsers = new List<RoleDbModel>() { adminRole }
            };
            await _hitsContext.Server.AddAsync(newServer);
            await _hitsContext.SaveChangesAsync();

            var newSub = new UserServerDbModel
            {
                UserId = user.Id,
                ServerId = newServer.Id,
                RoleId = adminRole.Id,
                UserServerName = user.AccountName
            };
            await _hitsContext.UserServer.AddAsync(newSub);
            await _hitsContext.SaveChangesAsync();

            var newTextChannel = new TextChannelDbModel
            {
                Name = "Основной текстовый",
                ServerId = newServer.Id,
                IsMessage = false,
                RolesCanView = await _hitsContext.Role.ToListAsync(),
                RolesCanWrite = await _hitsContext.Role.ToListAsync()
            };
            var newVoiceChannel = new VoiceChannelDbModel
            {
                Name = "Основной голосовой",
                ServerId = newServer.Id,
                RolesCanView = await _hitsContext.Role.ToListAsync(),
                RolesCanWrite = await _hitsContext.Role.ToListAsync()
            };
            var newAnnouncementChannel = new AnnouncementChannelDbModel
            {
                Name = "Основной опросный",
                ServerId = newServer.Id,
                RolesCanView = await _hitsContext.Role.ToListAsync(),
                RolesCanWrite = await _hitsContext.Role.ToListAsync()
            };
            await _hitsContext.Channel.AddAsync(newTextChannel);
            await _hitsContext.Channel.AddAsync(newVoiceChannel);
            await _hitsContext.Channel.AddAsync(newAnnouncementChannel);
            await _hitsContext.SaveChangesAsync();

            newServer.Channels.Add(newTextChannel);
            newServer.Channels.Add(newVoiceChannel);
            newServer.Channels.Add(newAnnouncementChannel);
            _hitsContext.Server.Update(newServer);
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

    public async Task SubscribeAsync(Guid serverId, string token, string? userName)
    {
        try
        {
            var user = await _authorizationService.GetUserByTokenAsync(token);
            var server = await CheckServerExistAsync(serverId, false);
            await _authenticationService.CheckSubscriptionNotExistAsync(server.Id, user.Id);
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

            var newSubscriberResponse = new NewSubscribeResponseDTO
            {
                ServerId = serverId,
                UserId = user.Id,
                UserName = newSub.UserServerName,
                Role = uncertainRole
            };
            var usersServer = await _hitsContext.UserCoordinates.Where(uc => uc.ServerId == serverId).Select(uc => uc.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, usersServer, "New user on server");
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

    public async Task UnsubscribeAsync(Guid serverId, string token)
    {
        try
        {
            var user = await _authorizationService.GetUserByTokenAsync(token);
            var server = await CheckServerExistAsync(serverId, false);
            var sub = await _authenticationService.CheckUserNotCreatorAsync(server.Id, user.Id);
            var voiceChannel = await _hitsContext.Channel
                .Include(c => ((VoiceChannelDbModel)c).Users)
                .FirstOrDefaultAsync(c => 
                    c.ServerId == serverId && 
                    c is VoiceChannelDbModel && 
                    ((VoiceChannelDbModel)c).Users.Contains(user)
                );
            if(voiceChannel != null)
            {
                ((VoiceChannelDbModel)voiceChannel).Users.Remove(user);
            }
            _hitsContext.UserServer.Remove(sub);
            await _hitsContext.SaveChangesAsync();

            var userCoordinates = await _hitsContext.UserCoordinates.FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.ServerId == serverId);
            if (userCoordinates != null)
            {
                userCoordinates.ServerId = null;
                userCoordinates.ChannelId = null;
                _hitsContext.UserCoordinates.Update(userCoordinates);
                await _hitsContext.SaveChangesAsync();
            }

            var newUnsubscriberResponse = new UnsubscribeResponseDTO
            {
                ServerId = serverId,
                UserId = user.Id,
            };
            var usersServer = await _hitsContext.UserCoordinates.Where(uc => uc.ServerId == serverId).Select(uc => uc.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, usersServer, "User unsubscribe");
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

    public async Task UnsubscribeForCreatorAsync(Guid serverId, string token, Guid newCreatorId)
    {
        try
        {
            var owner = await _authorizationService.GetUserByTokenAsync(token);
            var server = await CheckServerExistAsync(serverId, false);
            var newCreator = await _authorizationService.GetUserByIdAsync(newCreatorId);
            var ownerSub = await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
            var newCreatorSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, newCreator.Id);
            var adminRole = (await _roleService.CheckRoleExistAsync("Admin")).Id;
            var voiceChannel = await _hitsContext.Channel
                .Include(c => ((VoiceChannelDbModel)c).Users)
                .FirstOrDefaultAsync(c => 
                    c.ServerId == serverId && 
                    c is VoiceChannelDbModel && 
                    ((VoiceChannelDbModel)c).Users.Contains(owner)
                );
            if (voiceChannel != null)
            {
                ((VoiceChannelDbModel)voiceChannel).Users.Remove(owner);
            }
            _hitsContext.UserServer.Remove(ownerSub);
            server.CreatorId = newCreator.Id;
            _hitsContext.Server.Update(server);
            newCreatorSub.RoleId = adminRole;
            _hitsContext.UserServer.Update(newCreatorSub);
            await _hitsContext.SaveChangesAsync();

            var userCoordinates = await _hitsContext.UserCoordinates.FirstOrDefaultAsync(uc => uc.UserId == owner.Id && uc.ServerId == serverId);
            if (userCoordinates != null)
            {
                userCoordinates.ServerId = null;
                userCoordinates.ChannelId = null;
                _hitsContext.UserCoordinates.Update(userCoordinates);
                await _hitsContext.SaveChangesAsync();
            }

            var newUnsubscriberResponse = new UnsubscribeResponseDTO
            {
                ServerId = serverId,
                UserId = owner.Id,
            };
            var usersServer = await _hitsContext.UserCoordinates.Where(uc => uc.ServerId == serverId).Select(uc => uc.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, usersServer, "User unsubscribe");
            }

            var newUserRole = new NewUserRoleResponseDTO
            {
                UserId = newCreatorSub.UserId,
                RoleId = adminRole,
            };
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUserRole, usersServer, "Role changed");
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

    public async Task DeleteServerAsync(Guid serverId, string token)
    {
        try
        {
            var owner = await _authorizationService.GetUserByTokenAsync(token);
            var server = await CheckServerExistAsync(serverId, true);
            await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
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

            var usersServer = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(new { ServerId =  server.Id}, usersServer, "Server deleted");
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

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        try
        {
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
            var owner = await _authorizationService.GetUserByTokenAsync(token);
            var server = await CheckServerExistAsync(serverId, false);
            await _authenticationService.CheckUserRightsChangeRoles(server.Id, owner.Id);
            await _authorizationService.GetUserByIdAsync(userId);
            var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
            if(userId == owner.Id)
            {
                throw new CustomException("User cant change his role", "Change user role", "User", 400);
            }
            if (server.CreatorId == userId)
            {
                throw new CustomException("User cant change role of creator", "Change user role", "User", 400);
            }
            var role = await _roleService.CheckRoleExistByIdAsync(roleId);
            userSub.RoleId = role.Id;
            _hitsContext.UserServer.Update(userSub);
            await _hitsContext.SaveChangesAsync();

            var newUserRole = new NewUserRoleResponseDTO
            {
                UserId = userId,
                RoleId = roleId,
            };
            var usersServer = await _hitsContext.UserCoordinates.Where(uc => uc.ServerId == serverId).Select(uc => uc.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUserRole, usersServer, "Role changed");
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

    public async Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId)
    {
        try
        {
            var user = await _authorizationService.GetUserByTokenAsync(token);
            var server = await GetServerFullModelAsync(serverId);
            var sub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
            var announcementChannels = await _hitsContext.Channel
                .Include(c => c.RolesCanView)
                .Include(c => c.RolesCanWrite)
                .Include(c => ((AnnouncementChannelDbModel)c).RolesToNotify)
                .Where(c => c.ServerId == serverId && c is AnnouncementChannelDbModel)
                .ToListAsync();

            var info = new ServerInfoDTO
            {
                ServerId = serverId,
                ServerName = server.Name,
                Roles = await _hitsContext.Role.ToListAsync(),
                UserRoleId = sub.RoleId,
                UserRole = sub.Role.Name,
                CanChangeRole = server.RolesCanChangeRolesUsers.Contains(sub.Role),
                CanDeleteUsers = server.RolesCanDeleteUsers.Contains(sub.Role),
                CanWorkWithChannels = server.RolesCanWorkWithChannels.Contains(sub.Role),
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
                Channels = new ChannelListDTO
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
                    /*
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
                    .ToList()*/
                }
            };

            var userCoordinates = await _hitsContext.UserCoordinates.FirstOrDefaultAsync(uc => uc.UserId ==  user.Id);
            if (userCoordinates != null)
            {
                userCoordinates.ServerId = server.Id;
                userCoordinates.ChannelId = null;
                _hitsContext.UserCoordinates.Update(userCoordinates);
                await _hitsContext.SaveChangesAsync();
            }

            return info;
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

    public async Task DeleteUserFromServerAsync(string token, Guid serverId, Guid userId)
    {
        try
        {
            var owner = await _authorizationService.GetUserByTokenAsync(token);
            var server = await CheckServerExistAsync(serverId, false);
            await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);
            await _authorizationService.GetUserByIdAsync(userId);
            var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
            if (userId == owner.Id)
            {
                throw new CustomException("User cant delete himself", "Change user role", "User", 400);
            }
            if (server.CreatorId == userId)
            {
                throw new CustomException("User cant delete creator of server", "Change user role", "User", 400);
            }
            _hitsContext.UserServer.Remove(userSub);
            await _hitsContext.SaveChangesAsync();

            var newUnsubscriberResponse = new UnsubscribeResponseDTO
            {
                ServerId = serverId,
                UserId = userId,
            };
            var usersServer = await _hitsContext.UserCoordinates.Where(uc => uc.ServerId == serverId).Select(uc => uc.UserId).ToListAsync();
            if (usersServer != null && usersServer.Count() > 0)
            {
                await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, usersServer, "User unsubscribe");
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