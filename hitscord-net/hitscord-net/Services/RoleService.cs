using Authzed.Api.V0;
using Grpc.Core;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace hitscord_net.Services;

public class RoleService : IRoleService
{
    private readonly HitsContext _hitsContext;

    public RoleService(HitsContext hitsContext)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
    }

    public async Task<RoleDbModel> CheckRoleExistAsync(string roleName)
    {
        try
        {
            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                throw new CustomException($"{roleName} role not found", "Check role for existing", "Role", 404);
            }
            return role;
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

    public async Task<RoleDbModel> CheckRoleExistByIdAsync(Guid roleId)
    {
        try
        {
            var role = await _hitsContext.Role.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                throw new CustomException($"Role with id {roleId} not found", "Check role for existing by Id", "Role Id", 404);
            }
            return role;
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

    public async Task CreateRolesAsync()
    {
        try
        {
            var roles = await _hitsContext.Role.ToListAsync();
            if (roles != null && roles.Count > 0)
            {
                throw new CustomException("Roles already created", "Create roles", "Roles", 400);
            }
            var adminRole = new RoleDbModel
            {
                Name = "Admin",
            };
            var teacherRole = new RoleDbModel
            {
                Name = "Teacher",
            };
            var studentRole = new RoleDbModel
            {
                Name = "Student",
            };
            var uncertainRole = new RoleDbModel
            {
                Name = "Uncertain",
            };
            _hitsContext.Role.Add(adminRole);
            await _hitsContext.SaveChangesAsync();
            _hitsContext.Role.Add(teacherRole);
            await _hitsContext.SaveChangesAsync();
            _hitsContext.Role.Add(studentRole);
            await _hitsContext.SaveChangesAsync();
            _hitsContext.Role.Add(uncertainRole);
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

    public async Task<List<RoleDbModel>> GetRolesAsync()
    {
        try
        {
            return (await _hitsContext.Role.ToListAsync());
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