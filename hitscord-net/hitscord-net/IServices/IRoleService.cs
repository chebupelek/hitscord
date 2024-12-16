using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using System.Runtime.CompilerServices;

namespace hitscord_net.IServices;

public interface IRoleService
{
    Task<RoleDbModel> CheckRoleExistAsync(string roleName);
    Task<RoleDbModel> CheckRoleExistByIdAsync(Guid roleId);
    Task CreateRolesAsync();
    Task<List<RoleDbModel>> GetRolesAsync();
}