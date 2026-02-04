using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.request;
using hitscord.Models.response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using hitscord.nClamUtil;
using hitscord.Models.other;
using hitscord.Utils;
using SixLabors.ImageSharp;
using hitscord.JwtCreation;
using Grpc.Core;
using hitscord.WebSockets;
using Authzed.Api.V0;
using Microsoft.AspNetCore.SignalR;
using NickBuhro.Translit;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Channels;
using System.Data;
using hitscord_new.Migrations.Token;
using nClam;
using Newtonsoft.Json.Linq;

namespace hitscord.Services;

public class AdminService: IAdminService
{
	private readonly IConfiguration _configuration;
	private readonly HitsContext _hitsContext;
	private readonly PasswordHasher<string> _passwordHasher;
	private readonly TokenContext _tokenContext;
	private readonly WebSocketsManager _webSocketManager;
	private readonly MinioService _minioService;
	private readonly nClamService _clamService;
	private readonly IChannelService _channelService;

	public AdminService(TokenContext tokenContext, WebSocketsManager webSocketManager, HitsContext hitsContext, IConfiguration configuration, IChannelService channelService, MinioService minioService, nClamService clamService)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_passwordHasher = new PasswordHasher<string>();
		_configuration = configuration;
		_tokenContext = tokenContext ?? throw new ArgumentNullException(nameof(tokenContext));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
		_clamService = clamService ?? throw new ArgumentNullException(nameof(clamService));
		_channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
	}

	public async Task<FileMetaResponseDTO?> GetImageAsync(Guid iconId)
	{
		var file = await _hitsContext.File.FindAsync(iconId);
		if (file == null)
			return null;

		if (!file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
			return null;

		return new FileMetaResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size,
			Deleted = file.Deleted
		};
	}

	public async Task<bool> CheckAdminAuthAsync(string token)
	{
		if (await _tokenContext.AdminToken.FirstOrDefaultAsync(x => x.AccessToken == token) == null)
		{
			throw new CustomException("Access token not found", "CheckAuth", "Access token", 401, "Сессия не найдена", "Проверка авторизации");
		}
		var tokenHandler = new JwtSecurityTokenHandler();
		if (!tokenHandler.CanReadToken(token))
		{
			return true;
		}
		var jwtToken = tokenHandler.ReadJwtToken(token);
		var expirationTimeUnix = long.Parse(jwtToken.Claims.First(c => c.Type == "exp").Value);
		var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationTimeUnix).UtcDateTime;
		if (expirationTime < DateTime.UtcNow)
		{
			throw new CustomException("Access token expired", "CheckAuth", "Access token", 401, "Сессия окончена", "Проверка авторизации");
		}
		return true;
	}

	public async Task<AdminDbModel> GetAdminAsync(string token)
	{
		await CheckAdminAuthAsync(token);
		var tokenHandler = new JwtSecurityTokenHandler();
		var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
		var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
		if (userId == null)
		{
			throw new CustomException("UserId not found", "Profile", "Access token", 404, "Не найден подобный Id пользователя", "Получение профиля");
		}
		Guid userIdGuid = Guid.Parse(userId);
		var user = await _hitsContext.Admin.FirstOrDefaultAsync(u => u.Id == userIdGuid && u.Approved == true);
		if (user == null)
		{
			throw new CustomException("User not found", "Profile", "User", 404, "Пользователь не найден", "Получение профиля");
		}
		return user;
	}

	public async Task CreateAccount(string token, AdminRegistrationDTO registrationData)
	{
		var admin = await GetAdminAsync(token);

		if (await _hitsContext.Admin.FirstOrDefaultAsync(u => u.Login == registrationData.Login) != null)
		{
			throw new CustomException("Account with this login already exist", "Account", "Login", 400, "Аккаунт с таким логином уже существует", "Регистрация");
		}
		if (await _hitsContext.Admin.FirstOrDefaultAsync(u => u.AccountName == registrationData.AccountName) != null)
		{
			throw new CustomException("Account with this name already exist", "Account", "Name", 400, "Аккаунт с таким именем уже существует", "Регистрация");
		}

		var newUser = new AdminDbModel
		{
			Id = Guid.NewGuid(),
			Login = registrationData.Login,
			PasswordHash = _passwordHasher.HashPassword(registrationData.Login, registrationData.Password),
			AccountName = registrationData.AccountName,
			Approved = false
		};

		await _hitsContext.Admin.AddAsync(newUser);
		_hitsContext.SaveChanges();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Создание аккаунта админа",
			OperationData = $"Админ {admin.AccountName} создал новый аккаунт админа",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<TokenDTO> LoginAsync(AdminLoginDTO loginData)
	{
		var userData = await _hitsContext.Admin.FirstOrDefaultAsync(u => u.Login == loginData.Login && u.Approved == true);
		if (userData == null)
		{
			throw new CustomException("A user with this login doesnt exists", "Login", "Login", 404, "Пользователь с таким логином не существует", "Логин");
		}

		var passwordcheck = _passwordHasher.VerifyHashedPassword(loginData.Login, userData.PasswordHash, loginData.Password);

		if (passwordcheck == PasswordVerificationResult.Failed)
		{
			throw new CustomException("Wrong password", "Login", "Password", 401, "Неверный пароль", "Логин");
		}

		var tokenAccessData = userData.CreateClaims().CreateJwtTokenAccess(_configuration);
		var tokenHandler = new JwtSecurityTokenHandler();
		var accessToken = tokenHandler.WriteToken(tokenAccessData);

		var logDb = new AdminLogDbModel
		{
			Id = Guid.NewGuid(),
			AdminId = userData.Id,
			AccessToken = accessToken,
			Start = DateTime.UtcNow
		};

		try
		{
			_tokenContext.AdminToken.Add(logDb);
			await _tokenContext.SaveChangesAsync();
		}
		catch (DbUpdateException ex)
		{
			var inner = ex.InnerException?.Message;
			throw new Exception($"EF SaveChanges failed: {inner}", ex);
		}

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Авторизация",
			OperationData = $"Админ {userData.AccountName} вошел в систему в {DateTime.Now}",
			AdminId = userData.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();

		return new TokenDTO { AccessToken = accessToken };
	}

	public async Task LogoutAsync(string token)
	{
		await GetAdminAsync(token);
		var bannedToken = await _tokenContext.AdminToken.FirstOrDefaultAsync(x => x.AccessToken == token);

		if (bannedToken == null)
		{
			throw new CustomException("Access token not found", "Logout", "Access token", 404, "Access токен не найден", "Инвалидация access токена");
		}

		_tokenContext.AdminToken.Remove(bannedToken);
		await _tokenContext.SaveChangesAsync();
	}

	public async Task<UsersAdminListDTO> UsersListAsync(string token, int num, int page, UsersSortEnum? sort, string? name, string? mail, List<Guid>? rolesIds)
	{
		var admin = await GetAdminAsync(token);

		if (num < 1)
		{
			throw new CustomException("Num < 1", "UsersListAsync", "Num", 400, "Количество элементов меньше 1", "Получение списка пользователей");
		}

		if (page < 1)
		{
			throw new CustomException("Page < 1", "UsersListAsync", "Page", 400, "Номер страницы меньше 1", "Получение списка пользователей");
		}

		var usersCount = await _hitsContext.User
			.Where(u =>
				(name == null || u.AccountName.Contains(name)) &&
				(mail == null || u.Mail.Contains(mail)) &&
				(rolesIds == null || rolesIds.Count < 1 || u.SystemRoles.Any(r => rolesIds.Contains(r.Id))))
			.CountAsync();

		var usersQuery = _hitsContext.User
			.Include(u => u.SystemRoles)
			.Include(u => u.IconFile)
			.Where(u =>
				(name == null || u.AccountName.Contains(name)) &&
				(mail == null || u.Mail.Contains(mail)) &&
				(rolesIds == null || rolesIds.Count < 1 || u.SystemRoles.Any(r => rolesIds.Contains(r.Id))));

		if (sort != null)
		{
			usersQuery = sort switch
			{
				UsersSortEnum.NameAsc => usersQuery.OrderBy(u => u.AccountName),
				UsersSortEnum.NameDesc => usersQuery.OrderByDescending(u => u.AccountName),
				UsersSortEnum.MailAsc => usersQuery.OrderBy(u => u.Mail),
				UsersSortEnum.MailDesc => usersQuery.OrderByDescending(u => u.Mail),
				UsersSortEnum.AccountNumberAsc => usersQuery.OrderBy(u => u.AccountNumber),
				UsersSortEnum.AccountNumberDesc => usersQuery.OrderByDescending(u => u.AccountNumber),
				_ => usersQuery
			};
		}

		var users = await usersQuery
			.Skip((page - 1) * num)
			.Take(num)
			.Select(u => new UserItemDTO
			{
				Id = u.Id,
				Mail = u.Mail,
				AccountName = u.AccountName,
				AccountTag = u.AccountTag,
				AccountNumber = u.AccountNumber,
				AccountCreateDate = u.AccountCreateDate,
				WhereCreator = _hitsContext.UserServer
					.Where(us => us.UserId == u.Id)
					.Where(us => us.SubscribeRoles
						.Any(sr => sr.Role.Role == RoleEnum.Creator))
					.Select(us => us.Server)
					.Distinct()
					.Select(s => new ServersShortListItemDTO
					{
						ServerId = s.Id,
						ServerName = s.Name,
						ServerType = s.ServerType,
						Icon = s.IconFile == null ? null : new FileMetaResponseDTO
						{
							FileId = s.IconFile.Id,
							FileName = s.IconFile.Name,
							FileType = s.IconFile.Type,
							FileSize = s.IconFile.Size,
							Deleted = s.IconFile.Deleted
						}
					})
					.ToList(),
				Notifiable = u.Notifiable,
				FriendshipApplication = u.FriendshipApplication,
				NonFriendMessage = u.NonFriendMessage,
				Icon = u.IconFile == null ? null : new FileMetaResponseDTO
				{
					FileId = u.IconFile.Id,
					FileName = u.IconFile.Name,
					FileType = u.IconFile.Type,
					FileSize = u.IconFile.Size,
					Deleted = u.IconFile.Deleted
				},
				SystemRoles = u.SystemRoles
					.Select(sr => new SystemRoleShortItemDTO
					{
						Id = sr.Id,
						Name = sr.Name,
						Type = sr.Type
					})
					.ToList()
			})
			.ToListAsync();

		var usersList = new UsersAdminListDTO
		{
			Users = users,
			Page = page,
			Number = num,
			PageCount = (usersCount + num - 1) / num,
			NumberCount = usersCount
		};

		var rolesString = "[";
		if (rolesIds != null)
		{
			foreach (var id in rolesIds)
			{
				rolesString += id.ToString() + ", ";
			}
		}
		rolesString += "]";

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Получение списка пользователей",
			OperationData = $"Админ {admin.AccountName} запросил список пользователей num {num}, page {page}, sort {sort}, name {name}, mail {mail}, rolesString {rolesString}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();

		return usersList;
	}

	public async Task<ChannelsAdminListDTO> DeletedChannelsListAsync(string token, int num, int page)
	{
		var admin = await GetAdminAsync(token);

		if (num < 1)
		{
			throw new CustomException("Num < 1", "DeletedChannelsListAsync", "Num", 400, "Количество элементов меньше 1", "Получение списка удаленных каналов");
		}

		if (page < 1)
		{
			throw new CustomException("Page < 1", "DeletedChannelsListAsync", "Page", 400, "Номер страницы меньше 1", "Получение списка удаленных каналов");
		}

		var chanCount = await _hitsContext.Channel
			.Where(c => ((TextChannelDbModel)c).DeleteTime != null)
			.CountAsync();

		if (chanCount == 0)
		{
			return (new ChannelsAdminListDTO
			{
				Channels = new List<TextChannelAdminItemDTO>(),
				Page = page,
				Number = num,
				PageCount = 0,
				NumberCount = 0
			});
		}

		if ((page - 1) * num >= chanCount)
		{
			throw new CustomException("Pagination error", "DeletedChannelsListAsync", "Pagination", 400, "Запрашиваются элементы превышающие их количество", "Получение списка удаленных каналов");
		}


		var channels = await _hitsContext.Channel
			.Include(c => c.Server)
			.Where(c => ((TextChannelDbModel)c).DeleteTime != null)
			.Skip((page - 1) * num)
			.Take(num)
			.Select( c => new TextChannelAdminItemDTO
			{
				ChannelId = c.Id,
				ChannelName = c.Name,
				ServerID = c.Server.Id,
				ServerName = c.Server.Name,
				DeleteTime = (DateTime)((TextChannelDbModel)c).DeleteTime
			})
			.ToListAsync();

		var channelsList = new ChannelsAdminListDTO
		{
			Channels = channels,
			Page = page,
			Number = num,
			PageCount = (chanCount + num - 1) / num,
			NumberCount = chanCount
		};

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Получение списка удаленных каналов",
			OperationData = $"Админ {admin.AccountName} запросил список удаленных каналов num {num}, page {page}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();

		return channelsList;
	}

	public async Task RewiveDeletedChannel(string token, Guid ChannelId)
	{
		var admin = await GetAdminAsync(token);

		var channel = await _hitsContext.Channel
			.FirstOrDefaultAsync(c => ((TextChannelDbModel)c).DeleteTime != null && c.Id == ChannelId);

		if (channel == null)
		{
			throw new CustomException("Channel not found", "RewiveDeletedChannel", "Channel", 404, "Канал не найден", "Восстановление удаленного канала");
		}

		((TextChannelDbModel)channel).DeleteTime = null;

		_hitsContext.Channel.Update(channel);
		await _hitsContext.SaveChangesAsync();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Восстановление удаленного канала",
			OperationData = $"Админ {admin.AccountName} восстановил канал с id {ChannelId}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<SystemRolesFullListDTO> RolesFullListAsync(string token)
	{
		var admin = await GetAdminAsync(token);

		var allRoles = await _hitsContext.SystemRole
			.Include(r => r.ChildRoles)
			.AsNoTracking()
			.ToListAsync();

		var roleDtos = allRoles.Select(r => new SystemRoleItemDTO
			{
				Id = r.Id,
				Name = r.Name,
				Type = r.Type
			})
			.ToList();

		var rolesDict = roleDtos.ToDictionary(r => r.Id);

		foreach (var dbRole in allRoles)
		{
			if (dbRole.ParentRoleId != null && rolesDict.TryGetValue(dbRole.ParentRoleId.Value, out var parentDto))
			{
				parentDto.ChildRoles.Add(rolesDict[dbRole.Id]);
			}
		}

		var rootRoles = allRoles
			.Where(r => r.ParentRoleId == null)
			.Select(r => rolesDict[r.Id])
			.ToList();

		var roles = new SystemRolesFullListDTO
		{
			Roles = allRoles
				.Where(r => r.ParentRoleId == null)
				.Select(r => rolesDict[r.Id])
				.ToList()
		};

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Получение полного списка ролей",
			OperationData = $"Админ {admin.AccountName} запросил полный список ролей системы",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();

		return roles;
	}

	public async Task<SystemRolesFullListDTO> RolesShortListAsync(string token, string? name)
	{
		var admin = await GetAdminAsync(token);

		var allRoles = await _hitsContext.SystemRole
			.Where(r => name == null || r.Name.Contains(name))
			.Select(r => new SystemRoleItemDTO
			{
				Id = r.Id,
				Name = r.Name,
				Type = r.Type
			})
			.ToListAsync();

		var roles = new SystemRolesFullListDTO
		{
			Roles = allRoles
		};

		return roles;
	}

	public async Task CreateSystemRoleAsync(string token, Guid ParentRoleId, string name)
	{
		var admin = await GetAdminAsync(token);

		var parentRole = await _hitsContext.SystemRole
			.FirstOrDefaultAsync(c => c.Id == ParentRoleId);

		if (parentRole == null)
		{
			throw new CustomException("Parent role not found", "CreateSystemRoleAsync", "Parent role", 404, "Родительская роль не найдена", "Создание системной роли");
		}

		var newSystemRole = new SystemRoleDbModel
		{
			Name = name,
			Type = parentRole.Type,
			ParentRoleId = parentRole.Id,
			ParentRole = parentRole,
			ChildRoles = new List<SystemRoleDbModel>(),
			Users = new List<UserDbModel>()
		};

		await _hitsContext.SystemRole.AddAsync(newSystemRole);
		await _hitsContext.SaveChangesAsync();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Создание системной роли",
			OperationData = $"Админ {admin.AccountName} создал системную роль name {name}, ParentRoleId {ParentRoleId}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task RenameSystemRoleAsync(string token, Guid RoleId, string name)
	{
		var admin = await GetAdminAsync(token);

		var role = await _hitsContext.SystemRole
			.FirstOrDefaultAsync(c => c.Id == RoleId);

		if (role == null)
		{
			throw new CustomException("Role not found", "RenameSystemRoleAsync", "Role", 404, "Роль не найдена", "Переименование системной роли");
		}

		role.Name = name;

		_hitsContext.SystemRole.Update(role);
		await _hitsContext.SaveChangesAsync();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Переименование системной роли",
			OperationData = $"Админ {admin.AccountName} переименовал системную роль RoleId {RoleId}, name {name}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task DeleteSubscribedRoleAsync(UserDbModel user, RoleDbModel role)
	{
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.FirstOrDefaultAsync(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id) && us.UserId == user.Id);
		if (userSub == null)
		{
			return;
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == role.ServerId).Select(us => us.UserId).ToListAsync();
		if (userSub.SubscribeRoles.Count() == 1)
		{
			var userVoiceChannel = await _hitsContext.UserVoiceChannel
				.Include(us => us.VoiceChannel)
				.FirstOrDefaultAsync(us =>
					us.VoiceChannel.ServerId == role.ServerId
					&& us.UserId == user.Id);
			if (userVoiceChannel != null)
			{
				_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
			}

			var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.UserId == user.Id && lr.TextChannel.ServerId == role.ServerId).ToListAsync();
			_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

			var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == userSub.Id).ToListAsync();
			_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

			_hitsContext.UserServer.Remove(userSub);
			await _hitsContext.SaveChangesAsync();

			var newUnsubscriberResponse = new UnsubscribeResponseDTO
			{
				ServerId = role.ServerId,
				UserId = user.Id,
			};
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
			}
		}
		else
		{
			var deletedRole = userSub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == role.Id);

			userSub.SubscribeRoles.Remove(deletedRole);
			_hitsContext.UserServer.Update(userSub);
			await _hitsContext.SaveChangesAsync();

			var removedChannels = await _hitsContext.ChannelCanSee
				.Where(ccs => ccs.RoleId == deletedRole.RoleId)
				.Select(ccs => ccs.ChannelId)
				.ToListAsync();
			foreach (var channelId in removedChannels)
			{
				bool stillHasAccess = await _hitsContext.ChannelCanSee
					.AnyAsync(ccs => removedChannels.Contains(ccs.ChannelId)
									 && userSub.SubscribeRoles.Select(sr => sr.RoleId).Contains(ccs.RoleId));

				if (!stillHasAccess)
				{
					var lastRead = await _hitsContext.LastReadChannelMessage
						.FirstOrDefaultAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channelId);

					if (lastRead != null)
						_hitsContext.LastReadChannelMessage.Remove(lastRead);
				}
			}
			await _hitsContext.SaveChangesAsync();

			var oldUserRole = new NewUserRoleResponseDTO
			{
				ServerId = role.ServerId,
				UserId = user.Id,
				RoleId = deletedRole.RoleId,
			};
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
			}
		}
	}

	public async Task DeleteSystemRoleAsync(string token, Guid RoleId)
	{
		var admin = await GetAdminAsync(token);

		var role = await _hitsContext.SystemRole
			.FirstOrDefaultAsync(c => c.Id == RoleId);

		if (role == null)
		{
			throw new CustomException("Role not found", "DeleteSystemRoleAsync", "Role", 404, "Роль не найдена", "Удаление системной роли");
		}

		var presets = await _hitsContext.Preset
			.Where(p => p.SystemRoleId == role.Id)
			.Select(p => p.ServerRoleId)
			.ToListAsync();

		var userServerRolePairs = await _hitsContext.UserServer
			.Include(us => us.User)
				.ThenInclude(u => u.SystemRoles)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(us =>
				us.User.SystemRoles.Any(sr => sr.Id == role.Id) &&
				us.SubscribeRoles.Any(sr => presets.Contains(sr.RoleId))
			)
			.SelectMany(us => us.SubscribeRoles
				.Where(sr => presets.Contains(sr.RoleId))
				.Select(sr => new
				{
					User = us.User,
					ServerRole = sr.Role
				})
			)
			.Distinct()
			.ToListAsync();

		foreach (var pair in userServerRolePairs)
		{
			await DeleteSubscribedRoleAsync(pair.User, pair.ServerRole);
		}

		_hitsContext.SystemRole.Remove(role);
		await _hitsContext.SaveChangesAsync();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Удаление системной роли",
			OperationData = $"Админ {admin.AccountName} удалил системную роль {role.Name} с id {role.Id}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task CreateSubscribedRoleAsync(UserDbModel user, RoleDbModel role)
	{
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.FirstOrDefaultAsync(us => us.ServerId == role.ServerId && us.UserId == user.Id);

		if (userSub != null && userSub.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
		{
			return;
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == role.ServerId).Select(us => us.UserId).ToListAsync();

		var channelsCanRead = await _hitsContext.ChannelCanSee
			.Include(ccs => ccs.Channel)
			.Where(ccs => (ccs.Channel is TextChannelDbModel || ccs.Channel is NotificationChannelDbModel || ccs.Channel is SubChannelDbModel)
				&& ccs.Channel.ServerId == role.ServerId
				&& ccs.RoleId == role.Id)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();

		if (userSub == null)
		{
			var newSub = new UserServerDbModel
			{
				Id = Guid.NewGuid(),
				UserId = user.Id,
				ServerId = role.ServerId,
				UserServerName = user.AccountName,
				IsBanned = false,
				NonNotifiable = false,
				SubscribeRoles = new List<SubscribeRoleDbModel>()
			};
			newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
			{
				UserServerId = newSub.Id,
				RoleId = role.Id
			});

			var lastReadedList = new List<LastReadChannelMessageDbModel>();
			if (channelsCanRead != null)
			{
				foreach (var channel in channelsCanRead)
				{
					lastReadedList.Add(new LastReadChannelMessageDbModel
					{
						UserId = user.Id,
						TextChannelId = channel,
						LastReadedMessageId = (
							await _hitsContext.ChannelMessage
							.Where(cm => cm.TextChannelId == channel)
							.Select(m => (long?)m.Id).MaxAsync() ?? 0
						)
					});
				}
			}

			await _hitsContext.UserServer.AddAsync(newSub);
			await _hitsContext.LastReadChannelMessage.AddRangeAsync(lastReadedList);
			await _hitsContext.SaveChangesAsync();

			var newSubscriberResponse = new ServerUserDTO
			{
				ServerId = role.ServerId,
				UserId = user.Id,
				UserName = user.AccountName,
				UserTag = user.AccountTag,
				Icon = null,
				Roles = new List<UserServerRoles>{
					new UserServerRoles
					{
						RoleId = role.Id,
						RoleName = role.Name,
						RoleType = role.Role
					}
				},
				Notifiable = user.Notifiable,
				FriendshipApplication = user.FriendshipApplication,
				NonFriendMessage = user.NonFriendMessage,
				isFriend = false,
				SystemRoles = user.SystemRoles
					.Select(sr => new SystemRoleShortItemDTO
					{
						Name = sr.Name,
						Type = sr.Type
					})
					.ToList()
			};
			if (user != null && user.IconFileId != null)
			{
				var userIcon = await GetImageAsync((Guid)user.IconFileId);
				newSubscriberResponse.Icon = userIcon;
			}

			var allFriends = await _hitsContext.Friendship
				.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
				.Select(f => f.UserIdFrom == user.Id ? f.UserIdTo : f.UserIdFrom)
				.Distinct()
				.ToListAsync();
			var friendsSet = new HashSet<Guid>(allFriends);
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				foreach (var alertedUser in alertedUsers)
				{
					newSubscriberResponse.isFriend = friendsSet.Contains(alertedUser);
					await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, new List<Guid> { alertedUser }, "New user on server");
				}
			}
		}
		else
		{
			userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
			{
				UserServerId = userSub.Id,
				RoleId = role.Id
			});

			_hitsContext.UserServer.Update(userSub);
			await _hitsContext.SaveChangesAsync();

			foreach (var channel in channelsCanRead)
			{
				bool alreadyExists = await _hitsContext.LastReadChannelMessage
					.AnyAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channel);

				if (!alreadyExists)
				{
					var lastMessageId = await _hitsContext.ChannelMessage
						.Where(m => m.TextChannelId == channel)
						.OrderByDescending(m => m.Id)
						.Select(m => (long?)m.Id)
						.FirstOrDefaultAsync() ?? 0;

					var lastRead = new LastReadChannelMessageDbModel
					{
						UserId = user.Id,
						TextChannelId = channel,
						LastReadedMessageId = lastMessageId
					};

					await _hitsContext.LastReadChannelMessage.AddAsync(lastRead);
				}
			}
			await _hitsContext.SaveChangesAsync();

			var newUserRole = new NewUserRoleResponseDTO
			{
				ServerId = role.ServerId,
				UserId = user.Id,
				RoleId = role.Id,
			};
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
			}
		}
	}

	public async Task AddSystemRoleAsync(string token, Guid RoleId, List<Guid> UsersIds)
	{
		var admin = await GetAdminAsync(token);

		var role = await _hitsContext.SystemRole
			.FirstOrDefaultAsync(c => c.Id == RoleId);

		if (role == null)
		{
			throw new CustomException("Role not found", "AddSystemRoleAsync", "Role", 404, "Роль не найдена", "Присвоение системной роли");
		}

		var users = await _hitsContext.User
			.Include(u => u.SystemRoles)
			.Where(u => UsersIds.Contains(u.Id)
				&& (u.SystemRoles.Any(sr => sr.Id != role.Id) || u.SystemRoles.Count == 0))
			.ToListAsync();

		if (users == null || users.Count == 0)
		{
			throw new CustomException("Users not found", "AddSystemRoleAsync", "UsersIds", 404, "Пользователи не найдены", "Присвоение системной роли");
		}

		var presets = await _hitsContext.Preset
			.Include(p => p.ServerRole)
			.Where(p => p.SystemRoleId == role.Id)
			.Select(p => p.ServerRole)
			.ToListAsync();

		if (presets != null && presets.Count > 0)
		{
			foreach (var serverRole in presets)
			{
				foreach (var user in users)
				{
					await CreateSubscribedRoleAsync(user, serverRole);
				}
			}
		}

		foreach (var user in users)
		{
			user.SystemRoles.Add(role);
		}
		_hitsContext.User.UpdateRange(users);
		await _hitsContext.SaveChangesAsync();

		var usersString = "[";
		if (UsersIds != null)
		{
			foreach (var id in UsersIds)
			{
				usersString += id.ToString() + ", ";
			}
		}
		usersString += "]";

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Добавление системной роли",
			OperationData = $"Админ {admin.AccountName} добавил системную роль {role.Name} с id {role.Id} пользователям с id {usersString}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task RemoveSystemRoleAsync(string token, Guid RoleId, Guid UserId)
	{
		var admin = await GetAdminAsync(token);

		var role = await _hitsContext.SystemRole
			.FirstOrDefaultAsync(c => c.Id == RoleId);

		if (role == null)
		{
			throw new CustomException("Role not found", "RemoveSystemRoleAsync", "Role", 404, "Роль не найдена", "Отвязка системной роли");
		}

		var user = await _hitsContext.User
			.Include(u => u.SystemRoles)
			.FirstOrDefaultAsync(u => u.Id == UserId && u.SystemRoles.Any(sr => sr.Id == role.Id));

		if (user == null)
		{
			throw new CustomException("User not found", "RemoveSystemRoleAsync", "UserId", 404, "Пользователь не найдены", "Отвязка системной роли");
		}

		var presetRolesToUnlink = await _hitsContext.Preset
			.Include(sp => sp.ServerRole)
			.Where(sp => sp.SystemRoleId == role.Id)
			.Select(sp => sp.ServerRole)
			.ToListAsync();

		foreach (var serverRole in presetRolesToUnlink)
		{
			await DeleteSubscribedRoleAsync(user, serverRole);
		}

		user.SystemRoles.Remove(role);
		_hitsContext.User.Update(user);
		await _hitsContext.SaveChangesAsync();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Изъятие системной роли",
			OperationData = $"Админ {admin.AccountName} изъял системную роль {role.Name} с id {role.Id} у пользователя {user.AccountName} с id {user.Id}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<AdminDbModel> CreateAccountOnce()
	{
		if (await _hitsContext.Admin.AnyAsync())
			return null;

		var newAdmin = new AdminDbModel
		{
			Id = Guid.NewGuid(),
			Login = "TetyaDusya",
			PasswordHash = _passwordHasher.HashPassword("TetyaDusya", "TetyaDusya"),
			AccountName = "Тётя Дуся",
			Approved = true
		};

		_hitsContext.Admin.Add(newAdmin);
		await _hitsContext.SaveChangesAsync();

		return newAdmin;
	}

	public async Task<FileResponseDTO> GetIconAsync(string token, Guid fileId)
	{
		await GetAdminAsync(token);

		var file = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == fileId);
		if (file == null)
		{
			throw new CustomException("File not found", "Get file", "File id", 404, "Файл не найден", "Получение файла");
		}

		if ((file.ServerId == null) && (file.UserId == null) && (file.ChatIcId == null))
		{
			throw new CustomException("File is not an icon", "Get file", "Icon", 400, "Файл не является иконкой", "Получение изображения");
		}

		if (string.IsNullOrWhiteSpace(file.Type) || !file.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
		{
			throw new CustomException("File is not an image", "Get file", "File type", 400, "Файл не является изображением", "Получение изображения");
		}

		try
		{
			await _minioService.StatFileAsync(file.Path);
		}
		catch (Minio.Exceptions.ObjectNotFoundException)
		{
			throw new CustomException("File not found in storage", "Get file", "Minio object", 404, "Файл не найден в хранилище", "Получение файла");
		}

		var fileBytes = await _minioService.GetFileAsync(file.Path);
		var base64File = Convert.ToBase64String(fileBytes);

		return new FileResponseDTO
		{
			FileId = file.Id,
			FileName = file.Name,
			FileType = file.Type,
			FileSize = file.Size,
			Base64File = base64File
		};
	}

	public async Task<OperationsListDTO> GetOperationHistoryAsync(string token, int num, int page)
	{
		var admin = await GetAdminAsync(token);

		if (num < 1)
		{
			throw new CustomException("Num < 1", "GetOperationHistoryAsync", "Num", 400, "Количество элементов меньше 1", "Получение истории операций админов");
		}

		if (page < 1)
		{
			throw new CustomException("Page < 1", "GetOperationHistoryAsync", "Page", 400, "Номер страницы меньше 1", "Получение истории операций админов");
		}

		var operationsCount = await _hitsContext.OperationsHistory.CountAsync();

		if (operationsCount == 0)
		{
			return (new OperationsListDTO
			{
				Operations = new List<OperationItemDTO>(),
				Page = page,
				Number = num,
				PageCount = 0,
				NumberCount = 0
			});
		}

		if ((page - 1) * num >= operationsCount)
		{
			throw new CustomException("Pagination error", "GetOperationHistoryAsync", "Pagination", 400, "Запрашиваются элементы превышающие их количество", "Получение истории операций админов");
		}


		var operations = await _hitsContext.OperationsHistory
			.Include(c => c.Admin)
			.OrderByDescending(c => c.OperationDate)
			.Skip((page - 1) * num)
			.Take(num)
			.Select(c => new OperationItemDTO
			{
				Id = c.Id,
				OpaerationTime = c.OperationDate,
				AdminName = c.Admin.AccountName,
				Operation = c.Operation,
				OperationData = c.OperationData
			})
			.ToListAsync();

		var operationsList = new OperationsListDTO
		{
			Operations = operations,
			Page = page,
			Number = num,
			PageCount = (operationsCount + num - 1) / num,
			NumberCount = operationsCount
		};

		return operationsList;
	}

	public async Task ChangeUserPasswordAsync(string token, Guid userId, string newPassword)
	{
		var admin = await GetAdminAsync(token);

		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);

		if (user == null)
		{
			throw new CustomException("User not found", "ChangeUserPasswordAsync", "UserId", 404, "Пользователь не найдены", "Изменение пароля пользователя");
		}

		user.PasswordHash = _passwordHasher.HashPassword(user.Mail, newPassword);
		_hitsContext.User.Update(user);
		await _hitsContext.SaveChangesAsync();

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Изменение пароля пользователя",
			OperationData = $"Админ {admin.AccountName} изменил пароль пользователя {user.AccountName} с id {user.Id}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();
	}

	public async Task<ServersAdminListDTO> GetServersListAsync(string token, int num, int page, string? name)
	{
		var admin = await GetAdminAsync(token);

		if (num < 1)
		{
			throw new CustomException("Num < 1", "GetServersListAsync", "Num", 400, "Количество элементов меньше 1", "Получение списка серверов");
		}

		if (page < 1)
		{
			throw new CustomException("Page < 1", "GetServersListAsync", "Page", 400, "Номер страницы меньше 1", "Получение списка серверов");
		}

		var serversCount = await _hitsContext.Server.CountAsync();

		if (serversCount == 0)
		{
			return (new ServersAdminListDTO
			{
				Servers = new List<ServerItemDTO>(),
				Page = page,
				Number = num,
				PageCount = 0,
				NumberCount = 0
			});
		}

		if ((page - 1) * num >= serversCount)
		{
			throw new CustomException("Pagination error", "GetServersListAsync", "Pagination", 400, "Запрашиваются элементы превышающие их количество", "Получение списка серверов");
		}


		var servers = await _hitsContext.Server
			.Include(s => s.Subscribtions)
			.Include(s => s.IconFile)
			.Where(s => (name == null || s.Name.Contains(name)))
			.OrderByDescending(s => s.ServerCreateDate)
			.Skip((page - 1) * num)
			.Take(num)
			.Select(s => new ServerItemDTO
			{
				Id = s.Id,
				ServerName = s.Name,
				ServerType = s.ServerType,
				UsersNumber = s.Subscribtions.Count,
				Icon = s.IconFile == null ? null : new FileMetaResponseDTO
				{
					FileId = s.IconFile.Id,
					FileName = s.IconFile.Name,
					FileType = s.IconFile.Type,
					FileSize = s.IconFile.Size,
					Deleted = s.IconFile.Deleted
				}
			})
			.ToListAsync();

		var serversList = new ServersAdminListDTO
		{
			Servers = servers,
			Page = page,
			Number = num,
			PageCount = (serversCount + num - 1) / num,
			NumberCount = serversCount
		};

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Получение списка серверов",
			OperationData = $"Админ {admin.AccountName} запросил список серверов num {num}, page {page}, name {name}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();

		return serversList;
	}

	public async Task<ServerAdminInfoDTO> GetServerDataAsync(string token, Guid ServerId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.Include(s => s.IconFile)
			.FirstOrDefaultAsync(s => s.Id == ServerId);

		if (server == null)
		{
			throw new CustomException("Server not found", "GetServerDataAsync", "ServerId", 404, "Сервер не найден", "Получение информации о сервере");
		}

		var serverUsers = await _hitsContext.UserServer
			.Include(us => us.User)
				.ThenInclude(u => u.IconFile)
			.Include(us => us.User)
				.ThenInclude(u => u.SystemRoles)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(us => us.ServerId == ServerId)
			.Select(us => new ServerUserAdminDTO
			{
				ServerId = us.ServerId,
				UserId = us.UserId,
				UserName = us.User.AccountName,
				UserServerName = us.UserServerName,
				UserTag = us.User.AccountTag,
				Icon = us.User.IconFile == null ? null : new FileMetaResponseDTO
				{
					FileId = us.User.IconFile.Id,
					FileName = us.User.IconFile.Name,
					FileType = us.User.IconFile.Type,
					FileSize = us.User.IconFile.Size,
					Deleted = us.User.IconFile.Deleted
				},
				IsBanned = us.IsBanned,
				BanReason = us.BanReason,
				BanTime = us.BanTime,
				NonNotifiable = us.NonNotifiable,
				Roles = us.SubscribeRoles
					.Select(sr => new UserServerRoleAdminDTO
					{
						RoleId = sr.Role.Id,
						RoleName = sr.Role.Name,
						RoleType = sr.Role.Role,
						Colour = sr.Role.Color
					})
					.ToList(),
				SystemRoles = us.User.SystemRoles
					.Select(sr => new SystemRoleShortItemDTO
					{
						Id = null,
						Name = sr.Name,
						Type = sr.Type
					})
					.ToList()
			})
			.ToListAsync();

		var roles = await _hitsContext.Role
			.Include(r => r.ChannelCanSee)
				.ThenInclude(c => c.Channel)
			.Include(r => r.ChannelCanWrite)
				.ThenInclude(c => c.TextChannel)
			.Include(r => r.ChannelCanWriteSub)
				.ThenInclude(c => c.TextChannel)
			.Include(r => r.ChannelNotificated)
				.ThenInclude(c => c.NotificationChannel)
			.Include(r => r.ChannelCanUse)
				.ThenInclude(c => c.SubChannel)
			.Include(r => r.ChannelCanJoin)
				.ThenInclude(c => c.VoiceChannel)
			.Where(r => r.ServerId == ServerId)
			.Select(r => new RolesAdminItemDTO
			{
				Id = r.Id,
				Name = r.Name,
				Tag = r.Tag,
				Color = r.Color,
				Type = r.Role,
				Permissions = new SettingsDTO
				{
					CanChangeRole = r.ServerCanChangeRole,
					CanWorkChannels = r.ServerCanWorkChannels,
					CanDeleteUsers = r.ServerCanDeleteUsers,
					CanMuteOther = r.ServerCanMuteOther,
					CanDeleteOthersMessages = r.ServerCanDeleteOthersMessages,
					CanIgnoreMaxCount = r.ServerCanIgnoreMaxCount,
					CanCreateRoles = r.ServerCanCreateRoles,
					CanCreateLessons = r.ServerCanCreateLessons,
					CanCheckAttendance = r.ServerCanCheckAttendance
				},
				ChannelCanSee = r.ChannelCanSee
					.Where(c => !(c.Channel is TextChannelDbModel) || ((TextChannelDbModel)c.Channel).DeleteTime == null)
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.ChannelId,
						Name = c.Channel.Name
					})
					.ToList(),
				ChannelCanWrite = r.ChannelCanWrite
					.Where(c => c.TextChannel.DeleteTime == null)
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.TextChannelId,
						Name = c.TextChannel.Name
					})
					.ToList(),
				ChannelCanWriteSub = r.ChannelCanWriteSub
					.Where(c => c.TextChannel.DeleteTime == null)
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.TextChannelId,
						Name = c.TextChannel.Name
					})
					.ToList(),
				ChannelNotificated = r.ChannelNotificated
					.Where(c => c.NotificationChannel.DeleteTime == null)
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.NotificationChannelId,
						Name = c.NotificationChannel.Name
					})
					.ToList(),
				ChannelCanUse = r.ChannelCanUse
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.SubChannelId,
						Name = c.SubChannel.Name
					})
					.ToList(),
				ChannelCanJoin = r.ChannelCanJoin
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.VoiceChannelId,
						Name = c.VoiceChannel.Name
					})
					.ToList()
			})
			.ToListAsync();

		var presets = await _hitsContext.Preset
			.Include(p => p.ServerRole)
			.Include(p => p.SystemRole)
			.Where(p => p.ServerRole.ServerId == server.Id)
			.Select(p => new ServerPresetItemDTO
			{
				ServerRoleId = p.ServerRoleId,
				ServerRoleName = p.ServerRole.Name,
				SystemRoleId = p.SystemRoleId,
				SystemRoleName = p.SystemRole.Name,
				SystemRoleType = p.SystemRole.Type
			})
			.ToListAsync();

		var systemRoles = await _hitsContext.SystemRole
			.Select(r => new SystemRoleItemDTO
			{
				Id = r.Id,
				Name = r.Name,
				Type = r.Type
			})
			.ToListAsync();

		var voiceChannelResponses = await _hitsContext.VoiceChannel
			.Include(vc => vc.Users)
			.Where(vc => vc.ServerId == server.Id && EF.Property<string>(vc, "ChannelType") == "Voice")
			.Select(vc => new VoiceChannelAdminResponseDTO
			{
				ChannelName = vc.Name,
				ChannelId = vc.Id,
				MaxCount = vc.MaxCount
			})
			.ToListAsync();

		var pairVoiceChannelResponses = await _hitsContext.PairVoiceChannel
			.Include(vc => vc.Users)
			.Include(vc => vc.ChannelCanSee)
			.Include(vc => vc.ChannelCanJoin)
			.Where(vc => vc.ServerId == server.Id && EF.Property<string>(vc, "ChannelType") == "PairVoice")
			.Select(vc => new VoiceChannelAdminResponseDTO
			{
				ChannelName = vc.Name,
				ChannelId = vc.Id,
				MaxCount = vc.MaxCount
			})
			.ToListAsync();

		var textChannelResponses = await _hitsContext.TextChannel
			.Include(t => t.Messages)
			.Where(t => t.ServerId == server.Id && EF.Property<string>(t, "ChannelType") == "Text" && t.DeleteTime == null)
			.Select(t => new TextChannelAdminResponseDTO
			{
				ChannelName = t.Name,
				ChannelId = t.Id,
				MessagesNumber = t.Messages.Count
			})
			.ToListAsync();

		var notificationChannelResponses = await _hitsContext.NotificationChannel
			.Include(t => t.Messages)
			.Where(t => t.ServerId == server.Id && EF.Property<string>(t, "ChannelType") == "Notification" && t.DeleteTime == null)
			.Select(t => new TextChannelAdminResponseDTO
			{
				ChannelName = t.Name,
				ChannelId = t.Id,
				MessagesNumber = t.Messages.Count
			})
			.ToListAsync();

		var response = new ServerAdminInfoDTO
		{
			ServerId = server.Id,
			ServerName = server.Name,
			ServerType = server.ServerType,
			Icon = server.IconFile == null ? null : new FileMetaResponseDTO
			{
				FileId = server.IconFile.Id,
				FileName = server.IconFile.Name,
				FileType = server.IconFile.Type,
				FileSize = server.IconFile.Size,
				Deleted = server.IconFile.Deleted
			},
			IsClosed = server.IsClosed,
			Users = serverUsers,
			Roles = roles,
			SystemRoles = systemRoles,
			Presets = presets,
			Channels = new ChannelAdminListDTO
			{
				TextChannels = textChannelResponses,
				NotificationChannels = notificationChannelResponses,
				VoiceChannels = voiceChannelResponses,
				PairVoiceChannels = pairVoiceChannelResponses
			}
		};

		var newOperation = new AdminOperationsHistoryDbModel
		{
			Operation = "Получение информации о сервере",
			OperationData = $"Админ {admin.AccountName} запросил информацию о сервере {server.Name} с id {server.Id}",
			AdminId = admin.Id,
		};
		await _hitsContext.OperationsHistory.AddAsync(newOperation);
		await _hitsContext.SaveChangesAsync();

		return response;
	}



	public async Task AddUserAsync(string token, string Mail, string Name, string Password, IFormFile? iconFile)
	{
		var admin = await GetAdminAsync(token);

		if (await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == Mail) != null)
		{
			throw new CustomException("Account with this mail already exist", "Create user", "Mail", 400, "Аккаунт с такой почтой уже существует", "Создание пользователя");
		}

		var count = (await _hitsContext.User.Select(u => (int?)u.AccountNumber).MaxAsync() ?? 0) + 1;

		string formattedNumber = count.ToString("D6");

		if (formattedNumber.Length > 6)
		{
			formattedNumber = formattedNumber.Substring(formattedNumber.Length - 6);
		}

		var studentRole = await _hitsContext.SystemRole.FirstOrDefaultAsync(sr => sr.ParentRoleId == null && sr.Type == SystemRoleTypeEnum.Student);
		if (studentRole == null)
		{
			throw new CustomException("Student role not found", "Create user", "Student role", 404, "Стартовая роль не найдена", "Создание пользователя");
		}

		var newUser = new UserDbModel
		{
			Mail = Mail,
			PasswordHash = _passwordHasher.HashPassword(Mail, Password),
			AccountName = Name,
			AccountTag = Regex.Replace(Transliteration.CyrillicToLatin(Name, Language.Russian), "[^a-zA-Z0-9]", "").ToLower() + "#" + formattedNumber,
			AccountNumber = count,
			Notifiable = true,
			FriendshipApplication = true,
			NonFriendMessage = true,
			NotificationLifeTime = 4,
			SystemRoles = new List<SystemRoleDbModel>()
		};
		newUser.SystemRoles.Add(studentRole);

		if (iconFile != null)
		{
			if (iconFile.Length > 10 * 1024 * 1024)
			{
				throw new CustomException("Icon too large", "Create user", "Icon", 400, "Файл слишком большой (макс. 10 МБ)", "Создание пользователя");
			}

			if (!iconFile.ContentType.StartsWith("image/"))
			{
				throw new CustomException("Invalid file type", "Create user", "Icon", 400, "Файл не является изображением!", "Создание пользователя");
			}

			byte[] fileBytes;
			using (var ms = new MemoryStream())
			{
				await iconFile.CopyToAsync(ms);
				fileBytes = ms.ToArray();
			}

			var scanResult = await _clamService.ScanFileAsync(fileBytes);
			if (scanResult.Result != ClamScanResults.Clean)
			{
				throw new CustomException("Virus detected", "Create user", "Icon", 400, "Обнаружен вирус в файле", "Создание пользователя");
			}

			using var imgStream = new MemoryStream(fileBytes);
			SixLabors.ImageSharp.Image image;
			try
			{
				image = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
			}
			catch (SixLabors.ImageSharp.UnknownImageFormatException)
			{
				throw new CustomException("Invalid image file", "Create user", "Icon", 400, "Файл не является валидным изображением!", "Создание пользователя");
			}

			if (image.Width > 650 || image.Height > 650)
			{
				throw new CustomException("Icon too large", "Create user", "Icon", 400, "Изображение слишком большое (макс. 650x650)", "Создание пользователя");
			}

			var originalFileName = Path.GetFileName(iconFile.FileName);
			var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
			var objectName = $"icons/{safeFileName}";

			await _minioService.UploadFileAsync(objectName, fileBytes, iconFile.ContentType);

			var file = new FileDbModel
			{
				Id = Guid.NewGuid(),
				Path = objectName,
				Name = originalFileName,
				Type = iconFile.ContentType,
				Size = iconFile.Length,
				Creator = newUser.Id,
				IsApproved = true,
				CreatedAt = DateTime.UtcNow,
				Deleted = false,
				UserId = newUser.Id
			};

			await _hitsContext.User.AddAsync(newUser);
			_hitsContext.SaveChanges();

			_hitsContext.File.Add(file);
			await _hitsContext.SaveChangesAsync();

			newUser.IconFileId = file.Id;
			_hitsContext.User.Update(newUser);
			await _hitsContext.SaveChangesAsync();
		}
		else
		{
			await _hitsContext.User.AddAsync(newUser);
			_hitsContext.SaveChanges();
		}
	}







	public async Task ChangeServerDataAsync(string token, Guid serverId, string? serverName, ServerTypeEnum? serverType, bool? serverClosed, Guid? newCreatorId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "ChangeServerDataAsync", "ServerId", 404, "Сервер не найден", "Изменение информации о сервере");
		}

		server.Name = serverName != null ? serverName : server.Name;
		server.ServerType = (ServerTypeEnum)(serverType != null ? serverType : server.ServerType);
		server.IsClosed = (bool)(serverClosed != null ? serverClosed : server.IsClosed);

		_hitsContext.Server.Update(server);

		if (newCreatorId != null)
		{
			var oldCreator = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
				.FirstOrDefaultAsync(us => (us.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator)) && us.ServerId == server.Id);
			if (oldCreator == null)
			{
				throw new CustomException("OldCreator not found", "ChangeServerDataAsync", "oldCreator", 404, "Нынешний создатель не найден", "Изменение информации о сервере");
			}

			var adminRole = await _hitsContext.Role.FirstOrDefaultAsync(r => (r.Role == RoleEnum.Admin) && r.ServerId == server.Id);
			if (adminRole == null)
			{
				throw new CustomException("Admin role not found", "ChangeServerDataAsync", "adminRole", 404, "Роль админа не найдена", "Изменение информации о сервере");
			}

			var newCreator = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
					.ThenInclude(sr => sr.Role)
				.FirstOrDefaultAsync(us => !(us.SubscribeRoles.Any(sr => sr.Role.Role == RoleEnum.Creator)) && us.ServerId == server.Id && us.UserId == newCreatorId);
			if (newCreator == null)
			{
				throw new CustomException("NewCreator not found", "ChangeServerDataAsync", "newCreator", 404, "Новый создатель не найден", "Изменение информации о сервере");
			}

			var creatorRole = await _hitsContext.Role.FirstOrDefaultAsync(r => (r.Role == RoleEnum.Creator) && r.ServerId == server.Id);
			if (creatorRole == null)
			{
				throw new CustomException("Creator role not found", "ChangeServerDataAsync", "creatorRole", 404, "Роль создателя не найдена", "Изменение информации о сервере");
			}

			var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

			var removed = oldCreator.SubscribeRoles.FirstOrDefault(sr => sr.RoleId == creatorRole.Id);
			oldCreator.SubscribeRoles.Remove(removed);
			oldCreator.SubscribeRoles.Add(new SubscribeRoleDbModel { UserServerId = oldCreator.Id, RoleId = adminRole.Id });
			var newAdminRole = new NewUserRoleResponseDTO
			{
				ServerId = serverId,
				UserId = oldCreator.UserId,
				RoleId = adminRole.Id,
			};
			var removedCreatorRole = new NewUserRoleResponseDTO
			{
				ServerId = serverId,
				UserId = oldCreator.UserId,
				RoleId = creatorRole.Id,
			};
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(removedCreatorRole, alertedUsers, "Role removed from user");
				await _webSocketManager.BroadcastMessageAsync(newAdminRole, alertedUsers, "Role added to user");
			}
			foreach (var sr in newCreator.SubscribeRoles)
			{
				var removedNewCreatorRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = newCreator.UserId,
					RoleId = sr.RoleId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(removedNewCreatorRole, alertedUsers, "Role removed from user");
				}
			}
			newCreator.SubscribeRoles.Clear();
			newCreator.SubscribeRoles.Add(new SubscribeRoleDbModel { UserServerId = newCreator.Id, RoleId = creatorRole.Id });
			var newCreatorRole = new NewUserRoleResponseDTO
			{
				ServerId = serverId,
				UserId = newCreator.UserId,
				RoleId = creatorRole.Id,
			};
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(newCreatorRole, alertedUsers, "Role added to user");
			}
			_hitsContext.UserServer.Update(oldCreator);
			_hitsContext.UserServer.Update(newCreator);
		}
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeServerIconAdminAsync(string token, Guid serverId, IFormFile iconFile)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "ChangeServerIconAdminAsync", "ServerId", 404, "Сервер не найден", "Изменение иконки сервера админом");
		}

		if (iconFile.Length > 10 * 1024 * 1024)
		{
			throw new CustomException("Icon too large", "ChangeServerIconAdminAsync", "Icon", 400, "Файл слишком большой (макс. 10 МБ)", "Изменение иконки сервера админом");
		}

		if (!iconFile.ContentType.StartsWith("image/"))
		{
			throw new CustomException("Invalid file type", "ChangeServerIconAdminAsync", "Icon", 400, "Файл не является изображением!", "Изменение иконки сервера админом");
		}

		byte[] fileBytes;
		using (var ms = new MemoryStream())
		{
			await iconFile.CopyToAsync(ms);
			fileBytes = ms.ToArray();
		}

		var scanResult = await _clamService.ScanFileAsync(fileBytes);
		if (scanResult.Result != ClamScanResults.Clean)
		{
			throw new CustomException("Virus detected", "ChangeServerIconAdminAsync", "Icon", 400, "Обнаружен вирус в файле", "Изменение иконки сервера админом");
		}

		using var imgStream = new MemoryStream(fileBytes);
		SixLabors.ImageSharp.Image image;
		try
		{
			image = await SixLabors.ImageSharp.Image.LoadAsync(imgStream);
		}
		catch (SixLabors.ImageSharp.UnknownImageFormatException)
		{
			throw new CustomException("Invalid image file", "ChangeServerIconAdminAsync", "Icon", 400, "Файл не является валидным изображением!", "Изменение иконки сервера админом");
		}

		if (image.Width > 650 || image.Height > 650)
		{
			throw new CustomException("Icon too large", "ChangeServerIconAdminAsync", "Icon", 400, "Изображение слишком большое (макс. 650x650)", "Изменение иконки сервера админом");
		}

		var originalFileName = Path.GetFileName(iconFile.FileName);
		var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
		var objectName = $"icons/{safeFileName}";

		await _minioService.UploadFileAsync(objectName, fileBytes, iconFile.ContentType);
		if (server.IconFileId != null)
		{
			var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == server.IconFileId);
			if (oldIcon != null)
			{
				try
				{
					await _minioService.DeleteFileAsync(oldIcon.Path);
				}
				catch
				{
				}
				_hitsContext.File.Remove(oldIcon);
				await _hitsContext.SaveChangesAsync();
			}
		}

		var file = new FileDbModel
		{
			Id = Guid.NewGuid(),
			Path = objectName,
			Name = originalFileName,
			Type = iconFile.ContentType,
			Size = iconFile.Length,
			Creator = admin.Id,
			IsApproved = true,
			CreatedAt = DateTime.UtcNow,
			Deleted = false,
			ServerId = server.Id
		};

		_hitsContext.File.Add(file);
		await _hitsContext.SaveChangesAsync();

		server.IconFileId = file.Id;
		_hitsContext.Server.Update(server);
		await _hitsContext.SaveChangesAsync();

		string base64Icon = Convert.ToBase64String(fileBytes);
		var changeIconDto = new ServerIconResponseDTO
		{
			ServerId = server.Id,
			Icon = new FileMetaResponseDTO
			{
				FileId = file.Id,
				FileName = file.Name,
				FileType = file.Type,
				FileSize = file.Size,
				Deleted = file.Deleted
			}
		};

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Any())
		{
			await _webSocketManager.BroadcastMessageAsync(changeIconDto, alertedUsers, "New icon on server");
		}
	}

	public async Task DeleteServerIconAdminAsync(string token, Guid serverId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "DeleteServerIconAdminAsync", "ServerId", 404, "Сервер не найден", "Удаление иконки сервера админом");
		}

		if (server.IconFileId == null)
		{
			throw new CustomException("Server has not icon", "DeleteServerIconAdminAsync", "Icon", 404, "Сервер не имеет иконки", "Удаление иконки сервера админом");
		}

		var oldIcon = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == server.IconFileId);
		if (oldIcon == null)
		{
			throw new CustomException("Old icon not found", "DeleteServerIconAdminAsync", "oldIconId", 404, "Иконка не найдена", "Удаление иконки сервера админом");
		}

		try
		{
			await _minioService.DeleteFileAsync(oldIcon.Path);
		}
		catch
		{
		}
		_hitsContext.File.Remove(oldIcon);
		await _hitsContext.SaveChangesAsync();

		var changeIconDto = new ServerIconResponseDTO
		{
			ServerId = server.Id,
			Icon = null
		};

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Any())
		{
			await _webSocketManager.BroadcastMessageAsync(changeIconDto, alertedUsers, "Server icon deleted");
		}
	}

	public async Task DeleteServerAdminAsync(Guid serverId, string token)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "DeleteServerAdminAsync", "ServerId", 404, "Сервер не найден", "Удаление сервера админом");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

		var userServerRelations = _hitsContext.UserServer.Where(us => us.ServerId == server.Id);
		var serverRoles = _hitsContext.Role.Where(r => r.ServerId == server.Id);

		var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.TextChannel.ServerId == server.Id).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

		var nonNitifiables = await _hitsContext.NonNotifiableChannel.Include(nnc => nnc.UserServer).Where(nnc => nnc.UserServer.ServerId == server.Id).ToListAsync();
		_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

		_hitsContext.UserServer.RemoveRange(userServerRelations);
		_hitsContext.Role.RemoveRange(serverRoles);

		var voiceChannels = server.Channels.OfType<VoiceChannelDbModel>().ToList();
		var pairVoiceChannels = server.Channels.OfType<PairVoiceChannelDbModel>().ToList();
		foreach (var voiceChannel in voiceChannels)
		{
			voiceChannel.Users.Clear();
		}
		foreach (var pairVoiceChannel in pairVoiceChannels)
		{
			pairVoiceChannel.Users.Clear();
		}
		var channelsToDelete = server.Channels.ToList();
		var applications = await _hitsContext.ServerApplications.Where(sa => sa.ServerId == server.Id).ToListAsync();
		_hitsContext.Channel.RemoveRange(channelsToDelete);
		_hitsContext.ServerApplications.RemoveRange(applications);

		if (server.IconFileId != null)
		{
			var iconFile = await _hitsContext.File.FirstOrDefaultAsync(f => f.Id == server.IconFileId);
			if (iconFile != null)
			{
				try
				{
					await _minioService.DeleteFileAsync(iconFile.Path.TrimStart('/'));
				}
				catch (Exception ex)
				{
				}

				// Удаляем запись из базы
				_hitsContext.File.Remove(iconFile);
			}
		}

		_hitsContext.Server.Remove(server);
		await _hitsContext.SaveChangesAsync();

		await _hitsContext.ChannelMessage
			.Where(m => m.TextChannel.ServerId == server.Id)
			.ExecuteUpdateAsync(setters => setters
				.SetProperty(m => m.DeleteTime, _ => DateTime.UtcNow.AddDays(21)));

		var serverDelete = new ServerDeleteDTO
		{
			ServerName = server.Name,
			ServerId = serverId
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(serverDelete, alertedUsers, "Server deleted");
		}
	}

	public async Task<RolesItemDTO> CreateRoleAdminAsync(string token, Guid serverId, string roleName, string color)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "CreateRoleAdminAsync", "ServerId", 404, "Сервер не найден", "Создание роли админом");
		}

		if (!Regex.IsMatch(color, "^#([A-Fa-f0-9]{6})$"))
		{
			throw new CustomException("Invalid color format", "CreateRoleAdminAsync", "Color", 400, "Неверный формат цвета. Используйте шестизначный HEX в формате #RRGGBB", "Создание роли админом");
		}

		var newRole = new RoleDbModel()
		{
			Name = roleName,
			Role = RoleEnum.Custom,
			ServerId = server.Id,
			Color = color,
			Tag = Regex.Replace(Transliteration.CyrillicToLatin(roleName, Language.Russian), "[^a-zA-Z0-9]", "").ToLower(),
			ServerCanChangeRole = false,
			ServerCanWorkChannels = false,
			ServerCanDeleteUsers = false,
			ServerCanMuteOther = false,
			ServerCanDeleteOthersMessages = false,
			ServerCanIgnoreMaxCount = false,
			ServerCanCreateRoles = false,
			ServerCanCreateLessons = false,
			ServerCanCheckAttendance = false,
			ChannelCanSee = new List<ChannelCanSeeDbModel>(),
			ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
			ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>(),
			ChannelNotificated = new List<ChannelNotificatedDbModel>(),
			ChannelCanUse = new List<ChannelCanUseDbModel>(),
			ChannelCanJoin = new List<ChannelCanJoinDbModel>(),
		};

		await _hitsContext.Role.AddAsync(newRole);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new RolesItemDTO
		{
			Id = newRole.Id,
			ServerId = newRole.ServerId,
			Name = newRole.Name,
			Tag = newRole.Tag,
			Color = newRole.Color,
			Type = newRole.Role
		};

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "New role");
		}

		return roleResponse;
	}

	public async Task DeleteRoleAdminAsync(string token, Guid serverId, Guid roleId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "DeleteRoleAdminAsync", "ServerId", 404, "Сервер не найден", "Удаление роли админом");
		}
		var role = await _hitsContext.Role
			.Include(r => r.ChannelCanSee)
			.Include(r => r.ChannelCanWrite)
			.Include(r => r.ChannelCanWriteSub)
			.Include(r => r.ChannelNotificated)
			.Include(r => r.ChannelCanUse)
			.Include(r => r.ChannelCanJoin)
			.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId);
		if (role == null)
		{
			throw new CustomException("Role not found", "DeleteRoleAdminAsync", "RoleId", 404, "Роль не найдена", "Удаление роли админом");
		}

		if (role.Role != RoleEnum.Custom)
		{
			throw new CustomException("Cant delete non custom role", "DeleteRoleAdminAsynce", "Role id", 400, "Нельзя удалить не пользовательскую роль", "Удаление роли админом");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

		var userServers = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanSee)
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
					.ThenInclude(r => r.ChannelCanUse)
			.Where(us => us.ServerId == server.Id && us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();

		if (userServers != null && userServers.Count() > 0)
		{
			var uncertainRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Role == RoleEnum.Uncertain && r.ServerId == server.Id);
			if (uncertainRole == null)
			{
				throw new CustomException("Uncertain role not found", "DeleteRoleAdminAsync", "Role id", 404, "Не найдена неопределенная роль", "Удаление роли админом");
			}
			var textChannels = await _hitsContext.TextChannel
				.Include(tc => tc.ChannelCanSee)
				.Where(tc => tc.ChannelCanSee.Any(ccs => ccs.RoleId == uncertainRole.Id) && EF.Property<string>(tc, "ChannelType") == "Text")
				.Select(tc => tc.Id)
				.ToListAsync();
			var notificationsChannels = await _hitsContext.NotificationChannel
				.Include(tc => tc.ChannelCanSee)
				.Where(tc => tc.ChannelCanSee.Any(ccs => ccs.RoleId == uncertainRole.Id))
				.Select(tc => tc.Id)
				.ToListAsync();
			var subChannels = await _hitsContext.SubChannel
				.Include(tc => tc.ChannelCanUse)
				.Where(tc => tc.ChannelCanUse.Any(ccs => ccs.RoleId == uncertainRole.Id))
				.Select(tc => tc.Id)
				.ToListAsync();
			var allChannels = textChannels
				.Concat(notificationsChannels)
				.Concat(subChannels)
				.Distinct()
				.ToList();

			var channelLastMessageIds = await _hitsContext.ChannelMessage
				.Where(m => allChannels.Contains((Guid)m.TextChannelId))
				.GroupBy(m => m.TextChannelId)
				.Select(g => new
				{
					ChannelId = g.Key,
					LastMessageId = g.Max(m => m.Id)
				})
				.ToDictionaryAsync(x => x.ChannelId, x => x.LastMessageId);

			foreach (var user in userServers)
			{

				var channelsThroughThisRole = user.SubscribeRoles
					.Where(sr => sr.RoleId == role.Id)
					.SelectMany(sr => sr.Role.ChannelCanSee.Select(ccs => ccs.ChannelId)
						.Concat(sr.Role.ChannelCanUse.Select(ccu => ccu.SubChannelId)))
					.Distinct()
					.ToList();

				var channelsThroughOtherRoles = user.SubscribeRoles
					.Where(sr => sr.RoleId != role.Id)
					.SelectMany(sr => sr.Role.ChannelCanSee.Select(ccs => ccs.ChannelId)
						.Concat(sr.Role.ChannelCanUse.Select(ccu => ccu.SubChannelId)))
					.Distinct()
					.ToList();

				var onlyThroughThisRole = channelsThroughThisRole
					.Except(channelsThroughOtherRoles)
					.ToList();

				await _hitsContext.LastReadChannelMessage
					.Where(lr => lr.UserId == user.UserId && onlyThroughThisRole.Contains(lr.TextChannelId))
					.ExecuteDeleteAsync();

				if (user.SubscribeRoles.Count() == 1)
				{
					await _hitsContext.SubscribeRole.AddAsync(new SubscribeRoleDbModel { UserServerId = user.Id, RoleId = uncertainRole.Id });
					await _hitsContext.SaveChangesAsync();

					await _webSocketManager.BroadcastMessageAsync(new NewUserRoleResponseDTO
					{
						ServerId = serverId,
						UserId = user.UserId,
						RoleId = uncertainRole.Id,
					}, alertedUsers, "Role added to user");

					var lastReads = await _hitsContext.LastReadChannelMessage
						.Where(lrcm => lrcm.UserId == user.UserId)
						.Select(lrcm => lrcm.TextChannelId)
						.ToArrayAsync();

					var missingChannels = allChannels.Except(lastReads).ToList();
					if (missingChannels.Count > 0)
					{
						var newLastReads = missingChannels.Select(chId => new LastReadChannelMessageDbModel
						{
							UserId = user.UserId,
							TextChannelId = chId,
							LastReadedMessageId = channelLastMessageIds.ContainsKey(chId)
								? channelLastMessageIds[chId]
								: 0
						}).ToList();

						await _hitsContext.LastReadChannelMessage.AddRangeAsync(newLastReads);
						await _hitsContext.SaveChangesAsync();
					}
				}

				var subRole = user.SubscribeRoles.FirstOrDefault(sr => sr.RoleId == role.Id);

				_hitsContext.SubscribeRole.Remove(subRole);
				await _hitsContext.SaveChangesAsync();

				await _webSocketManager.BroadcastMessageAsync(new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = user.UserId,
					RoleId = role.Id,
				}, alertedUsers, "Role removed from user");
			}
		}

		_hitsContext.Role.Remove(role);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new DeleteRoleResposeDTO
		{
			ServerId = role.ServerId,
			RoleId = role.Id,
		};

		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Deleted role");
		}
	}

	public async Task UpdateRoleAsync(string token, Guid serverId, Guid roleId, string name, string color)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "UpdateRoleAsync", "ServerId", 404, "Сервер не найден", "Обновление роли админом");
		}
		var role = await _hitsContext.Role
			.Include(r => r.ChannelCanSee)
			.Include(r => r.ChannelCanWrite)
			.Include(r => r.ChannelCanWriteSub)
			.Include(r => r.ChannelNotificated)
			.Include(r => r.ChannelCanUse)
			.Include(r => r.ChannelCanJoin)
			.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId);
		if (role == null)
		{
			throw new CustomException("Role not found", "UpdateRoleAsync", "RoleId", 404, "Роль не найдена", "Обновление роли админом");
		}

		if (!Regex.IsMatch(color, "^#([A-Fa-f0-9]{6})$"))
		{
			throw new CustomException("Invalid color format", "UpdateRoleAsync", "Color", 400, "Неверный формат цвета. Используйте шестизначный HEX в формате #RRGGBB", "Обновление роли");
		}

		if (role.Role == RoleEnum.Custom)
		{
			role.Name = name;
			role.Tag = Regex.Replace(Transliteration.CyrillicToLatin(name, Language.Russian), "[^a-zA-Z0-9]", "").ToLower();
		}
		role.Color = color;

		_hitsContext.Role.Update(role);
		await _hitsContext.SaveChangesAsync();

		var roleResponse = new RolesItemDTO
		{
			Id = role.Id,
			ServerId = role.ServerId,
			Name = role.Name,
			Tag = role.Tag,
			Color = role.Color,
			Type = role.Role
		};

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Updated role");
		}
	}

	public async Task ChangeRoleSettingsAdminAsync(string token, Guid serverId, Guid roleId, SettingsEnum setting, bool settingsData)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "ChangeRoleSettingsAdminAsync", "ServerId", 404, "Сервер не найден", "Изменение настроек роли админом");
		}
		var role = await _hitsContext.Role
			.Include(r => r.ChannelCanSee)
			.Include(r => r.ChannelCanWrite)
			.Include(r => r.ChannelCanWriteSub)
			.Include(r => r.ChannelNotificated)
			.Include(r => r.ChannelCanUse)
			.Include(r => r.ChannelCanJoin)
			.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId);
		if (role == null)
		{
			throw new CustomException("Role not found", "ChangeRoleSettingsAdminAsync", "RoleId", 404, "Роль не найдена", "Изменение настроек роли админом");
		}

		if (role.Role != RoleEnum.Custom)
		{
			throw new CustomException("This role not custom", "ChangeRoleSettingsAdminAsync", "Role", 400, "Нельзя менять настройки предсозданных ролей", "Изменение настроек роли админом");
		}

		switch (setting)
		{
			case SettingsEnum.CanChangeRole:
				if (settingsData)
				{
					role.ServerCanChangeRole = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanChangeRole = false;
					role.ServerCanCreateRoles = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanWorkChannels:
				if (settingsData)
				{
					role.ServerCanWorkChannels = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanWorkChannels = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanDeleteUsers:
				if (settingsData)
				{
					role.ServerCanDeleteUsers = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanDeleteUsers = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanMuteOther:
				if (settingsData)
				{
					role.ServerCanMuteOther = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanMuteOther = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanDeleteOthersMessages:
				if (settingsData)
				{
					role.ServerCanDeleteOthersMessages = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanDeleteOthersMessages = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanIgnoreMaxCount:
				if (settingsData)
				{
					role.ServerCanIgnoreMaxCount = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanIgnoreMaxCount = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanCreateRole:
				if (settingsData)
				{
					role.ServerCanCreateRoles = true;
					role.ServerCanChangeRole = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanCreateRoles = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanCreateLessons:
				if (server.ServerType != ServerTypeEnum.Teacher)
				{
					throw new CustomException("Cant change CanCreateLessons", "ChangeRoleSettingsAdminAsync", "Role", 400, "Нельзя менять данную настройку в этом канале", "Изменение настроек роли админом");
				}
				if (settingsData)
				{
					role.ServerCanCreateLessons = true;
					role.ServerCanCheckAttendance = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanCreateLessons = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			case SettingsEnum.CanCheckAttendance:
				if (server.ServerType != ServerTypeEnum.Teacher)
				{
					throw new CustomException("Cant change CanCreateLessons", "ChangeRoleSettingsAdminAsync", "Role", 400, "Нельзя менять данную настройку в этом канале", "Изменение настроек роли админом");
				}
				if (settingsData)
				{
					role.ServerCanCheckAttendance = true;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				else
				{
					role.ServerCanCheckAttendance = false;
					role.ServerCanDeleteUsers = false;
					_hitsContext.Role.Update(role);
					await _hitsContext.SaveChangesAsync();
				}
				break;

			default: throw new CustomException("Setting not found", "ChangeRoleSettingsAdminAsync", "Setting", 404, "Настройка не найдена", "Изменение настроек роли админом");
		}

		var roleResponse = new RoleSettingsDTO
		{
			Role = new RolesItemDTO
			{
				Id = role.Id,
				ServerId = server.Id,
				Name = role.Name,
				Tag = role.Tag,
				Color = role.Color,
				Type = role.Role
			},
			Settings = new SettingsDTO
			{
				CanChangeRole = role.ServerCanChangeRole,
				CanWorkChannels = role.ServerCanWorkChannels,
				CanDeleteUsers = role.ServerCanDeleteUsers,
				CanMuteOther = role.ServerCanMuteOther,
				CanDeleteOthersMessages = role.ServerCanDeleteOthersMessages,
				CanIgnoreMaxCount = role.ServerCanIgnoreMaxCount,
				CanCreateRoles = role.ServerCanCreateRoles,
				CanCreateLessons = role.ServerCanCreateLessons,
				CanCheckAttendance = role.ServerCanCheckAttendance
			}
		};

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(roleResponse, alertedUsers, "Updated role settings");
		}
	}

	public async Task DeleteUserFromServerAdminAsync(string token, Guid serverId, Guid userId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "DeleteUserFromServerAdminAsync", "ServerId", 404, "Сервер не найден", "Удаление пользователя с сервера админом");
		}

		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new CustomException("User not found", "DeleteUserFromServerAdminAsync", "ServerId", 404, "Пользователь не найден", "Удаление пользователя с сервера админом");
		}
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "Check user", "User", 404, "Удаляемый пользователь не найден", "Удаление пользователя с сервера");
		}

		var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.TextChannel.ServerId == server.Id && lr.UserId == userSub.UserId).ToListAsync();
		_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

		var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == userSub.Id).ToListAsync();
		_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

		userSub.IsBanned = true;
		userSub.BanReason = "Забанен админом";
		userSub.BanTime = DateTime.UtcNow;
		_hitsContext.UserServer.Update(userSub);
		var userVoiceChannel = await _hitsContext.UserVoiceChannel.Include(uvc => uvc.VoiceChannel).FirstOrDefaultAsync(uvc => uvc.UserId == userId && uvc.VoiceChannel.ServerId == serverId);
		var newRemovedUserResponse = new RemovedUserDTO
		{
			ServerId = serverId,
			IsNeedRemoveFromVC = userVoiceChannel != null
		};
		await _hitsContext.SaveChangesAsync();

		await _webSocketManager.BroadcastMessageAsync(newRemovedUserResponse, new List<Guid> { userId }, "You removed from server");

		var newUnsubscriberResponse = new UnsubscribeResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
		}

		await _hitsContext.Notifications.AddAsync(new NotificationDbModel
		{
			UserId = userId,
			Text = $"Вы были забанены на сервере: {server.Name}",
			CreatedAt = DateTime.UtcNow,
			IsReaded = false
		});
		await _hitsContext.SaveChangesAsync();
	}

	public async Task ChangeUserNameAdminAsync(Guid serverId, string token, Guid userId, string name)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "ChangeUserNameAsync", "ServerId", 404, "Сервер не найден", "Изменение имени пользователя на сервере админом");
		}

		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new CustomException("User not found", "ChangeUserNameAsync", "ServerId", 404, "Пользователь не найден", "Изменение имени пользователя на сервере админом");
		}
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "ChangeUserNameAsync", "User", 404, "Удаляемый пользователь не найден", "Изменение имени пользователя на сервере админом");
		}

		userSub.UserServerName = name;
		_hitsContext.UserServer.Update(userSub);
		await _hitsContext.SaveChangesAsync();


		var changeServerName = new ChangeNameOnServerDTO
		{
			ServerId = serverId,
			UserId = userSub.UserId,
			Name = name
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeServerName, alertedUsers, "New users name on server");
		}
	}

	public async Task AddRoleToUserAdminAsync(string token, Guid serverId, Guid userId, Guid roleId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "AddRoleToUserAdminAsync", "ServerId", 404, "Сервер не найден", "Добавление роли пользователю админом");
		}

		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new CustomException("User not found", "AddRoleToUserAdminAsync", "ServerId", 404, "Пользователь не найден", "Добавление роли пользователю админом");
		}

		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(us => us.Role)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "AddRoleToUserAdminAsync", "User", 404, "Пользователь не найден", "Добавление роли пользователю админом");
		}

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId && r.ServerId == serverId && r.Role != RoleEnum.Creator);
		if (role == null)
		{
			throw new CustomException("Role not found", "AddRoleToUserAdminAsync", "Role ID", 404, "Роль не найдена", "Добавление роли пользователю");
		}

		if (userSub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == role.Id) != null)
		{
			throw new CustomException("User already has this role", "AddRoleToUserAdminAsync", "Changed user role", 401, "Пользователь уже имеет эту роль", "Добавление роли пользователю админом");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

		if (role.Role == RoleEnum.Uncertain)
		{
			var rolesIds = userSub.SubscribeRoles.Select(sr => sr.RoleId).ToList();

			var removedChannels = await _hitsContext.ChannelCanSee
				.Where(ccs => rolesIds.Contains(ccs.RoleId))
				.Select(ccs => ccs.ChannelId)
				.ToListAsync();

			var lastRead = await _hitsContext.LastReadChannelMessage
				.Where(lr => lr.UserId == user.Id && removedChannels.Contains(lr.TextChannelId))
				.ToListAsync();

			if (lastRead != null)
			{
				_hitsContext.LastReadChannelMessage.RemoveRange(lastRead);
				await _hitsContext.SaveChangesAsync();
			}

			_hitsContext.SubscribeRole.RemoveRange(userSub.SubscribeRoles);
			await _hitsContext.SaveChangesAsync();

			foreach (var remRole in rolesIds)
			{
				var oldUserRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = userId,
					RoleId = remRole,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
				}
			}
		}
		else
		{
			var unc = userSub.SubscribeRoles.FirstOrDefault(sr => sr.Role.Role == RoleEnum.Uncertain);
			if (unc != null)
			{
				var removedChannels = await _hitsContext.ChannelCanSee
					.Where(ccs => ccs.RoleId == unc.RoleId)
					.Select(ccs => ccs.ChannelId)
					.ToListAsync();

				var lastRead = await _hitsContext.LastReadChannelMessage
					.Where(lr => lr.UserId == user.Id && removedChannels.Contains(lr.TextChannelId))
					.ToListAsync();

				if (lastRead != null)
				{
					_hitsContext.LastReadChannelMessage.RemoveRange(lastRead);
					await _hitsContext.SaveChangesAsync();
				}

				_hitsContext.SubscribeRole.Remove(unc);
				await _hitsContext.SaveChangesAsync();

				var oldUserRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = userId,
					RoleId = unc.RoleId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
				}
			}
		}

		userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
		{
			UserServerId = userSub.Id,
			RoleId = role.Id
		});

		_hitsContext.UserServer.Update(userSub);
		await _hitsContext.SaveChangesAsync();

		var visibleChannels = await _hitsContext.ChannelCanSee
			.Where(ccs => ccs.RoleId == role.Id)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();
		foreach (var channel in visibleChannels)
		{
			bool alreadyExists = await _hitsContext.LastReadChannelMessage
				.AnyAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channel);

			if (!alreadyExists)
			{
				var lastMessageId = await _hitsContext.ChannelMessage
					.Where(m => m.TextChannelId == channel)
					.OrderByDescending(m => m.Id)
					.Select(m => (long?)m.Id)
					.FirstOrDefaultAsync() ?? 0;

				var lastRead = new LastReadChannelMessageDbModel
				{
					UserId = user.Id,
					TextChannelId = channel,
					LastReadedMessageId = lastMessageId
				};

				await _hitsContext.LastReadChannelMessage.AddAsync(lastRead);
			}
		}
		await _hitsContext.SaveChangesAsync();

		var newUserRole = new NewUserRoleResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
			RoleId = role.Id,
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
		}
	}

	public async Task RemoveRoleFromUserAdminAsync(string token, Guid serverId, Guid userId, Guid roleId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "RemoveRoleFromUserAdminAsync", "ServerId", 404, "Сервер не найден", "Удаление роли у пользователя админом");
		}

		var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
		{
			throw new CustomException("User not found", "RemoveRoleFromUserAdminAsync", "ServerId", 404, "Пользователь не найден", "Удаление роли у пользователя админом");
		}
		var userSub = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
			.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);
		if (userSub == null)
		{
			throw new CustomException("User is not subscriber of this server", "RemoveRoleFromUserAdminAsync", "User sub", 404, "Пользователь не является подписчиком сервера", "Удаление роли у пользователя админом");
		}

		var deletedRole = userSub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == roleId);

		if (deletedRole == null)
		{
			throw new CustomException("Deleted role not found", "RemoveRoleFromUserAdminAsync", "Deleted role", 404, "Удаляемая роль не найдена", "Удаление роли у пользователя админом");
		}

		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (userSub.SubscribeRoles.Count() == 1)
		{
			var uncertainRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.ServerId == server.Id && r.Role == RoleEnum.Uncertain);
			if (uncertainRole == null)
			{
				throw new CustomException("Uncertain role not found", "RemoveRoleFromUserAdminAsync", "Uncertain role", 404, "Неопредленная роль не найдена", "Удаление роли у пользователя админом");
			}

			if (uncertainRole.Id == deletedRole.RoleId)
			{
				throw new CustomException("Only role of user - is uncertain", "RemoveRoleFromUserAdminAsync", "Deleted role", 400, "У пользователя только одна роль - неопределенная", "Удаление роли у пользователя админом");
			}
			else
			{
				userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
				{
					RoleId = uncertainRole.Id,
					UserServerId = userSub.Id
				});
				_hitsContext.UserServer.Update(userSub);
				await _hitsContext.SaveChangesAsync();

				var newUserRole = new NewUserRoleResponseDTO
				{
					ServerId = serverId,
					UserId = userId,
					RoleId = uncertainRole.Id,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
				}
			}
		}

		userSub.SubscribeRoles.Remove(deletedRole);
		_hitsContext.UserServer.Update(userSub);
		await _hitsContext.SaveChangesAsync();

		var removedChannels = await _hitsContext.ChannelCanSee
			.Where(ccs => ccs.RoleId == deletedRole.RoleId)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();
		foreach (var channelId in removedChannels)
		{
			bool stillHasAccess = await _hitsContext.ChannelCanSee
				.AnyAsync(ccs => removedChannels.Contains(ccs.ChannelId)
								 && userSub.SubscribeRoles.Select(sr => sr.RoleId).Contains(ccs.RoleId));

			if (!stillHasAccess)
			{
				var lastRead = await _hitsContext.LastReadChannelMessage
					.FirstOrDefaultAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channelId);

				if (lastRead != null)
					_hitsContext.LastReadChannelMessage.Remove(lastRead);
			}
		}
		await _hitsContext.SaveChangesAsync();

		var oldUserRole = new NewUserRoleResponseDTO
		{
			ServerId = serverId,
			UserId = userId,
			RoleId = deletedRole.RoleId,
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
		}
	}

	public async Task CreateChannelAdminAsync(Guid serverId, string token, string name, ChannelTypeEnum channelType, int? maxCount)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server == null)
		{
			throw new CustomException("Server not found", "CreateChannelAdminAsync", "ServerId", 404, "Сервер не найден", "Создания канала админом");
		}

		var serverRolesId = await _hitsContext.Role.Where(r => r.ServerId == server.Id && (r.Role == RoleEnum.Admin || r.Role == RoleEnum.Creator)).Select(r => r.Id).ToListAsync();

		Guid channelId = Guid.NewGuid();
		string channelName = "";

		switch (channelType)
		{
			case ChannelTypeEnum.Text:
				var newTextChannel = new TextChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					Messages = new List<ChannelMessageDbModel>(),
					ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>()
				};

				channelId = newTextChannel.Id;
				channelName = newTextChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newTextChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newTextChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newTextChannel.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = newTextChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newTextChannel.ChannelCanWriteSub.Add(new ChannelCanWriteSubDbModel { TextChannelId = newTextChannel.Id, RoleId = roleId });
				}

				await _hitsContext.TextChannel.AddAsync(newTextChannel);
				await _hitsContext.SaveChangesAsync();

				var usersIdText = await _hitsContext.UserServer
					.Include(us => us.SubscribeRoles)
					.Where(us => us.ServerId == serverId &&
						us.SubscribeRoles.Any(sr => serverRolesId.Contains(sr.RoleId)))
					.Select(us => us.UserId)
					.ToListAsync();

				var lastReadedList = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersIdText)
				{
					lastReadedList.Add(new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newTextChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedList);
				await _hitsContext.SaveChangesAsync();

				break;

			case ChannelTypeEnum.Voice:
				var newVoiceChannel = new VoiceChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					MaxCount = (int)(maxCount == null ? 999 : maxCount),
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					ChannelCanJoin = new List<ChannelCanJoinDbModel>()
				};

				channelId = newVoiceChannel.Id;
				channelName = newVoiceChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newVoiceChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newVoiceChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newVoiceChannel.ChannelCanJoin.Add(new ChannelCanJoinDbModel { VoiceChannelId = newVoiceChannel.Id, RoleId = roleId });
				}

				await _hitsContext.VoiceChannel.AddAsync(newVoiceChannel);
				await _hitsContext.SaveChangesAsync();

				break;

			case ChannelTypeEnum.Pair:
				if (server.ServerType != ServerTypeEnum.Teacher)
				{
					throw new CustomException("Server no teachers", "CreateChannelAdminAsync", "Channel type", 401, "Канал такого типа нельзя создать в вашем сервере", "Создания канала админом");
				}
				var newPairChannel = new PairVoiceChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					MaxCount = (int)(maxCount == null ? 999 : maxCount),
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					ChannelCanJoin = new List<ChannelCanJoinDbModel>(),
					Pairs = new List<PairDbModel>()
				};

				channelId = newPairChannel.Id;
				channelName = newPairChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newPairChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newPairChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newPairChannel.ChannelCanJoin.Add(new ChannelCanJoinDbModel { VoiceChannelId = newPairChannel.Id, RoleId = roleId });
				}

				await _hitsContext.PairVoiceChannel.AddAsync(newPairChannel);
				await _hitsContext.SaveChangesAsync();

				break;

			case ChannelTypeEnum.Notification:
				var newNotificationChannel = new NotificationChannelDbModel
				{
					Name = name,
					ServerId = serverId,
					ChannelCanSee = new List<ChannelCanSeeDbModel>(),
					Messages = new List<ChannelMessageDbModel>(),
					ChannelCanWrite = new List<ChannelCanWriteDbModel>(),
					ChannelNotificated = new List<ChannelNotificatedDbModel>(),

					ChannelCanWriteSub = new List<ChannelCanWriteSubDbModel>()
				};

				channelId = newNotificationChannel.Id;
				channelName = newNotificationChannel.Name;

				foreach (var roleId in serverRolesId)
				{
					newNotificationChannel.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = newNotificationChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newNotificationChannel.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = newNotificationChannel.Id, RoleId = roleId });
				}
				foreach (var roleId in serverRolesId)
				{
					newNotificationChannel.ChannelNotificated.Add(new ChannelNotificatedDbModel { NotificationChannelId = newNotificationChannel.Id, RoleId = roleId });
				}

				await _hitsContext.NotificationChannel.AddAsync(newNotificationChannel);
				await _hitsContext.SaveChangesAsync();

				var usersIdNot = await _hitsContext.UserServer
					.Include(us => us.SubscribeRoles)
					.Where(us => us.ServerId == serverId &&
						us.SubscribeRoles.Any(sr => serverRolesId.Contains(sr.RoleId)))
					.Select(us => us.UserId)
					.ToListAsync();

				var lastReadedListNot = new List<LastReadChannelMessageDbModel>();
				foreach (var userId in usersIdNot)
				{
					lastReadedListNot.Add(new LastReadChannelMessageDbModel
					{
						UserId = userId,
						TextChannelId = newNotificationChannel.Id,
						LastReadedMessageId = 0
					});
				}

				_hitsContext.LastReadChannelMessage.AddRange(lastReadedListNot);
				await _hitsContext.SaveChangesAsync();
				break;

			default:
				throw new CustomException("Invalid channel type", "CreateChannelAdminAsync", "Channel type", 400, "Отсутствует такой тип канала", "Создание канала админом");
		}

		var newChannelResponse = new ChannelResponseSocket
		{
			Create = true,
			ServerId = serverId,
			ChannelId = channelId,
			ChannelName = channelName,
			ChannelType = channelType
		};
		var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(newChannelResponse, alertedUsers, "New channel");
		}
	}

	public async Task DeleteChannelAdminAsync(Guid chnnelId, string token)
	{
		var admin = await GetAdminAsync(token);
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == chnnelId && ((TextChannelDbModel)c).DeleteTime == null);
		if (channel == null)
		{
			throw new CustomException("Channel not found", "DeleteChannelAdminAsync", "Channel", 404, "Канал не найден", "Удаление канала админом");
		}

		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();

		var channelType = await _channelService.GetChannelType(channel.Id);
		if (channelType == ChannelTypeEnum.Voice || channelType == ChannelTypeEnum.Pair)
		{
			var userVoiceChannelIds = await _hitsContext.UserVoiceChannel.Where(uvc => uvc.VoiceChannelId == channel.Id).Select(uvc => uvc.UserId).ToListAsync();

			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				foreach (var userId in userVoiceChannelIds)
				{
					var removedUser = new UserVoiceChannelResponseDTO
					{
						ServerId = channel.ServerId,
						isEnter = false,
						UserId = userId,
						ChannelId = channel.Id
					};

					await _webSocketManager.BroadcastMessageAsync(removedUser, alertedUsers, "User removed from voice channel");
					await _webSocketManager.BroadcastMessageAsync(removedUser, new List<Guid> { userId }, "You removed from voice channel");
				}
			}

			_hitsContext.Channel.Remove(channel);
			await _hitsContext.SaveChangesAsync();
		}
		if (channelType == ChannelTypeEnum.Text || channelType == ChannelTypeEnum.Notification)
		{
			var tc = await _hitsContext.TextChannel.FirstOrDefaultAsync(c => c.Id == chnnelId);
			tc.DeleteTime = DateTime.UtcNow.AddDays(21);
			_hitsContext.TextChannel.Update(tc);
			await _hitsContext.SaveChangesAsync();
		}

		var deletedChannelResponse = new ChannelResponseSocket
		{
			Create = false,
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			ChannelName = channel.Name,
			ChannelType = channel is VoiceChannelDbModel ? ChannelTypeEnum.Voice : (channel is TextChannelDbModel ? ChannelTypeEnum.Text : ChannelTypeEnum.Notification)
		};
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(deletedChannelResponse, alertedUsers, "Channel deleted");
		}
	}

	public async Task ChnageChannnelNameAdminAsync(string token, Guid channelId, string name, int? number)
	{
		var admin = await GetAdminAsync(token);
		var channel = await _hitsContext.Channel.FirstOrDefaultAsync(c => c.Id == channelId && ((TextChannelDbModel)c).DeleteTime == null);
		if (channel == null)
		{
			throw new CustomException("Channel not found", "ChnageChannnelNameAdminAsync", "Channel", 404, "Канал не найден", "Изменение информации канала админом");
		}

		channel.Name = name;
		var channelType = await _channelService.GetChannelType(channel.Id);
		if (channelType == ChannelTypeEnum.Voice || channelType == ChannelTypeEnum.Pair)
		{
			if (number == null)
			{
				throw new CustomException("Number not found", "ChnageChannnelNameAdminAsync", "Number", 404, "Максимальная вместимость должна быть указана", "Изменение информации канала админом");
			}
			((VoiceChannelDbModel)channel).MaxCount = (int)number;
		}
		_hitsContext.Channel.Update(channel);
		await _hitsContext.SaveChangesAsync();

		var changeChannelName = new ChangeChannelNameDTO
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			Name = name
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changeChannelName, alertedUsers, "Change channel name");
		}
		if (channelType == ChannelTypeEnum.Voice || channelType == ChannelTypeEnum.Pair)
		{
			var changeMaxCount = new ChangeMaxCountDTO
			{
				ServerId = channel.ServerId,
				VoiceChannelId = channel.Id,
				MaxCount = (int)number
			};
			var alertedUsersSec = await _hitsContext.UserServer
				.Where(us => us.ServerId == channel.ServerId)
				.Select(us => us.UserId)
				.ToListAsync();
			if (alertedUsers != null && alertedUsers.Count() > 0)
			{
				await _webSocketManager.BroadcastMessageAsync(changeMaxCount, alertedUsersSec, "Change max count");
			}
		}
	}

	public async Task ChangeVoiceChannelSettingsAdminAsync(string token, ChannelRoleDTO settingsData)
	{
		var admin = await GetAdminAsync(token);
		var channel = await _channelService.CheckVoiceChannelExistAsync(settingsData.ChannelId, false);

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);

		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change voice channel settings admin", "Role", 404, "Роль не существует", "Изменение настроек голосового канала");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change voice channel settings admin", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек голосового канала");
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change voice channel settings admin", "Role", 400, "Роль уже может видеть канал", "Изменение настроек голосового канала админом");
				}
				_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change voice channel settings admin", "Role", 400, "Роль уже неможет видеть канал", "Изменение настроек голосового канала админом");
				}

				var canJoin = await _hitsContext.ChannelCanJoin.FirstOrDefaultAsync(ccs => ccs.VoiceChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canJoin != null)
				{
					_hitsContext.ChannelCanJoin.Remove(canJoin);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanJoin)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
			var canJoin = await _hitsContext.ChannelCanJoin.FirstOrDefaultAsync(ccs => ccs.VoiceChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canJoin != null)
				{
					throw new CustomException("Role already can join channel", "Change voice channel settings admin", "Role", 400, "Роль уже может присоединиться к каналу", "Изменение настроек голосового канала админом");
				}
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
					await _hitsContext.SaveChangesAsync();
				}
				_hitsContext.ChannelCanJoin.Add(new ChannelCanJoinDbModel { VoiceChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canJoin == null)
				{
					throw new CustomException("Role already cant see channel", "Change voice channel settings admin", "Role", 400, "Роль уже неможет видеть канал", "Изменение настроек голосового канала админом");
				}
				_hitsContext.ChannelCanJoin.Remove(canJoin);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanJoin)
		{
			throw new CustomException("Wrong setting type", "Change voice channel settings admin", "Role", 404, "Тип настроек не верен", "Изменение настроек голосового канала админом");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Voice channel settings edited");
		}
	}

	public async Task ChangeTextChannelSettingsAdminAsync(string token, ChannelRoleDTO settingsData)
	{
		var admin = await GetAdminAsync(token);
		var channel = await _channelService.CheckTextChannelExistAsync(settingsData.ChannelId);

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change text channel settings admin", "Role", 404, "Роль не существует", "Изменение настроек текстового канала админом");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change text channel settings admin", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек текстового канала админом");
		}

		var userServersLastRead = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();

		var lastMessageId = await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channel.Id)
			.OrderByDescending(m => m.Id)
			.Select(m => m.Id)
			.FirstOrDefaultAsync();

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change text channel settings admin", "Role", 400, "Роль уже может видеть канал", "Изменение настроек текстового канала админом");
				}
				_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change text channel settings admin", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек текстового канала админом");
				}

				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite != null)
				{
					_hitsContext.ChannelCanWrite.Remove(canWrite);
				}
				var canWriteSub = await _hitsContext.ChannelCanWriteSub.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWriteSub != null)
				{
					_hitsContext.ChannelCanWriteSub.Remove(canWriteSub);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);

				var subs = await _hitsContext.SubChannel.Where(sc => sc.TextChannelId == channel.Id).Select(sc => sc.Id).ToListAsync();
				var canUse = await _hitsContext.ChannelCanUse.Where(ccu => subs.Contains(ccu.SubChannelId) && ccu.RoleId == role.Id).ToListAsync();

				_hitsContext.ChannelCanUse.RemoveRange(canUse);
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var hasOtherAccess = us.SubscribeRoles
						.Any(sr => sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id && sr.RoleId != role.Id));
					if (!hasOtherAccess)
					{
						var lastReadEntries = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
						if (lastReadEntries != null)
						{
							_hitsContext.LastReadChannelMessage.RemoveRange(lastReadEntries);
						}
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWrite)
		{
			var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canWrite != null)
				{
					throw new CustomException("Role already can write in channel", "Change text channel settings admin", "Role", 400, "Роль уже может писать в канал", "Изменение настроек текстового канала админом");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canWrite == null)
				{
					throw new CustomException("Role already cant write in channel", "Change text channel settings admin", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек текстового канала админом");
				}
				var canWriteSub = await _hitsContext.ChannelCanWriteSub.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWriteSub != null)
				{
					_hitsContext.ChannelCanWriteSub.Remove(canWriteSub);
				}
				_hitsContext.ChannelCanWrite.Remove(canWrite);

				var subs = await _hitsContext.SubChannel.Where(sc => sc.TextChannelId == channel.Id).Select(sc => sc.Id).ToListAsync();
				var canUse = await _hitsContext.ChannelCanUse.Where(ccu => subs.Contains(ccu.SubChannelId) && ccu.RoleId == role.Id).ToListAsync();

				_hitsContext.ChannelCanUse.RemoveRange(canUse);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWriteSub)
		{
			var canWriteSub = await _hitsContext.ChannelCanWriteSub.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canWriteSub != null)
				{
					throw new CustomException("Role already can write subs in channel", "Change text channel settings admin", "Role", 400, "Роль уже может писать подчаты в канал", "Изменение настроек текстового канала админом");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite == null)
				{
					_hitsContext.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelCanWriteSub.Add(new ChannelCanWriteSubDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canWriteSub == null)
				{
					throw new CustomException("Role already cant write subs in channel", "Change text channel settings admin", "Role", 400, "Роль уже не может писать подчаты в канал", "Изменение настроек текстового канала админом");
				}
				_hitsContext.ChannelCanWriteSub.Remove(canWriteSub);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanWrite && settingsData.Type != ChangeRoleTypeEnum.CanWriteSub)
		{
			throw new CustomException("Wrong setting type", "Change text channel settings admin", "Role", 404, "Тип настроек не верен", "Изменение настроек текстового канала админом");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Text channel settings edited");
		}
	}

	public async Task ChangeNotificationChannelSettingsAdminAsync(string token, ChannelRoleDTO settingsData)
	{
		var admin = await GetAdminAsync(token);
		var channel = await _channelService.CheckNotificationChannelExistAsync(settingsData.ChannelId);

		var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == settingsData.RoleId && r.ServerId == channel.ServerId);
		if (role == null)
		{
			throw new CustomException("Role doesnt exist", "Change notification channel settings admin", "Role", 404, "Роль не существует", "Изменение настроек уведомительного канала админом");
		}
		if (role.Role == RoleEnum.Creator || role.Role == RoleEnum.Admin)
		{
			throw new CustomException("Cant change creator permissions", "Change notification channel settings admin", "Role", 400, "Нельзя изменять разрешения создателя", "Изменение настроек уведомительного канала админом");
		}

		var userServersLastRead = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Where(us => us.SubscribeRoles.Any(sr => sr.RoleId == role.Id))
			.ToListAsync();

		var lastMessageId = await _hitsContext.ChannelMessage
			.Where(m => m.TextChannelId == channel.Id)
			.OrderByDescending(m => m.Id)
			.Select(m => m.Id)
			.FirstOrDefaultAsync();

		if (settingsData.Type == ChangeRoleTypeEnum.CanSee)
		{
			var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canSee != null)
				{
					throw new CustomException("Role already can see channel", "Change notification channel settings admin", "Role", 400, "Роль уже может видеть канал", "Изменение настроек уведомительного канала админом");
				}
				_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canSee == null)
				{
					throw new CustomException("Role already cant see channel", "Change notification channel settings admin", "Role", 400, "Роль уже не может видеть канал", "Изменение настроек уведомительного канала админом");
				}
				var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canWrite != null)
				{
					_hitsContext.ChannelCanWrite.Remove(canWrite);
				}
				var notificated = await _hitsContext.ChannelNotificated.FirstOrDefaultAsync(ccs => ccs.NotificationChannelId == channel.Id && ccs.RoleId == role.Id);
				if (notificated != null)
				{
					_hitsContext.ChannelNotificated.Remove(notificated);
				}
				_hitsContext.ChannelCanSee.Remove(canSee);
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var hasOtherAccess = us.SubscribeRoles
						.Any(sr => sr.Role.ChannelCanSee.Any(ccs => ccs.ChannelId == channel.Id && sr.RoleId != role.Id));
					if (!hasOtherAccess)
					{
						var lastReadEntries = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
						if (lastReadEntries != null)
						{
							_hitsContext.LastReadChannelMessage.RemoveRange(lastReadEntries);
						}
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.CanWrite)
		{
			var canWrite = await _hitsContext.ChannelCanWrite.FirstOrDefaultAsync(ccs => ccs.TextChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (canWrite != null)
				{
					throw new CustomException("Role already can write in channel", "Change notification channel settings admin", "Role", 400, "Роль уже может писать в канал", "Изменение настроек уведомительного канала админом");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelCanWrite.Add(new ChannelCanWriteDbModel { TextChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (canWrite == null)
				{
					throw new CustomException("Role already cant write in channel", "Change notification channel settings admin", "Role", 400, "Роль уже не может писать в канал", "Изменение настроек уведомительного канала админом");
				}
				_hitsContext.ChannelCanWrite.Remove(canWrite);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type == ChangeRoleTypeEnum.Notificated)
		{
			var notificated = await _hitsContext.ChannelNotificated.FirstOrDefaultAsync(ccs => ccs.NotificationChannelId == channel.Id && ccs.RoleId == role.Id);

			if (settingsData.Add == true)
			{
				if (notificated != null)
				{
					throw new CustomException("Role already notificated in channel", "Change notification channel settings admin", "Role", 400, "Роль уже уведомляется в канале", "Изменение настроек уведомительного канала админом");
				}
				var canSee = await _hitsContext.ChannelCanSee.FirstOrDefaultAsync(ccs => ccs.ChannelId == channel.Id && ccs.RoleId == role.Id);
				if (canSee == null)
				{
					_hitsContext.ChannelCanSee.Add(new ChannelCanSeeDbModel { ChannelId = channel.Id, RoleId = role.Id });
				}
				_hitsContext.ChannelNotificated.Add(new ChannelNotificatedDbModel { NotificationChannelId = channel.Id, RoleId = role.Id });
				await _hitsContext.SaveChangesAsync();

				foreach (var us in userServersLastRead)
				{
					var alreadyExists = await _hitsContext.LastReadChannelMessage
						.AnyAsync(lr => lr.UserId == us.UserId && lr.TextChannelId == channel.Id);
					if (!alreadyExists)
					{
						_hitsContext.LastReadChannelMessage.Add(new LastReadChannelMessageDbModel
						{
							UserId = us.UserId,
							TextChannelId = channel.Id,
							LastReadedMessageId = lastMessageId
						});
					}
				}
				await _hitsContext.SaveChangesAsync();
			}
			else
			{
				if (notificated == null)
				{
					throw new CustomException("Role already notificated in channel", "Change notification channel settings admin", "Role", 400, "Роль уже уведомляется в канале", "Изменение настроек уведомительного канала админом");
				}
				_hitsContext.ChannelNotificated.Remove(notificated);
				await _hitsContext.SaveChangesAsync();
			}
		}

		if (settingsData.Type != ChangeRoleTypeEnum.CanSee && settingsData.Type != ChangeRoleTypeEnum.CanWrite && settingsData.Type != ChangeRoleTypeEnum.Notificated)
		{
			throw new CustomException("Wrong setting type", "Change notification channel settings admin", "Role", 404, "Тип настроек не верен", "Изменение настроек уведомительного канала админом");
		}

		var changedSettingsresponse = new ChannelRoleResponseSocket
		{
			ServerId = channel.ServerId,
			ChannelId = channel.Id,
			RoleId = role.Id,
			Add = settingsData.Add,
			Type = settingsData.Type
		};
		var alertedUsers = await _hitsContext.UserServer
			.Where(us => us.ServerId == channel.ServerId)
			.Select(us => us.UserId)
			.ToListAsync();
		if (alertedUsers != null && alertedUsers.Count() > 0)
		{
			await _webSocketManager.BroadcastMessageAsync(changedSettingsresponse, alertedUsers, "Notification channel settings edited");
		}
	}

	public async Task<ServerPresetItemDTO> CreatePresetAdminAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server.ServerType != ServerTypeEnum.Teacher)
		{
			throw new CustomException("Server isnt teachers", "CreatePresetAdminAsync", "Server", 401, "Сервер не является учительсяким", "Создание пресета админом");
		}

		var serverRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == serverRoleId && r.ServerId == server.Id);
		if (serverRole == null)
		{
			throw new CustomException("Server role not found", "CreatePresetAdminAsync", "Server role", 404, "Серверная роль не найдена", "Создание пресета админом");
		}

		var systemRole = await _hitsContext.SystemRole.FirstOrDefaultAsync(r => r.Id == systemRoleId);
		if (systemRole == null)
		{
			throw new CustomException("System role not found", "CreatePresetAdminAsync", "System role", 404, "Системная роль не найдена", "Создание пресета админом");
		}

		var preset = await _hitsContext.Preset.FirstOrDefaultAsync(p => p.SystemRoleId == systemRole.Id && p.ServerRoleId == serverRole.Id);
		if (preset != null)
		{
			throw new CustomException("Preset already exist", "CreatePresetAdminAsync", "Preset", 400, "Такой пресет уже существует", "Создание пресета админом");
		}

		var newPreset = new ServerPresetDbModel
		{
			SystemRoleId = systemRole.Id,
			ServerRoleId = serverRole.Id
		};

		await _hitsContext.Preset.AddAsync(newPreset);
		await _hitsContext.SaveChangesAsync();

		var users = await _hitsContext.User
			.Include(u => u.SystemRoles)
			.Where(u => u.SystemRoles.Any(sr => sr.Id == systemRoleId))
			.ToListAsync();

		var channelsCanRead = await _hitsContext.ChannelCanSee
			.Include(ccs => ccs.Channel)
			.Where(ccs => (ccs.Channel is TextChannelDbModel || ccs.Channel is NotificationChannelDbModel || ccs.Channel is SubChannelDbModel)
				&& ccs.Channel.ServerId == server.Id
				&& ccs.RoleId == serverRole.Id)
			.Select(ccs => ccs.ChannelId)
			.ToListAsync();

		foreach (var user in users)
		{
			var userSub = await _hitsContext.UserServer
				.Include(us => us.SubscribeRoles)
				.FirstOrDefaultAsync(us => us.ServerId == server.Id && us.UserId == user.Id);

			if (userSub == null || !(userSub.SubscribeRoles.Any(sr => sr.RoleId == serverRole.Id)))
			{
				var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();

				if (userSub == null)
				{
					var newSub = new UserServerDbModel
					{
						Id = Guid.NewGuid(),
						UserId = user.Id,
						ServerId = server.Id,
						UserServerName = user.AccountName,
						IsBanned = false,
						NonNotifiable = false,
						SubscribeRoles = new List<SubscribeRoleDbModel>()
					};
					newSub.SubscribeRoles.Add(new SubscribeRoleDbModel
					{
						UserServerId = newSub.Id,
						RoleId = serverRole.Id
					});

					var lastReadedList = new List<LastReadChannelMessageDbModel>();
					if (channelsCanRead != null)
					{
						foreach (var channel in channelsCanRead)
						{
							lastReadedList.Add(new LastReadChannelMessageDbModel
							{
								UserId = user.Id,
								TextChannelId = channel,
								LastReadedMessageId = (
									await _hitsContext.ChannelMessage
										.Where(cm => cm.TextChannelId == channel)
										.Select(m => (long?)m.Id).MaxAsync() ?? 0
								)
							});
						}
					}

					await _hitsContext.UserServer.AddAsync(newSub);
					await _hitsContext.LastReadChannelMessage.AddRangeAsync(lastReadedList);
					await _hitsContext.SaveChangesAsync();

					var newSubscriberResponse = new ServerUserDTO
					{
						ServerId = server.Id,
						UserId = user.Id,
						UserName = user.AccountName,
						UserTag = user.AccountTag,
						Icon = null,
						Roles = new List<UserServerRoles>{
							new UserServerRoles
							{
								RoleId = serverRole.Id,
								RoleName = serverRole.Name,
								RoleType = serverRole.Role
							}
						},
						Notifiable = user.Notifiable,
						FriendshipApplication = user.FriendshipApplication,
						NonFriendMessage = user.NonFriendMessage,
						isFriend = false,
						SystemRoles = user.SystemRoles
							.Select(sr => new SystemRoleShortItemDTO
							{
								Name = sr.Name,
								Type = sr.Type
							})
							.ToList()
					};
					if (user != null && user.IconFileId != null)
					{
						var userIcon = await GetImageAsync((Guid)user.IconFileId);
						newSubscriberResponse.Icon = userIcon;
					}

					var allFriends = await _hitsContext.Friendship
						.Where(f => f.UserIdFrom == user.Id || f.UserIdTo == user.Id)
						.Select(f => f.UserIdFrom == user.Id ? f.UserIdTo : f.UserIdFrom)
						.Distinct()
						.ToListAsync();
					var friendsSet = new HashSet<Guid>(allFriends);
					if (alertedUsers != null && alertedUsers.Count() > 0)
					{
						foreach (var alertedUser in alertedUsers)
						{
							newSubscriberResponse.isFriend = friendsSet.Contains(alertedUser);
							await _webSocketManager.BroadcastMessageAsync(newSubscriberResponse, new List<Guid> { alertedUser }, "New user on server");
						}
					}
				}
				else
				{
					userSub.SubscribeRoles.Add(new SubscribeRoleDbModel
					{
						UserServerId = userSub.Id,
						RoleId = serverRole.Id
					});

					_hitsContext.UserServer.Update(userSub);
					await _hitsContext.SaveChangesAsync();

					foreach (var channel in channelsCanRead)
					{
						bool alreadyExists = await _hitsContext.LastReadChannelMessage
							.AnyAsync(lr => lr.UserId == user.Id && lr.TextChannelId == channel);

						if (!alreadyExists)
						{
							var lastMessageId = await _hitsContext.ChannelMessage
								.Where(m => m.TextChannelId == channel)
								.OrderByDescending(m => m.Id)
								.Select(m => (long?)m.Id)
								.FirstOrDefaultAsync() ?? 0;

							var lastRead = new LastReadChannelMessageDbModel
							{
								UserId = user.Id,
								TextChannelId = channel,
								LastReadedMessageId = lastMessageId
							};

							await _hitsContext.LastReadChannelMessage.AddAsync(lastRead);
						}
					}
					await _hitsContext.SaveChangesAsync();

					var newUserRole = new NewUserRoleResponseDTO
					{
						ServerId = server.Id,
						UserId = user.Id,
						RoleId = serverRole.Id,
					};
					if (alertedUsers != null && alertedUsers.Count() > 0)
					{
						await _webSocketManager.BroadcastMessageAsync(newUserRole, alertedUsers, "Role added to user");
					}
				}
			}
		}

		var response = new ServerPresetItemDTO
		{
			ServerRoleId = serverRole.Id,
			ServerRoleName = serverRole.Name,
			SystemRoleId = systemRole.Id,
			SystemRoleName = systemRole.Name,
			SystemRoleType = systemRole.Type
		};
		return response;
	}

	public async Task DeletePresetAdminAsync(string token, Guid serverId, Guid serverRoleId, Guid systemRoleId)
	{
		var admin = await GetAdminAsync(token);

		var server = await _hitsContext.Server
			.FirstOrDefaultAsync(s => s.Id == serverId);
		if (server.ServerType != ServerTypeEnum.Teacher)
		{
			throw new CustomException("Server isnt teachers", "DeletePresetAdminAsync", "Server", 401, "Сервер не является учительсяким", "Удаление пресета админом");
		}

		var serverRole = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == serverRoleId && r.ServerId == server.Id);
		if (serverRole == null)
		{
			throw new CustomException("Server role not found", "DeletePresetAdminAsync", "Server role", 404, "Серверная роль не найдена", "Удаление пресета админом");
		}

		var systemRole = await _hitsContext.SystemRole.FirstOrDefaultAsync(r => r.Id == systemRoleId);
		if (systemRole == null)
		{
			throw new CustomException("System role not found", "DeletePresetAdminAsync", "System role", 404, "Системная роль не найдена", "Удаление пресета админом");
		}

		var preset = await _hitsContext.Preset.FirstOrDefaultAsync(p => p.SystemRoleId == systemRole.Id && p.ServerRoleId == serverRole.Id);
		if (preset == null)
		{
			throw new CustomException("Preset not found", "DeletePresetAdminAsync", "Preset", 404, "Пресет не найден", "Удаление пресета админом");
		}

		var usersSubs = await _hitsContext.UserServer
			.Include(us => us.SubscribeRoles)
				.ThenInclude(sr => sr.Role)
			.Where(us => us.ServerId == server.Id
				&& us.SubscribeRoles.Any(sr => sr.RoleId == serverRole.Id))
			.ToListAsync();

		foreach (var sub in usersSubs)
		{
			var alertedUsers = await _hitsContext.UserServer.Where(us => us.ServerId == server.Id).Select(us => us.UserId).ToListAsync();
			if (sub.SubscribeRoles.Count() == 1)
			{
				var userVoiceChannel = await _hitsContext.UserVoiceChannel
					.Include(us => us.VoiceChannel)
					.FirstOrDefaultAsync(us =>
						us.VoiceChannel.ServerId == server.Id
						&& us.UserId == sub.UserId);
				if (userVoiceChannel != null)
				{
					_hitsContext.UserVoiceChannel.Remove(userVoiceChannel);
				}

				var lastMessage = await _hitsContext.LastReadChannelMessage.Include(lr => lr.TextChannel).Where(lr => lr.UserId == sub.UserId && lr.TextChannel.ServerId == server.Id).ToListAsync();
				_hitsContext.LastReadChannelMessage.RemoveRange(lastMessage);

				var nonNitifiables = await _hitsContext.NonNotifiableChannel.Where(nnc => nnc.UserServerId == sub.Id).ToListAsync();
				_hitsContext.NonNotifiableChannel.RemoveRange(nonNitifiables);

				_hitsContext.UserServer.Remove(sub);
				await _hitsContext.SaveChangesAsync();

				var newUnsubscriberResponse = new UnsubscribeResponseDTO
				{
					ServerId = server.Id,
					UserId = sub.UserId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(newUnsubscriberResponse, alertedUsers, "User unsubscribe");
				}
			}
			else
			{
				var deletedRole = sub.SubscribeRoles.FirstOrDefault(usr => usr.RoleId == serverRole.Id);

				sub.SubscribeRoles.Remove(deletedRole);
				_hitsContext.UserServer.Update(sub);
				await _hitsContext.SaveChangesAsync();

				var removedChannels = await _hitsContext.ChannelCanSee
					.Where(ccs => ccs.RoleId == deletedRole.RoleId)
					.Select(ccs => ccs.ChannelId)
					.ToListAsync();
				foreach (var channelId in removedChannels)
				{
					bool stillHasAccess = await _hitsContext.ChannelCanSee
						.AnyAsync(ccs => removedChannels.Contains(ccs.ChannelId)
										 && sub.SubscribeRoles.Select(sr => sr.RoleId).Contains(ccs.RoleId));

					if (!stillHasAccess)
					{
						var lastRead = await _hitsContext.LastReadChannelMessage
							.FirstOrDefaultAsync(lr => lr.UserId == sub.UserId && lr.TextChannelId == channelId);

						if (lastRead != null)
							_hitsContext.LastReadChannelMessage.Remove(lastRead);
					}
				}
				await _hitsContext.SaveChangesAsync();

				var oldUserRole = new NewUserRoleResponseDTO
				{
					ServerId = server.Id,
					UserId = sub.UserId,
					RoleId = deletedRole.RoleId,
				};
				if (alertedUsers != null && alertedUsers.Count() > 0)
				{
					await _webSocketManager.BroadcastMessageAsync(oldUserRole, alertedUsers, "Role removed from user");
				}
			}
		}

		_hitsContext.Preset.Remove(preset);
		await _hitsContext.SaveChangesAsync();
	}
}