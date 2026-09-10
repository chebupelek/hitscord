using hitscord.Models.db;
using hitscord.Models.response;
using hitscord.Models.request;
using System.Runtime.CompilerServices;
using hitscord.Models.other;
using hitscord_new.Models.response;

namespace hitscord.IServices;

public interface ICurrentUserService
{
	Guid UserId { get; }
}