using Authzed.Api.V0;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace hitscord_net.Services;

public class AuthService : IAuthService
{
    private readonly HitsContext _hitsContext;
    private readonly PasswordHasher<string> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(HitsContext hitsContext, ITokenService tokenService)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _passwordHasher = new PasswordHasher<string>();
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<bool> CheckUserAuthAsync(string token)
    {
        try
        {
            if (!await _tokenService.IsTokenValidAsync(token))
            {
                throw new CustomException("Access token not found", "CheckAuth", "Access token", 401);
            }

            if (_tokenService.IsTokenExpired(token))
            {
                throw new CustomException("Access token expired", "CheckAuth", "Access token", 401);
            }

            return true;
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

    public async Task<UserDbModel> GetUserByTokenAsync(string token)
    {
        try
        {
            await CheckUserAuthAsync(token);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new CustomException("UserId not found", "Profile", "Access token", 404);
            }
            Guid userIdGuid = Guid.Parse(userId);
            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (user == null) 
            {
                throw new CustomException("User not found", "Profile", "User", 404);
            }
            return user;
        }
        catch (CustomException ex)
        {
            throw new CustomException(ex.Message, ex.Type, ex.Object, ex.Code);
        }
        catch(Exception ex) 
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<UserDbModel> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id ==  userId);
            if (user == null)
            {
                throw new CustomException("User not found", "Get user by id", "User", 404);
            }
            return user;
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

    public async Task<Guid?> GetUserIdAsync(string token)
    {
        try
        {
            await CheckUserAuthAsync(token);

            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new CustomException("UserId not found", "Profile", "Access token", 404);
            }
            Guid userIdGuid = Guid.Parse(userId);
            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (user == null)
            {
                throw new CustomException("User not found", "Profile", "User", 404);
            }
            if (user.Id == null)
            {
                throw new CustomException("UserId error", "Profile", "User", 404);
            }
            return user.Id;
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

    public async Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData)
    {
        try
        {
            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == registrationData.Mail) != null)
            {
                throw new CustomException("Account with this mail already exist", "Account", "Mail", 400);
            }

            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.AccountName == registrationData.AccountName) != null)
            {
                throw new CustomException("Account with this account name already exist", "Account", "Account name", 400);
            }

            var newUser = new UserDbModel
            {
                Mail = registrationData.Mail,
                PasswordHash = _passwordHasher.HashPassword(registrationData.Mail, registrationData.Password),
                AccountName = registrationData.AccountName,
                AccountTag = Regex.Replace(registrationData.AccountName, "[^a-zA-Z0-9]", "").ToLower()
            };
            await _hitsContext.User.AddAsync(newUser);
            _hitsContext.SaveChanges();

            var tokens = _tokenService.CreateTokens(newUser);
            await _tokenService.ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, newUser.Id);

            return tokens;
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

    public async Task<TokensDTO> LoginAsync(LoginDTO loginData)
    {
        try
        {
            var userData = await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == loginData.Mail);
            if (userData == null)
            {
                throw new CustomException("A user with this email doesnt exists", "Login", "Email", 400);
            }

            var passwordcheck = _passwordHasher.VerifyHashedPassword(loginData.Mail, userData.PasswordHash, loginData.Password);

            if (passwordcheck == PasswordVerificationResult.Failed)
            {
                throw new CustomException("Wrong password", "Login", "Password", 400);
            }

            var tokens = _tokenService.CreateTokens(userData);
            await _tokenService.ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, userData.Id);

            return tokens;
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

    public async Task LogoutAsync(string token)
    {
        try
        {
            await CheckUserAuthAsync(token);

            await _tokenService.InvalidateTokenAsync(token);
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

    public async Task<TokensDTO> RefreshTokensAsync(string token)
    {
        try
        {
            if (!await _tokenService.CheckRefreshToken(token))
            {
                throw new CustomException("Refresh token not found", "Refresh", "Refresh token", 401);
            }

            if (_tokenService.IsTokenExpired(token))
            {
                await _tokenService.InvalidateRefreshTokenAsync(token);
                throw new CustomException("Refresh token expired", "Refresh", "Refresh token", 401);
            }

            var tokens = await _tokenService.UpdateTokens(token);
            return tokens;
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

    public async Task<ProfileDTO> GetProfileAsync(string token)
    {
        try
        {
            await CheckUserAuthAsync(token);

            var userData = new ProfileDTO(await GetUserByTokenAsync(token));
            return userData;
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
