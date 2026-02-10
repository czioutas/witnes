using System.Security.Cryptography;
using System.Text;

namespace Api.Product.DailySalt;

public interface IVisitorHashService
{
    /// <summary>
    /// Computes a visitor hash ID from IP and User-Agent using the daily salt.
    /// Format: "w-gid_" + base64(SHA256(ip + userAgent + salt))[:16]
    /// </summary>
    Task<string> ComputeHashIdAsync(string ip, string userAgent);
}

public class VisitorHashService : IVisitorHashService
{
    private readonly IDailySaltService _saltService;

    public VisitorHashService(IDailySaltService saltService)
    {
        _saltService = saltService;
    }

    public async Task<string> ComputeHashIdAsync(string ip, string userAgent)
    {
        var salt = await _saltService.GetTodaySaltAsync();
        var input = $"{ip}{userAgent}{salt}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var b64 = Convert.ToBase64String(hashBytes);
        return $"w-gid_{b64[..16]}";
    }
}
