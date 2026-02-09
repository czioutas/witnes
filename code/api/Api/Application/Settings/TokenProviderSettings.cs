using Microsoft.IdentityModel.Tokens;

namespace Api.Application.Settings
{
    public class TokenProviderSettings
    {
        public string Path { get; set; } = "/account/login";
        public string Issuer { get; set; } = "WitnesIssuer";
        public string Audience { get; set; } = "WitnesAudience";
        public TimeSpan ShortExpiration { get; set; } = TimeSpan.FromDays(1);
        public TimeSpan LongExpiration { get; set; } = TimeSpan.FromDays(3);
        public string SecretKey { get; set; } = "asdasdasdasdsdasdasdasdasdasdasdasdasds";
        public SigningCredentials? SigningCredentials { get; set; }
    }
}
