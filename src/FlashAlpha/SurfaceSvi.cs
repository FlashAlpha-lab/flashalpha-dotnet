using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response for <c>GET /v1/surface/svi/{symbol}</c> (MCP <c>get_svi_params</c>).
///
/// <para>Live SVI-fitted volatility surface — the calibrated
/// <c>(a, b, ρ, m, σ)</c> parameters per expiry slice, with ATM total variance
/// and ATM IV. A lightweight subset of the full advanced-volatility payload for
/// clients that reconstruct the surface themselves. Requires Alpha+.</para>
/// </summary>
public sealed class SurfaceSviResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>Mid of the underlying.</summary>
    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("market_open")]
    public bool? MarketOpen { get; set; }

    /// <summary>SVI slices ordered by <see cref="SurfaceSviSlice.DaysToExpiry"/>.</summary>
    [JsonPropertyName("svi_parameters")]
    public List<SurfaceSviSlice>? SviParameters { get; set; }
}

/// <summary>One per-expiry SVI slice in <see cref="SurfaceSviResponse.SviParameters"/>.</summary>
public sealed class SurfaceSviSlice
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("days_to_expiry")]
    public int? DaysToExpiry { get; set; }

    /// <summary>Per-expiry forward price.</summary>
    [JsonPropertyName("forward")]
    public double? Forward { get; set; }

    /// <summary>Raw SVI level parameter.</summary>
    [JsonPropertyName("a")]
    public double? A { get; set; }

    /// <summary>Raw SVI angle/slope parameter.</summary>
    [JsonPropertyName("b")]
    public double? B { get; set; }

    /// <summary>Raw SVI rotation (skew) parameter.</summary>
    [JsonPropertyName("rho")]
    public double? Rho { get; set; }

    /// <summary>Raw SVI horizontal-shift parameter.</summary>
    [JsonPropertyName("m")]
    public double? M { get; set; }

    /// <summary>Raw SVI curvature (smoothness) parameter.</summary>
    [JsonPropertyName("sigma")]
    public double? Sigma { get; set; }

    [JsonPropertyName("atm_total_variance")]
    public double? AtmTotalVariance { get; set; }

    /// <summary>ATM implied vol as a percentage.</summary>
    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }
}
