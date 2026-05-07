using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/options/{ticker}</c> (Free+).
///
/// <para>Option chain metadata: every listed expiration for the symbol with
/// its full set of strikes. Useful for discovering valid (expiry, strike)
/// pairs before calling the priced-quote endpoints.</para>
/// </summary>
public sealed class OptionsMetaResponse
{
    /// <summary>Echoed underlying symbol (e.g. <c>"SPY"</c>).</summary>
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>One row per listed expiration, each with its strike list.</summary>
    [JsonPropertyName("expirations")]
    public List<OptionsMetaExpiration>? Expirations { get; set; }

    /// <summary>Length of <see cref="Expirations"/>.</summary>
    [JsonPropertyName("expiration_count")]
    public int? ExpirationCount { get; set; }

    /// <summary>Total number of listed contracts across all expirations (sum of strike counts).</summary>
    [JsonPropertyName("total_contracts")]
    public int? TotalContracts { get; set; }
}

/// <summary>
/// One row of <see cref="OptionsMetaResponse.Expirations"/> — one listed
/// expiration date plus the strikes available at it.
/// </summary>
public sealed class OptionsMetaExpiration
{
    /// <summary>Expiration date (<c>"yyyy-MM-dd"</c>).</summary>
    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    /// <summary>Listed strikes for this expiry (ascending).</summary>
    [JsonPropertyName("strikes")]
    public double[]? Strikes { get; set; }
}
