using Authzed.Api.V0;
using hitscord_net.Data.Contexts;
using hitscord_net.IServices;
using hitscord_net.Models.DBModels;
using hitscord_net.Models.DTOModels.RequestsDTO;
using hitscord_net.Models.DTOModels.ResponseDTO;
using hitscord_net.OtherFunctions.EmailServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public async Task<TokensDTO> CreateAccount(UserRegistrationDTO registrationData)
    {
        try
        {
            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == registrationData.Mail) != null)
            {
                throw new ArgumentException("Account", new Exception("Account with this mail already exist"));
            }

            if (await _hitsContext.User.FirstOrDefaultAsync(u => u.AccountName == registrationData.AccountName) != null)
            {
                throw new ArgumentException("Account", new Exception("Account with this account name already exist"));
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
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public async Task<TokensDTO> LoginTestAsync(LoginDTO loginData)
    {
        try
        {
            var userData = await _hitsContext.User.FirstOrDefaultAsync(u => u.Mail == loginData.Mail);
            if (userData == null)
            {
                throw new ArgumentException("A user with this email doesnt exists");
            }

            var logresult = _passwordHasher.VerifyHashedPassword(loginData.Mail, userData.PasswordHash, loginData.Password);

            if (logresult == PasswordVerificationResult.Failed)
            {
                throw new ArgumentException("Wrong password");
            }

            var tokens = _tokenService.CreateTokens(userData);
            await _tokenService.ValidateTokenAsync(tokens.AccessToken, tokens.RefreshToken, userData.Id);

            return tokens;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

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
}
