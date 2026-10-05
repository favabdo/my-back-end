using System.Text.Json;

namespace NileTechno.API.Auth;

/// <summary>
/// Lets a JWT arrive as `accessToken` inside a JSON request body for the address endpoints only,
/// and only when no Authorization header was sent. The token still goes through the normal JwtBearer
/// validation (same key, issuer, audience, lifetime), so this changes where it is read, not what it grants.
/// </summary>
public static class BodyAccessTokenReader
{
    private const long MaxBodyBytes = 32 * 1024;
    private const string AllowedPrefix = "/api/addresses";

    public static bool AppliesTo(HttpRequest request)
        => request.Method is "POST" or "PUT"
            && request.Path.StartsWithSegments(AllowedPrefix);

    public static async Task<string?> ReadAsync(HttpRequest request)
    {
        // A known oversized body is skipped; an unknown length (HTTP/2 or a proxy that drops
        // Content-Length) must still be read, otherwise the fallback silently does nothing.
        if (request.ContentLength > MaxBodyBytes) return null;

        request.EnableBuffering();
        try
        {
            using var doc = await JsonDocument.ParseAsync(request.Body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (!doc.RootElement.TryGetProperty("accessToken", out var value) || value.ValueKind != JsonValueKind.String)
                return null;

            // Hand-tested callers often paste the token with its scheme; the validator wants the bare JWT.
            var raw = value.GetString()?.Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? raw[7..].Trim() : raw;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}
