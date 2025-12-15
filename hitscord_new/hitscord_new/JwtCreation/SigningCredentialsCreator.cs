using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace hitscord.JwtCreation
{
	public static class SigningCredentialsCreator
	{
		public static SigningCredentials CreateSigningCredentials()
		{
			var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
							?? throw new InvalidOperationException("JWT_SECRET is not set");

			var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

			Console.WriteLine($"[DEBUG] Using JWT secret: {jwtSecret}");
			Console.WriteLine($"[DEBUG] Key length in bytes: {keyBytes.Length}");

			if (keyBytes.Length < 16)
				throw new InvalidOperationException("JWT_SECRET is too short. Minimum 16 bytes required for HS256.");

			return new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
		}
	}
}