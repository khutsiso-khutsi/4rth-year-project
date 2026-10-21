using System.Threading.Tasks;

namespace LabManager.Repository
{
    public interface IGoogleTokenService
    {
        // Exchange a refresh token for an access token (handles refresh).
        Task<string> GetAccessTokenAsync(string refreshToken);
    }
}
