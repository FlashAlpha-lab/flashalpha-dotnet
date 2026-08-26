using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /health</c> (public, no authentication required).
///
/// <para>Liveness probe. Public endpoint, not rate-limited — safe to poll.</para>
/// </summary>
public sealed class HealthResponse : FlashAlphaResponse
{
    /// <summary>Health string. Typically <c>"Healthy"</c>.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
