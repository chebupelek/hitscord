using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace hitscord.JwtCreation
{
    public static class JwtTokenCreator
    {
        public static JwtSecurityToken CreateJwtTokenAccess(this IEnumerable<Claim> claims, IConfiguration configuration)
        {
            var expire = configuration.GetSection("Jwt:ExpireAccess").Get<int>();

            return new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddDays(expire),
                signingCredentials: configuration.CreateSigningCredentials()
            );
        }

        public static JwtSecurityToken CreateJwtTokenRefresh(this IEnumerable<Claim> claims, IConfiguration configuration)
        {
            var expire = configuration.GetSection("Jwt:ExpireRefresh").Get<int>();

            return new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddDays(expire),
                signingCredentials: configuration.CreateSigningCredentials()
            );
        }

        public static JwtSecurityToken CreateJwtTokenApplication(this IEnumerable<Claim> claims, IConfiguration configuration)
        {
            var expire = configuration.GetSection("Jwt:ExpireApplicztion").Get<int>();

            return new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(expire),
                signingCredentials: configuration.CreateSigningCredentials()
            );
        }
    }
}
