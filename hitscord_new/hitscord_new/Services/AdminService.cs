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

namespace hitscord.Services;

public class AdminService: IAdminService
{
	private readonly IConfiguration _configuration;
	private readonly HitsContext _hitsContext;
	private readonly PasswordHasher<string> _passwordHasher;
	private readonly TokenContext _tokenContext;
	private readonly WebSocketsManager _webSocketManager;
	private readonly MinioService _minioService;

	public AdminService(TokenContext tokenContext, WebSocketsManager webSocketManager, HitsContext hitsContext, IConfiguration configuration, MinioService minioService)
	{
		_hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
		_webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
		_passwordHasher = new PasswordHasher<string>();
		_configuration = configuration;
		_tokenContext = tokenContext ?? throw new ArgumentNullException(nameof(tokenContext));
		_minioService = minioService ?? throw new ArgumentNullException(nameof(minioService));
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
				UserName = us.UserServerName,
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
				Id = r.ServerId,
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
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.ChannelId,
						Name = c.Channel.Name
					})
					.ToList(),
				ChannelCanWrite = r.ChannelCanWrite
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.TextChannelId,
						Name = c.TextChannel.Name
					})
					.ToList(),
				ChannelCanWriteSub = r.ChannelCanWriteSub
					.Select(c => new ChannelShortItemDTO
					{
						Id = c.TextChannelId,
						Name = c.TextChannel.Name
					})
					.ToList(),
				ChannelNotificated = r.ChannelNotificated
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
}