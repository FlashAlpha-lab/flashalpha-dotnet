using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/adv_volatility/{symbol}</c> (Alpha+).
///
/// <para>Advanced volatility analytics: per-expiry SVI parameter set, forward
/// prices, full total-variance surface (moneyness × tenor grid), butterfly /
/// calendar arbitrage flags, variance swap fair values, and second-/third-order
/// greek surfaces (vanna, charm, volga, speed).</para>
///
/// <para>Returns 403 <c>tier_restricted</c> for anything below the Alpha plan.</para>
/// </summary>
public sealed class AdvVolatilityResponse
{
    /// <summary>Echoed from the request path (e.g. <c>"SPY"</c>).</summary>
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("market_open")]
    public bool? MarketOpen { get; set; }

    /// <summary>Per-expiry SVI parameter set: (a, b, ρ, m, σ) plus forward and ATM-total-variance.</summary>
    [JsonPropertyName("svi_parameters")]
    public List<AdvVolatilitySviParams>? SviParameters { get; set; }

    /// <summary>Per-expiry forward prices and basis vs spot.</summary>
    [JsonPropertyName("forward_prices")]
    public List<AdvVolatilityForwardPrice>? ForwardPrices { get; set; }

    /// <summary>Total variance surface — log-moneyness × tenor grid plus implied-vol grid.</summary>
    [JsonPropertyName("total_variance_surface")]
    public AdvVolatilityVarianceSurface? TotalVarianceSurface { get; set; }

    /// <summary>Detected butterfly / calendar arbitrage violations across the surface.</summary>
    [JsonPropertyName("arbitrage_flags")]
    public List<AdvVolatilityArbitrageFlag>? ArbitrageFlags { get; set; }

    /// <summary>Variance swap fair values (variance + vol form) per expiry, with convexity adjustment.</summary>
    [JsonPropertyName("variance_swap_fair_values")]
    public List<AdvVolatilityVarianceSwap>? VarianceSwapFairValues { get; set; }

    /// <summary>Second-/third-order greek surfaces — vanna, charm, volga, speed.</summary>
    [JsonPropertyName("greeks_surfaces")]
    public AdvVolatilityGreeksSurfaces? GreeksSurfaces { get; set; }
}

/// <summary>
/// SVI (Stochastic Volatility Inspired) parameters for one expiry.
///
/// <para>The Gatheral SVI parameterisation: <c>w(k) = a + b · (ρ·(k-m) + sqrt((k-m)² + σ²))</c>
/// where <c>w</c> is total variance and <c>k</c> is log-moneyness.</para>
/// </summary>
public sealed class AdvVolatilitySviParams
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("days_to_expiry")]
    public int? DaysToExpiry { get; set; }

    /// <summary>Forward price for this expiry.</summary>
    [JsonPropertyName("forward")]
    public double? Forward { get; set; }

    /// <summary>SVI level parameter (vertical translation).</summary>
    [JsonPropertyName("a")]
    public double? A { get; set; }

    /// <summary>SVI angle parameter (slope of the wings).</summary>
    [JsonPropertyName("b")]
    public double? B { get; set; }

    /// <summary>SVI correlation parameter; controls left/right wing asymmetry. Range: [-1, 1].</summary>
    [JsonPropertyName("rho")]
    public double? Rho { get; set; }

    /// <summary>SVI horizontal-translation parameter.</summary>
    [JsonPropertyName("m")]
    public double? M { get; set; }

    /// <summary>SVI smoothness parameter (curvature near ATM).</summary>
    [JsonPropertyName("sigma")]
    public double? Sigma { get; set; }

    /// <summary>Total variance at the money (= w(0) under the SVI fit).</summary>
    [JsonPropertyName("atm_total_variance")]
    public double? AtmTotalVariance { get; set; }

    /// <summary>Implied vol at the money (annualised %) derived from the fit.</summary>
    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }
}

/// <summary>Forward price + basis for one expiry.</summary>
public sealed class AdvVolatilityForwardPrice
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("days_to_expiry")]
    public int? DaysToExpiry { get; set; }

    [JsonPropertyName("forward")]
    public double? Forward { get; set; }

    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    /// <summary><c>(forward - spot) / spot * 100</c>. Positive = forward over spot.</summary>
    [JsonPropertyName("basis_pct")]
    public double? BasisPct { get; set; }
}

