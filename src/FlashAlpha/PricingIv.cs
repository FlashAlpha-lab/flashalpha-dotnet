using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/pricing/iv</c> (Free+).
///
/// <para>Implied volatility from a market option price via Newton-Raphson root
/// finding. Returns <see cref="ImpliedVolatility"/> as a decimal (e.g. 0.18 =
/// 18%) and <see cref="ImpliedVolatilityPct"/> as a percent (e.g. 18.0).</para>
/// </summary>
public sealed class PricingIvResponse
{
    /// <summary>Echo of the request inputs.</summary>
    [JsonPropertyName("inputs")]
    public PricingIvInputs? Inputs { get; set; }

    /// <summary>Implied volatility as a decimal (e.g. <c>0.18</c> = 18%).</summary>
    [JsonPropertyName("implied_volatility")]
    public double? ImpliedVolatility { get; set; }

    /// <summary>Implied volatility as a percent (e.g. <c>18.0</c>).</summary>
    [JsonPropertyName("implied_volatility_pct")]
    public double? ImpliedVolatilityPct { get; set; }
}

/// <summary>Echo of the request inputs for <c>/v1/pricing/iv</c>.</summary>
public sealed class PricingIvInputs
{
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("dte")]
    public double? Dte { get; set; }

    /// <summary>Market option price (mid) — the input to the IV solver.</summary>
    [JsonPropertyName("price")]
    public double? Price { get; set; }

    /// <summary><c>"call"</c> or <c>"put"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Risk-free rate as a decimal (e.g. <c>0.045</c> = 4.5%).</summary>
    [JsonPropertyName("risk_free_rate")]
    public double? RiskFreeRate { get; set; }

    /// <summary>Dividend yield as a decimal (e.g. <c>0.013</c> = 1.3%).</summary>
    [JsonPropertyName("dividend_yield")]
    public double? DividendYield { get; set; }
}
