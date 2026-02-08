using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Application.Account.Models;
using Api.Application.Account.Services.Interfaces;
using Api.Application.Authentication;
using Api.Application.Settings;
using Microsoft.IdentityModel.Tokens;

namespace Api.Application.Account.Services;

public class TokenService : ITokenService
{
    private readonly TokenProviderSettings _tokenProviderSettings;
    private readonly TimeProvider _timeProvider;

    public TokenService(
        TimeProvider timeProvider,
        TokenProviderSettings tokenProviderSettings)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tokenProviderSettings = tokenProviderSettings ?? throw new ArgumentNullException(nameof(tokenProviderSettings));
    }

    public TokenModel GenerateToken(ApplicationUserModel user, IList<string> roles, Guid? businessId = null)
    {
        var nowDt = _timeProvider.GetUtcNow();

        return new TokenModel()
        {
            AccessToken = CreateToken(user, roles, nowDt, false, businessId),
            RefreshToken = CreateToken(user, roles, nowDt, true, businessId),
            CreatedAt = nowDt
        };
    }

    public TokenModel GenerateShortSpanToken(ApplicationUserModel user, IList<string> roles, Guid? businessId = null)
    {
        var nowDt = _timeProvider.GetUtcNow();

        TimeSpan oneMillisecond = new TimeSpan(0, 0, 0, 0, 1);

        var token = new TokenModel()
        {
            AccessToken = CreateToken(user, roles, nowDt, oneMillisecond, businessId),
            RefreshToken = CreateToken(user, roles, nowDt, oneMillisecond, businessId),
            CreatedAt = nowDt
        };

        return token;
    }


    public TokenModel RefreshToken(string refreshToken)
    {
        var nowDt = _timeProvider.GetUtcNow();

        // validate refreshToken
        var principal = GetPrincipalFromToken(refreshToken, nowDt) ?? throw new BadHttpRequestException("Invalid access token or refresh token");

        // Create a new instance of TokenModel
        return new TokenModel()
        {
            AccessToken = CreateTokenFromLLT(new JwtSecurityToken(refreshToken), nowDt),
            RefreshToken = refreshToken,
            CreatedAt = nowDt
        };

    }

    public string CreateTokenFromLLT(JwtSecurityToken longLivedToken, DateTimeOffset nowDt)
    {
        TimeSpan _expiration = _tokenProviderSettings.ShortExpiration;

        // Create the JWT and write it to a string
        JwtSecurityToken jwt = new JwtSecurityToken(
            issuer: _tokenProviderSettings.Issuer,
            audience: _tokenProviderSettings.Audience,
            claims: longLivedToken.Claims,
            notBefore: nowDt.UtcDateTime,
            expires: nowDt.UtcDateTime.Add(_expiration),
            signingCredentials: _tokenProviderSettings.SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    // this is not good enough
    // we must do it like dis https://stackoverflow.com/questions/42036810/asp-net-core-jwt-mapping-role-claims-to-claimsidentity/42037615#42037615
    public string CreateToken(ApplicationUserModel user, IList<string> roles, DateTimeOffset nowDt, bool longLived, Guid? businessId = null)
    {
        var claims = GetValidClaims(user, roles, nowDt, businessId);

        TimeSpan expiration = longLived ? _tokenProviderSettings.LongExpiration : _tokenProviderSettings.ShortExpiration;

        // Create the JWT and write it to a string
        JwtSecurityToken jwt = new JwtSecurityToken(
            issuer: _tokenProviderSettings.Issuer,
            audience: _tokenProviderSettings.Audience,
            claims: claims,
            notBefore: nowDt.UtcDateTime,
            expires: nowDt.UtcDateTime.Add(expiration),
            signingCredentials: _tokenProviderSettings.SigningCredentials);


        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }


    public string CreateToken(ApplicationUserModel user, IList<string> roles, DateTimeOffset nowDt, TimeSpan specificTimespan, Guid? businessId = null)
    {
        var claims = GetValidClaims(user, roles, nowDt, businessId);

        TimeSpan _expiration = specificTimespan;

        // Create the JWT and write it to a string
        JwtSecurityToken jwt = new JwtSecurityToken(
            issuer: _tokenProviderSettings.Issuer,
            audience: _tokenProviderSettings.Audience,
            claims: claims,
            notBefore: nowDt.UtcDateTime,
            expires: nowDt.UtcDateTime.Add(_expiration),
            signingCredentials: _tokenProviderSettings.SigningCredentials);


        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public List<Claim> GetValidClaims(ApplicationUserModel user, IList<string> roles, DateTimeOffset nowDtO, Guid? businessId = null)
    {
        var claims = new List<Claim>()
            {
                new Claim(ClaimTypeConstants.TenantId, user.TenantId.ToString()),
                new Claim(ClaimTypeConstants.FirstName, user.FirstName),
                new Claim(ClaimTypeConstants.LastName, user.LastName),
                new Claim(ClaimTypeConstants.EmailVerified, user.EmailConfirmed.ToString().ToLower(), ClaimValueTypes.Boolean),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, nowDtO.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
            };

        if (businessId.HasValue)
        {
            claims.Add(new Claim(ClaimTypeConstants.DefaultBusinessId, businessId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, DateTimeOffset dt)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _tokenProviderSettings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _tokenProviderSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _tokenProviderSettings.SigningCredentials?.Key,
            ValidateLifetime = true,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        var jwtToken = tokenHandler.ReadJwtToken(token) as JwtSecurityToken;

        if (dt.UtcDateTime > jwtToken.ValidTo)
        {
            throw new SecurityTokenExpiredException("Token expired.");
        }

        return principal;

    }
}
