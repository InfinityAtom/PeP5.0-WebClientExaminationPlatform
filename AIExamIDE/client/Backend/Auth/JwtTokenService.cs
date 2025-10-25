using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AIExamIDE.Backend.Auth;

public class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly byte[] _secretBytes;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _secretBytes = Encoding.UTF8.GetBytes(_options.Secret);
    }

    public string CreateToken(int userId, string email, string name, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role)
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(_secretBytes), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow + _options.TokenLifetime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
