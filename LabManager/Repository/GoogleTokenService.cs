using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using LabManager.Models;

namespace LabManager.Repository
{
    // Minimal implementation that exchanges refresh token for access token
    // using Google's token endpoint. For production, consider using Google.Apis.Auth.
    public class GoogleTokenService : IGoogleTokenService
    {
        private readonly EmailSettings _settings;
        private readonly HttpClient _httpClient = new HttpClient();

        public GoogleTokenService(EmailSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<string> GetAccessTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            var req = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _settings.OAuthClientId),
                new KeyValuePair<string, string>("client_secret", _settings.OAuthClientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            });
            req.Content = content;

            using var resp = await _httpClient.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("access_token", out var at))
                return at.GetString();

            throw new InvalidOperationException("Unable to obtain access token from token endpoint.");
        }
    }
}
