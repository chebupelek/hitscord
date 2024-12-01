using Authzed.Api.V0;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.Models.InnerModels;
using hitscord_net.OtherFunctions.EmailServer;
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
using IEmailSender = hitscord_net.OtherFunctions.EmailServer.IEmailSender;

namespace hitscord_net.Services;

public class AuthService : IAuthService
{
    private readonly HitsContext _hitsContext;
    private readonly PasswordHasher<string> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;

    public AuthService(HitsContext hitsContext, ITokenService tokenService, IEmailSender emailSender)
    {
        _hitsContext = hitsContext ?? throw new ArgumentNullException(nameof(hitsContext));
        _passwordHasher = new PasswordHasher<string>();
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    }

    private async Task<UserDbModel> GetUserAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                throw new ProfrileNotFoundException("UserId not found", "Profile", "Access token");
            }
            Guid userIdGuid = Guid.Parse(userId);
            var user = await _hitsContext.User.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (user == null) 
            {
                throw new ProfrileNotFoundException("User not found", "Profile", "User");
            }
            return user;
        }
        catch(Exception ex) 
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
                throw new CheckAccountExistRegistrationException("Account with this mail already exist", "Account", "Mail");
            }

            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.AccountName == registrationData.AccountName) != null)
            {
                throw new CheckAccountExistRegistrationException("Account with this account name already exist", "Account", "Account name");
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
        catch (CheckAccountExistRegistrationException ex)
        {
            throw new CheckAccountExistRegistrationException(ex.Message, ex.Type, ex.Object);
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
                throw new AuthCheckException("A user with this email doesnt exists", "Login", "Email");
            }

            var passwordcheck = _passwordHasher.VerifyHashedPassword(loginData.Mail, userData.PasswordHash, loginData.Password);

            if (passwordcheck == PasswordVerificationResult.Failed)
            {
                throw new AuthCheckException("Wrong password", "Login", "Password");
            }

            var tokens = _tokenService.CreateTokens(userData);
            await _tokenService.ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, userData.Id);

            return tokens;
        }
        catch (AuthCheckException ex)
        {
            throw new AuthCheckException(ex.Message, ex.Type, ex.Object);
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
            if(!await _tokenService.IsTokenValidAsync(token)) 
            {
                throw new LogoutException("Access token not found", "Logout", "Access token");
            }

            if(_tokenService.IsTokenExpired(token))
            {
                throw new LogoutException("Access token expired", "Logout", "Access token");
            }

            await _tokenService.InvalidateTokenAsync(token);
        }
        catch (LogoutException ex)
        {
            throw new LogoutException(ex.Message, ex.Type, ex.Object);
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
                throw new RefreshException("Refresh token not found", "Refresh", "Refresh token");
            }

            if (_tokenService.IsTokenExpired(token))
            {
                throw new RefreshException("Refresh token expired", "Refresh", "Refresh token");
            }

            var tokens = await _tokenService.UpdateTokens(token);
            return tokens;
        }
        catch (RefreshException ex)
        {
            throw new RefreshException(ex.Message, ex.Type, ex.Object);
        }
        catch (RefreshNotFoundException ex)
        {
            throw new RefreshNotFoundException(ex.Message, ex.Type, ex.Object);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<UserDbModel> GetProfileAsync(string token)
    {
        try
        {
            if (!await _tokenService.IsTokenValidAsync(token))
            {
                throw new ProfrileUnauthorizedException("Access token not found", "Get profile", "Access token");
            }

            if (_tokenService.IsTokenExpired(token))
            {
                throw new ProfrileUnauthorizedException("Access token expired", "Get profile", "Access token");
            }

            var userData = await GetUserAsync(token);
            return userData;
        }
        catch (ProfrileUnauthorizedException ex)
        {
            throw new ProfrileUnauthorizedException(ex.Message, ex.Type, ex.Object);
        }
        catch (ProfrileNotFoundException ex)
        {
            throw new ProfrileNotFoundException(ex.Message, ex.Type, ex.Object);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /*
    public async Task CreateRegistrationApplicationAsync(UserRegistrationDTO registrationData, string scheme, string host)
    {
        await ApplicationDataExistAsync(registrationData.Mail, registrationData.AccountName);
        var application = await AddRegistrationApplicationAsync(registrationData);

        var token = _tokenService.CreateApplicationToken(application);

        var verificationLink = $"{scheme}://{host}/api/user/verifyTest?token={token}";
        var emailBody = $"<p>Для завершения регистрации перейдите по ссылке:</p><a href='{verificationLink}'>Подтвердить регистрацию</a>";

        await _emailSender.SendEmailAsync(registrationData.Mail, "Подтверждение регистрации", emailBody);
    }

    public async Task VerifyAccountAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
        var decodedId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        Guid applicationId = Guid.Parse(decodedId);
        var application = await _hitsContext.RegistrationApplication.FirstOrDefaultAsync(a => a.Id == applicationId);
        if (application == null)
        {
            throw new ArgumentException("Application", new Exception("Application not exist"));
        }
        var newAccount = new UserDbModel()
        {
            Mail = application.Mail,
            PasswordHash = application.PasswordHash,
            AccountName = application.AccountName,
            AccountTag = application.AccountName
        };
        await _hitsContext.User.AddAsync(newAccount);
        _hitsContext.RegistrationApplication.Remove(application);
        await _hitsContext.SaveChangesAsync();
    }

    private async Task ApplicationDataExistAsync(string mail, string name)
    {
        try
        {
            if(await _hitsContext.RegistrationApplication.FirstOrDefaultAsync(u => u.Mail == mail) != null)
            {
                throw new ArgumentException("Application", new Exception("Application with this mail already exist"));
            }

            if (await _hitsContext.RegistrationApplication.FirstOrDefaultAsync(u => u.AccountName == name) != null)
            {
                throw new ArgumentException("Application", new Exception("Application with this account name already exist"));
            }

            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == mail) != null)
            {
                throw new ArgumentException("Account", new Exception("Account with this mail already exist"));
            }

            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.AccountName == name) != null)
            {
                throw new ArgumentException("Account", new Exception("Account with this account name already exist"));
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private async Task<RegistrationApplicationDbModel> AddRegistrationApplicationAsync(UserRegistrationDTO registrationData)
    {
        try
        {
            var newApplication = new RegistrationApplicationDbModel
            {
                AccountName = registrationData.AccountName,
                Mail = registrationData.Mail,
                PasswordHash = _passwordHasher.HashPassword(registrationData.Mail, registrationData.Password)
            };
            await _hitsContext.RegistrationApplication.AddAsync(newApplication);
            _hitsContext.SaveChanges();
            return newApplication;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    */
}
