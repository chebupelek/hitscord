using Authzed.Api.V0;
using Grpc.Core;
using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.other;
using Microsoft.EntityFrameworkCore;
using System.Data;
using hitscord.OrientDb.Service;
using HitscordLibrary.Models.other;
using EasyNetQ;
using HitscordLibrary.SocketsModels;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace hitscord.Services;

public class ServerService : IServerService
{
    private readonly HitsContext _hitsContext;
    private readonly IAuthorizationService _authorizationService;
    private readonly IServices.IAuthenticationService _authenticationService;
    private readonly OrientDbService _orientDbService;

    public ServerService(HitsContext hitsContext, IAuthorizationService authorizationService, IServices.IAuthenticationService authenticationService, OrientDbService orientDbService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _orientDbService = orientDbService ?? throw new ArgumentNullException(nameof(orientDbService));
    }

    public async Task<ServerDbModel> CheckServerExistAsync(Guid serverId, bool includeChannels)
    {
        var server = includeChannels ? await _hitsContext.Server.Include(s => s.Roles).Include(s => s.Channels).FirstOrDefaultAsync(s => s.Id == serverId) :
            await _hitsContext.Server.FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            throw new CustomException("Server not found", "Check server for existing", "Server id", 404, "Сервер не найден", "Проверка наличия сервера");
        }
        return server;
    }

    public async Task<ServerDbModel> GetServerFullModelAsync(Guid serverId)
    {
        var server = await _hitsContext.Server
                .Include(s => s.Channels)
                .Include(s => s.Channels)
                .Include(s => s.Roles)
                .FirstOrDefaultAsync(s => s.Id == serverId);
        if (server == null)
        {
            throw new CustomException("Server not found", "Get server with full model", "Server id", 404, "Сервер не найден", "Получение полной информации о сервере");
        }
        return server;
    }

    private async Task<RoleDbModel> CreateRoleAsync(Guid serverId, RoleEnum role, string roleName)
    {
        var newRole = new RoleDbModel()
        {
            Name = roleName,
            Role = role,
            ServerId = serverId
        };
        await _hitsContext.Role.AddAsync(newRole);
        await _hitsContext.SaveChangesAsync();
        return newRole;
    }

    public async Task CreateServerAsync(string token, string serverName)
    {
        var user = await _authorizationService.GetUserAsync(token);

        var newServer = new ServerDbModel()
        {
            Name = serverName
        };
        await _hitsContext.Server.AddAsync(newServer);
        await _hitsContext.SaveChangesAsync();

        var creatorRole = await CreateRoleAsync(newServer.Id, RoleEnum.Creator, "Создатель");
        var adminRole = await CreateRoleAsync(newServer.Id, RoleEnum.Admin, "Админ");
        var teacherRole = await CreateRoleAsync(newServer.Id, RoleEnum.Teacher, "Учитель");
        var studentRole = await CreateRoleAsync(newServer.Id, RoleEnum.Student, "Студент");
        var uncertainRole = await CreateRoleAsync(newServer.Id, RoleEnum.Uncertain, "Неопределенная");
        newServer.Roles = new List<RoleDbModel> { creatorRole, adminRole, teacherRole, studentRole, uncertainRole };
        _hitsContext.Server.Update(newServer);
        await _hitsContext.SaveChangesAsync();

        var newSub = new UserServerDbModel
        {
            UserId = user.Id,
            RoleId = creatorRole.Id,
            UserServerName = user.AccountName
        };
        await _hitsContext.UserServer.AddAsync(newSub);
        await _hitsContext.SaveChangesAsync();

        var newTextChannel = new TextChannelDbModel
        {
            Name = "Основной текстовый",
            ServerId = newServer.Id,
            IsMessage = false,
        };
        var newVoiceChannel = new VoiceChannelDbModel
        {
            Name = "Основной голосовой",
            ServerId = newServer.Id,
        };
        await _hitsContext.Channel.AddAsync(newTextChannel);
        await _hitsContext.Channel.AddAsync(newVoiceChannel);
        await _hitsContext.SaveChangesAsync();

        newServer.Channels.Add(newTextChannel);
        newServer.Channels.Add(newVoiceChannel);
        _hitsContext.Server.Update(newServer);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.CreateServerAsync(newServer.Id ,user.Id, new List<Guid> {newTextChannel.Id, newVoiceChannel.Id}, new List<RoleDbModel> { creatorRole, adminRole, teacherRole, studentRole, uncertainRole });
    }

    public async Task SubscribeAsync(Guid serverId, string token, string? userName)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, true);
        await _authenticationService.CheckSubscriptionNotExistAsync(server.Id, user.Id);
        var newSub = new UserServerDbModel
        {
            UserId = user.Id,
            RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
            UserServerName = userName != null ? userName : user.AccountName
        };
        await _hitsContext.UserServer.AddAsync(newSub);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.AssignUserToRoleAsync(user.Id, server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain).Id, server.Id);

        var newSubscriberResponse = new NewSubscribeResponseDTO
        {
            ServerId = serverId,
            UserId = user.Id,
            UserName = newSub.UserServerName,
            RoleId = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Id,
            RoleName = (server.Roles.FirstOrDefault(r => r.Role == RoleEnum.Uncertain)).Name
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        alertedUsers = alertedUsers.Where(a => a != user.Id).ToList();
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
            using (var bus = RabbitHutch.CreateBus($"host={rabbitHost}"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newSubscriberResponse, UserIds = alertedUsers, Message = "New user on server" }, "SendNotification");
            }
        }
    }

    public async Task UnsubscribeAsync(Guid serverId, string token)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, false);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        var subRole = await _authenticationService.CheckUserNotCreatorAsync(server.Id, user.Id);
        var sub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == subRole.Id && us.UserId == user.Id);
        var voiceChannel = await _hitsContext.Channel
            .Include(c => ((VoiceChannelDbModel)c).Users)
            .FirstOrDefaultAsync(c =>
                c.ServerId == serverId &&
                c is VoiceChannelDbModel &&
                ((VoiceChannelDbModel)c).Users.Contains(user)
            );
        if (voiceChannel != null)
        {
            ((VoiceChannelDbModel)voiceChannel).Users.Remove(user);
        }
        _hitsContext.UserServer.Remove(sub);
        await _hitsContext.SaveChangesAsync();
        
        await _orientDbService.UnassignUserFromRoleAsync(user.Id, subRole.Id, server.Id);

        var newUnsubscriberResponse = new UnsubscribeResponseDTO
        {
            ServerId = serverId,
            UserId = user.Id,
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
            using (var bus = RabbitHutch.CreateBus($"host={rabbitHost}"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUnsubscriberResponse, UserIds = alertedUsers, Message = "User unsubscribe" }, "SendNotification");
            }
        }
    }

    public async Task UnsubscribeForCreatorAsync(Guid serverId, string token, Guid newCreatorId)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, false);
        var newCreator = await _authorizationService.GetUserAsync(newCreatorId);
        var ownerSubRole = await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
        var newCreatorSubRole = await _authenticationService.CheckSubscriptionExistAsync(server.Id, newCreator.Id);
        var ownerSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == ownerSubRole.Id && us.UserId == owner.Id);
        var newCreatorSub = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.RoleId == newCreatorSubRole.Id && us.UserId == newCreator.Id);
        var creatorRole = server.Roles.FirstOrDefault(s => s.Role == RoleEnum.Creator);
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
        await _orientDbService.UnassignUserFromRoleAsync(owner.Id, ownerSubRole.Id, server.Id);
        _hitsContext.UserServer.Remove(ownerSub);
        await _orientDbService.UnassignUserFromRoleAsync(newCreator.Id, newCreatorSubRole.Id, server.Id);
        await _orientDbService.AssignUserToRoleAsync(newCreator.Id, creatorRole.Id, server.Id);
        newCreatorSub.RoleId = creatorRole.Id;
        _hitsContext.UserServer.Update(newCreatorSub);
        await _hitsContext.SaveChangesAsync();

        var newUnsubscriberResponse = new UnsubscribeResponseDTO
        {
            ServerId = serverId,
            UserId = owner.Id,
        };
        var newUserRole = new NewUserRoleResponseDTO
        {
            ServerId = serverId,
            UserId = newCreatorSub.UserId,
            RoleId = creatorRole.Id,
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
            using (var bus = RabbitHutch.CreateBus($"host={rabbitHost}"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUnsubscriberResponse, UserIds = alertedUsers, Message = "User unsubscribe" }, "SendNotification");
                bus.PubSub.Publish(new NotificationDTO { Notification = newUserRole, UserIds = alertedUsers, Message = "Role changed" }, "SendNotification");
            }
        }
    }

    public async Task DeleteServerAsync(Guid serverId, string token)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, true);
        await _authenticationService.CheckUserCreatorAsync(server.Id, owner.Id);
        var userServerRelations = _hitsContext.UserServer.Where(us => us.Role.ServerId == server.Id);
        var serverRoles = _hitsContext.Role.Where(r => r.ServerId == server.Id);
        _hitsContext.UserServer.RemoveRange(userServerRelations);
        _hitsContext.Role.RemoveRange(serverRoles);
        var voiceChannels = server.Channels.OfType<VoiceChannelDbModel>().ToList();
        foreach (var voiceChannel in voiceChannels)
        {
            voiceChannel.Users.Clear();
        }
        var channelsToDelete = server.Channels.ToList();
        _hitsContext.Channel.RemoveRange(channelsToDelete);
        _hitsContext.Server.Remove(server);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.DeleteServerAsync(server.Id);

        var serverDelete = new ServerDeleteDTO
        {
            ServerId = serverId
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
            using (var bus = RabbitHutch.CreateBus($"host={rabbitHost}"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = serverDelete, UserIds = alertedUsers, Message = "Server deleted" }, "SendNotification");
            }
        }
    }

    public async Task<ServersListDTO> GetServerListAsync(string token)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var idsList = await _orientDbService.GetSubscribedServerIdsListAsync(user.Id);
        return new ServersListDTO
        {
            ServersList = await _hitsContext.Server
                .Where(s => idsList.Contains(s.Id))
                .Select(s => new ServersListItemDTO
                {
                    ServerId = s.Id,
                    ServerName = s.Name
                })
                .ToListAsync()
        };
    }

    public async Task ChangeUserRoleAsync(string token, Guid serverId, Guid userId, Guid roleId)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, false);
        await _authenticationService.CheckUserRightsChangeRoles(server.Id, owner.Id);
        await _authorizationService.GetUserAsync(userId);
        var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
        var userSubRoleId = await _orientDbService.GetUserRoleOnServerAsync(userId, serverId);
        if(userSubRoleId == null) 
        {
            throw new CustomException("User hasn't role", "Change user role", "User", 400, "У пользователя нет роли", "Изменение роли пользователя");
        }
        Guid userRoleId = (Guid)userSubRoleId;
        var userSubRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == userSubRoleId);
        if (userId == owner.Id)
        {
            throw new CustomException("User cant change his role", "Change user role", "User", 400, "Пользователь не может менять свою роль", "Изменение роли пользователя");
        }
        if (userSubRole.Role == RoleEnum.Creator)
        {
            throw new CustomException("User cant change role of creator", "Change user role", "User", 400, "Пользователь не может менять роль создателя сервера", "Изменение роли пользователя");
        }
        var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId);
        var userServ = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == userId && us.RoleId == userSubRoleId);
        userServ.RoleId = role.Id;
        _hitsContext.UserServer.Update(userServ);
        await _hitsContext.SaveChangesAsync();

        await _orientDbService.UnassignUserFromRoleAsync(userId, userRoleId, serverId);
        await _orientDbService.AssignUserToRoleAsync(userId, role.Id, serverId);

        var newUserRole = new NewUserRoleResponseDTO
        {
            ServerId = serverId,
            UserId = userId,
            RoleId = role.Id,
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
            using (var bus = RabbitHutch.CreateBus($"host={rabbitHost}"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUserRole, UserIds = alertedUsers, Message = "Role changed" }, "SendNotification");
            }
        }
    }

    public async Task<ServerInfoDTO> GetServerInfoAsync(string token, Guid serverId)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var server = await GetServerFullModelAsync(serverId);
        var sub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);
        string result = await _orientDbService.GetUserRolePermissionsOnServerAsync(user.Id, serverId);
        var channelCanSee = await _orientDbService.GetVisibleChannelsAsync(user.Id, serverId);
        var channelCanWrite = await _orientDbService.GetWritableChannelsAsync(user.Id, serverId);

        var voiceChannelResponses = await _hitsContext.VoiceChannel
            .Include(vc => vc.Users)
            .Where(vc => vc.ServerId == server.Id)
            .Select(vc => new VoiceChannelResponseDTO
            {
                ChannelName = vc.Name,
                ChannelId = vc.Id,
                CanJoin = channelCanWrite.Contains(vc.Id),
                Users = vc.Users.Select(u => new VoiceChannelUserDTO
                {
                    UserId = u.Id
                })
                .ToList()
            })
            .ToListAsync();

        var info = new ServerInfoDTO
        {
            ServerId = serverId,
            ServerName = server.Name,
            Roles = await _hitsContext.Role
                .Where(r => r.ServerId == server.Id)
                .Select(r => new RolesItemDTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    ServerId = r.ServerId
                })
                .ToListAsync(),
            UserRoleId = sub.Id,
            UserRole = sub.Name,
            IsCreator = sub.Role == RoleEnum.Creator,
            CanChangeRole = result.Contains("ServerCanChangeRole"),
            CanDeleteUsers = result.Contains("ServerCanDeleteUsers"),
            CanWorkWithChannels = result.Contains("ServerCanWorkChannels"),
            Users = await _hitsContext.UserServer
                .Include(us => us.Role)
                .Where(us => us.Role.ServerId == serverId)
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
                        channelCanSee.Contains(c.Id)
                    ) &&
                    c is TextChannelDbModel &&
                    ((TextChannelDbModel)c).IsMessage == false)
                .Select(c => new TextChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                    CanWrite = channelCanWrite.Contains(c.Id)
                })
                .ToList(),
                /*
                VoiceChannels = server.Channels
                .Where(c =>
                    (
                        channelCanSee.Contains(c.Id)
                    ) &&
                    c is VoiceChannelDbModel)
                .Select(c => new VoiceChannelResponseDTO
                {
                    ChannelName = c.Name,
                    ChannelId = c.Id,
                    CanJoin = channelCanWrite.Contains(c.Id),
                    Users = ((VoiceChannelDbModel)c).Users
                        .Select(u => new VoiceChannelUserDTO
                        {
                            UserId = u.Id,
                            UserName = u.AccountName
                        })
                        .ToList()
                })
                .ToList()
                */

                VoiceChannels = voiceChannelResponses
            }
        };

        return info;
    }

    public async Task DeleteUserFromServerAsync(string token, Guid serverId, Guid userId)
    {
        var owner = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, false);
        await _authenticationService.CheckUserRightsDeleteUsers(server.Id, owner.Id);
        await _authorizationService.GetUserAsync(userId);
        var userSub = await _authenticationService.CheckSubscriptionExistAsync(server.Id, userId);
        if (userId == owner.Id)
        {
            throw new CustomException("User cant delete himself", "Change user role", "User", 400, "Пользователь не может удалить сам себя", "Удаление пользователя с сервера");
        }
        if (userSub.Role == RoleEnum.Creator)
        {
            throw new CustomException("User cant delete creator of server", "Change user role", "User", 400, "Нельзя удалить создателя сервера", "Удаление пользователя с сервера");
        }
        var userServer = await _hitsContext.UserServer.FirstOrDefaultAsync(us => us.UserId == userId && us.RoleId == userSub.Id);
        _hitsContext.UserServer.Remove(userServer);
        await _hitsContext.SaveChangesAsync();
        await _orientDbService.UnassignUserFromRoleAsync(userId, userSub.Id, serverId);

        var newUnsubscriberResponse = new UnsubscribeResponseDTO
        {
            ServerId = serverId,
            UserId = userId,
        };
        var alertedUsers = await _orientDbService.GetUsersByServerIdAsync(serverId);
        if (alertedUsers != null && alertedUsers.Count() > 0)
        {

            var rabbitHost = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost";
            using (var bus = RabbitHutch.CreateBus($"host={rabbitHost}"))
            {
                bus.PubSub.Publish(new NotificationDTO { Notification = newUnsubscriberResponse, UserIds = alertedUsers, Message = "User unsubscribe" }, "SendNotification");
            }
        }
    }

    public async Task<RolesListDTO> GetServerRolesAsync(string token, Guid serverId)
    {
        var user = await _authorizationService.GetUserAsync(token);
        var server = await CheckServerExistAsync(serverId, true);
        await _authenticationService.CheckSubscriptionExistAsync(server.Id, user.Id);

        var list = new RolesListDTO
        {
            Roles = server.Roles
                .Select(r => new RolesItemDTO
                {
                    Id = r.Id,
                    ServerId = server.Id,
                    Name = r.Name
                })
                .ToList()
        };

        return list;
    }
}