/// <summary>
/// Full total-variance surface as parallel arrays plus 2D matrices.
///
/// <para><see cref="TotalVariance"/> and <see cref="ImpliedVol"/> are
/// indexed as <c>[moneyness_idx][tenor_idx]</c> — match the outer dimension
/// to <see cref="Moneyness"/> and the inner to <see cref="Tenors"/>.</para>
/// </summary>
public sealed class AdvVolatilityVarianceSurface
{
    /// <summary>Log-moneyness grid axis.</summary>
    [JsonPropertyName("moneyness")]
    public double[]? Moneyness { get; set; }

    /// <summary>Expiry date strings paralleling <see cref="Tenors"/>.</summary>
    [JsonPropertyName("expiries")]
    public string[]? Expiries { get; set; }

    /// <summary>Tenor (days-to-expiry, in years) grid axis.</summary>
    [JsonPropertyName("tenors")]
    public double[]? Tenors { get; set; }

    /// <summary>Total-variance matrix — <c>[moneyness][tenor]</c>.</summary>
    [JsonPropertyName("total_variance")]
    public double[][]? TotalVariance { get; set; }

    /// <summary>Implied-vol matrix (annualised %) — <c>[moneyness][tenor]</c>.</summary>
    [JsonPropertyName("implied_vol")]
    public double[][]? ImpliedVol { get; set; }
}

/// <summary>
/// One detected static-arbitrage violation on the surface.
///
/// <para><see cref="Type"/> is typically <c>"butterfly"</c> (negative density
/// at a strike) or <c>"calendar"</c> (total variance non-monotonic in
/// expiry).</para>
/// </summary>
public sealed class AdvVolatilityArbitrageFlag
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary><c>"butterfly"</c> | <c>"calendar"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Strike or log-moneyness where the violation was detected.</summary>
    [JsonPropertyName("strike_or_k")]
    public double? StrikeOrK { get; set; }

    /// <summary>Plain-English explanation of the violation. Safe to surface verbatim.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>Variance swap fair values for one expiry.</summary>
public sealed class AdvVolatilityVarianceSwap
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("days_to_expiry")]
    public int? DaysToExpiry { get; set; }

    /// <summary>Fair variance strike (squared annualised vol).</summary>
    [JsonPropertyName("fair_variance")]
    public double? FairVariance { get; set; }

    /// <summary>Fair vol strike — <c>sqrt(fair_variance)</c>, annualised %.</summary>
    [JsonPropertyName("fair_vol")]
    public double? FairVol { get; set; }

    /// <summary>ATM implied vol at this expiry (annualised %).</summary>
    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    /// <summary><c>fair_vol - atm_iv</c>. Premium for the curvature of the smile.</summary>
    [JsonPropertyName("convexity_adjustment")]
    public double? ConvexityAdjustment { get; set; }
}

/// <summary>Second- and third-order greek surfaces over the strike × expiry grid.</summary>
public sealed class AdvVolatilityGreeksSurfaces
{
    /// <summary>∂²V/∂S∂σ — sensitivity of delta to vol changes.</summary>
    [JsonPropertyName("vanna")]
    public AdvVolatilityGreekGrid? Vanna { get; set; }

    /// <summary>∂²V/∂S∂t — sensitivity of delta to time decay.</summary>
    [JsonPropertyName("charm")]
    public AdvVolatilityGreekGrid? Charm { get; set; }

    /// <summary>∂²V/∂σ² — sensitivity of vega to vol changes.</summary>
    [JsonPropertyName("volga")]
    public AdvVolatilityGreekGrid? Volga { get; set; }

    /// <summary>∂³V/∂S³ — third-order spot sensitivity.</summary>
    [JsonPropertyName("speed")]
    public AdvVolatilityGreekGrid? Speed { get; set; }
}

/// <summary>
/// One greek surface on a strike × expiry grid.
///
/// <para><see cref="Values"/> is indexed as <c>[strike_idx][expiry_idx]</c> —
/// match the outer dimension to <see cref="Strikes"/> and the inner to
/// <see cref="Expiries"/>.</para>
/// </summary>
public sealed class AdvVolatilityGreekGrid
{
    /// <summary>Strike grid axis.</summary>
    [JsonPropertyName("strikes")]
    public double[]? Strikes { get; set; }

    /// <summary>Expiry date strings.</summary>
    [JsonPropertyName("expiries")]
    public string[]? Expiries { get; set; }

    /// <summary>Greek values — <c>[strike][expiry]</c>.</summary>
    [JsonPropertyName("values")]
    public double[][]? Values { get; set; }
}
