using System.Security.Claims;
using Api.Application.Account.Models;

namespace Api.Application.Account.Services.Interfaces;

public interface ITokenService
{
    TokenModel GenerateToken(ApplicationUserModel user, IList<string> roles, Guid? businessId = null);
    TokenModel GenerateShortSpanToken(ApplicationUserModel user, IList<string> roles, Guid? businessId = null);
    TokenModel RefreshToken(string refreshToken);

    string CreateToken(
        ApplicationUserModel user,
        IList<string> roles,
        DateTimeOffset nowDt,
        bool longLived,
        Guid? businessId = null
    );

    ClaimsPrincipal? GetPrincipalFromToken(string token, DateTimeOffset dt);

    List<Claim> GetValidClaims(ApplicationUserModel user, IList<string> roles, DateTimeOffset nowDt, Guid? businessId = null);
}
