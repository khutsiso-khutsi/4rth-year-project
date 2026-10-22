using System.Threading.Tasks;

namespace LabManager.Repository
{
    // Abstract storage for per-user refresh tokens (implement with DB or secure store)
    public interface IRefreshTokenStore
    {
        Task<string> GetRefreshTokenAsync(string userEmail);
        Task SaveRefreshTokenAsync(string userEmail, string refreshToken);
    }
}
