using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NileTechno.Application.Common;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Infrastructure.Services;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleTokenValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoogleIdentity?> ValidateAsync(string? idToken, string? accessToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idToken))
            return await ValidateIdTokenAsync(idToken.Trim(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
            return await ValidateAccessTokenAsync(accessToken.Trim(), cancellationToken);

        return null;
    }

    private async Task<GoogleIdentity?> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(GoogleTokenValidator));
        var response = await client.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google id token rejected with status {Status}", (int)response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>(cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
            return null;

        var clientId = _configuration["Google:ClientId"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        if (!string.IsNullOrWhiteSpace(clientId) &&
            !string.Equals(payload.Audience, clientId, StringComparison.Ordinal))
        {
            _logger.LogWarning("Google id token audience mismatch");
            return null;
        }

        return new GoogleIdentity(
            EmailNormalizer.Normalize(payload.Email),
            payload.Name ?? payload.Email.Split('@')[0],
            payload.Subject,
            IsVerified(payload.EmailVerified));
    }

    private async Task<GoogleIdentity?> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(GoogleTokenValidator));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google access token rejected with status {Status}", (int)response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Subject))
            return null;

        return new GoogleIdentity(
            EmailNormalizer.Normalize(payload.Email),
            payload.Name ?? payload.Email.Split('@')[0],
            payload.Subject,
            IsVerified(payload.EmailVerified));
    }

    private static bool IsVerified(JsonElement value) =>
        value.ValueKind == JsonValueKind.True
        || (value.ValueKind == JsonValueKind.String &&
            string.Equals(value.GetString(), "true", StringComparison.OrdinalIgnoreCase))
        || (value.ValueKind == JsonValueKind.Undefined);

    private sealed class GoogleTokenInfo
    {
        [JsonPropertyName("aud")]
        public string? Audience { get; set; }

        [JsonPropertyName("sub")]
        public string? Subject { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public JsonElement EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Subject { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public JsonElement EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
