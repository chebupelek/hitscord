using hitscord_net.Models.DBModels;
using System.Security.Claims;
using System.Security.Cryptography;

namespace hitscord_net.JwtCreation
{
    public static class JwtRefresh
    {
        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
    }
}