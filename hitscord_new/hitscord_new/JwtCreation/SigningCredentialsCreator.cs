using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace hitscord.JwtCreation
{
	public static class SigningCredentialsCreator
	{
		public static SigningCredentials CreateSigningCredentials(this IConfiguration configuration)
		{
			var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
							?? throw new InvalidOperationException("JWT_SECRET is not set");

			var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

			if (keyBytes.Length < 16)
				throw new InvalidOperationException("JWT_SECRET is too short. Minimum 16 bytes required for HS256.");

			return new SigningCredentials(
				new SymmetricSecurityKey(keyBytes),
				SecurityAlgorithms.HmacSha256
			);
		}
	}
}