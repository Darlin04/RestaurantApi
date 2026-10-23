using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Restaurant.Infrastructure.Identity;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public (string Token, DateTimeOffset ExpiresAt) Generate(ApplicationUser user, IList<string> roles)
    {
        var jwt = options.Value;
        var keyBytes = GetKeyBytes(jwt.Key);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(jwt.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expires.UtcDateTime,
            SigningCredentials = credentials,
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);
        return (token, expires);
    }

    private static byte[] GetKeyBytes(string key)
    {
        try
        {
            return Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(key);
        }
    }
}
