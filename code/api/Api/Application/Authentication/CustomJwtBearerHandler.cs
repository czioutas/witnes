using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Application.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Application;

public class CustomJwtBearerHandler : JwtBearerHandler
{
    private readonly TokenProviderSettings _settings;
    private readonly TimeProvider _timeProvider;

    public CustomJwtBearerHandler(
        TokenProviderSettings settings,
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TimeProvider timeProvider
    ) : base(options, logger, encoder)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(TokenProviderSettings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get the token from the Authorization header
        if (!Context.Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authorizationHeader = authorizationHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var principal = GetClaims(token);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "CustomJwtBearer")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex.Message));
        }
    }

    private ClaimsPrincipal GetClaims(string token)
    {
        var dt = _timeProvider.GetUtcNow();

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _settings.SigningCredentials?.Key,
            ValidateLifetime = false,
        };

        var handler = new JwtSecurityTokenHandler();

        var claimsPrincipal = handler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

        var jwtToken = handler.ReadJwtToken(token) as JwtSecurityToken;

        if (dt.DateTime > jwtToken.ValidTo)
        {
            throw new SecurityTokenExpiredException("Token expired.");
        }

        var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "Token");
        return new ClaimsPrincipal(claimsIdentity);
    }
}
