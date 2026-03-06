using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.db;
using hitscord.Models.other;
using hitscord.Models.response;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace hitscord.Services;

public class CurrentUserService : ICurrentUserService
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public CurrentUserService(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public Guid UserId
	{
		get
		{
			var userId = _httpContextAccessor.HttpContext?
				.User
				.FindFirst(ClaimTypes.NameIdentifier)?
				.Value;

			if (userId == null)
				throw new UnauthorizedAccessException();

			return Guid.Parse(userId);
		}
	}
}
