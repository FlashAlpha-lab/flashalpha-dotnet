using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/liquidity/{symbol} (MCP get_liquidity) ───────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/liquidity/{symbol}</c> (MCP <c>get_liquidity</c>).
///
/// <para>Per-expiry execution score (0-100), ATM bid-ask spread %, OI-weighted
/// spread %, ATM OI depth, plus chain-level OI-weighted score and best/worst
/// expiry. Requires Growth+.</para>
/// </summary>
public sealed class LiquidityResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>OI-weighted average of per-expiry execution scores (0-100).</summary>
    [JsonPropertyName("chain_execution_score")]
    public int? ChainExecutionScore { get; set; }

    [JsonPropertyName("best_expiry")]
    public string? BestExpiry { get; set; }

    [JsonPropertyName("worst_expiry")]
    public string? WorstExpiry { get; set; }

    /// <summary>Number of expiries labelled <c>illiquid</c>.</summary>
    [JsonPropertyName("thin_expiry_count")]
    public int? ThinExpiryCount { get; set; }

    [JsonPropertyName("expiries")]
    public List<LiquidityExpiry>? Expiries { get; set; }
}

/// <summary>One per-expiry liquidity row inside <see cref="LiquidityResponse.Expiries"/>.</summary>
public sealed class LiquidityExpiry
{
    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("dte")]
    public int? Dte { get; set; }

    /// <summary>Average bid-ask spread % at the strike closest to spot. <c>null</c> when neither side quotes.</summary>
    [JsonPropertyName("atm_spread_pct")]
    public double? AtmSpreadPct { get; set; }

    /// <summary>OI-weighted bid-ask spread %. <c>null</c> when no contract qualifies.</summary>
    [JsonPropertyName("weighted_spread_pct")]
    public double? WeightedSpreadPct { get; set; }

    [JsonPropertyName("atm_oi")]
    public long? AtmOi { get; set; }

    [JsonPropertyName("execution_score")]
    public int? ExecutionScore { get; set; }

    /// <summary><c>tight</c> / <c>normal</c> / <c>wide</c> / <c>illiquid</c>.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}

// ── /v1/volatility/skew-term/{symbol} (MCP get_skew_term) ────────────────────

/// <summary>
/// Typed response for <c>GET /v1/volatility/skew-term/{symbol}</c>
/// (MCP <c>get_skew_term</c>).
///
/// <para>Skew term structure with vol-desk naming conventions. For each expiry:
/// ATM IV, 25Δ and 10Δ wing IVs, and the named conventions —
/// <c>skew_25d</c>, <c>risk_reversal_25d</c>, <c>butterfly_25d</c> — plus
/// <c>tail_convexity</c>. Requires Growth+.</para>
/// </summary>
public sealed class SkewTermResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("expiries")]
    public List<SkewTermExpiry>? Expiries { get; set; }
}

/// <summary>One per-expiry skew row inside <see cref="SkewTermResponse.Expiries"/>.</summary>
public sealed class SkewTermExpiry
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("dte")]
    public int? Dte { get; set; }

    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    [JsonPropertyName("put_25d_iv")]
    public double? Put25dIv { get; set; }

    [JsonPropertyName("call_25d_iv")]
    public double? Call25dIv { get; set; }

    [JsonPropertyName("put_10d_iv")]
    public double? Put10dIv { get; set; }

    [JsonPropertyName("call_10d_iv")]
    public double? Call10dIv { get; set; }

    /// <summary><c>put_25d_iv − call_25d_iv</c>. Positive ⇒ put skew dominant.</summary>
    [JsonPropertyName("skew_25d")]
    public double? Skew25d { get; set; }

    /// <summary><c>call_25d_iv − put_25d_iv</c> (= <c>−skew_25d</c>).</summary>
    [JsonPropertyName("risk_reversal_25d")]
    public double? RiskReversal25d { get; set; }

    /// <summary>Wing premium over ATM.</summary>
    [JsonPropertyName("butterfly_25d")]
    public double? Butterfly25d { get; set; }

    /// <summary>Second difference of the put wing. Positive ⇒ steep tail.</summary>
    [JsonPropertyName("tail_convexity")]
    public double? TailConvexity { get; set; }
}

// ── /v1/volatility/spot-vol-correlation/{symbol} (MCP get_spot_vol_correlation)

/// <summary>
/// Typed response for <c>GET /v1/volatility/spot-vol-correlation/{symbol}</c>
/// (MCP <c>get_spot_vol_correlation</c>).
///
/// <para>Daily Pearson correlation between spot log-returns and first-differences
/// of ATM implied vol over 20-day and 60-day windows. Requires Growth+.</para>
/// </summary>
public sealed class SpotVolCorrelationResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Pearson over the last 20 snapshots. <c>null</c> when undefined.</summary>
    [JsonPropertyName("spot_vol_correlation_20d")]
    public double? SpotVolCorrelation20d { get; set; }

    /// <summary>Pearson over the last 60 snapshots. <c>null</c> when history is too short.</summary>
    [JsonPropertyName("spot_vol_correlation_60d")]
    public double? SpotVolCorrelation60d { get; set; }

    [JsonPropertyName("data_points_20d")]
    public int? DataPoints20d { get; set; }

    [JsonPropertyName("data_points_60d")]
    public int? DataPoints60d { get; set; }

    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }
}

// ── /v1/dispersion (MCP get_dispersion) ──────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/dispersion</c> (MCP <c>get_dispersion</c>).
///
/// <para>Demeterfi-Derman-Kani implied correlation between an index and a basket
/// of constituents, paired with a 1-factor realized correlation. Returns
/// <c>correlation_premium = implied − realized</c> and per-constituent
/// contribution to basket vol. Requires Alpha+.</para>
/// </summary>
public sealed class DispersionResponse
{
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("index")]
    public string? Index { get; set; }

    [JsonPropertyName("constituent_count")]
    public int? ConstituentCount { get; set; }

    [JsonPropertyName("missing_symbols")]
    public List<string>? MissingSymbols { get; set; }

    [JsonPropertyName("horizon_days")]
    public int? HorizonDays { get; set; }

    /// <summary>Implied correlation. <c>null</c> for a degenerate basket.</summary>
    [JsonPropertyName("implied_correlation")]
    public double? ImpliedCorrelation { get; set; }

    /// <summary>1-factor realized correlation. <c>null</c> when not computable.</summary>
    [JsonPropertyName("realized_correlation")]
    public double? RealizedCorrelation { get; set; }

    /// <summary><c>implied − realized</c>. Positive ⇒ market pricing more correlation than realized.</summary>
    [JsonPropertyName("correlation_premium")]
    public double? CorrelationPremium { get; set; }

    /// <summary>Index ATM IV (decimal).</summary>
    [JsonPropertyName("implied_vol_index")]
    public double? ImpliedVolIndex { get; set; }

    /// <summary><c>Σ wᵢ σᵢ</c> after renormalisation.</summary>
    [JsonPropertyName("implied_vol_basket")]
    public double? ImpliedVolBasket { get; set; }

    /// <summary>Sorted descending by <c>contribution_to_basket_vol</c>.</summary>
    [JsonPropertyName("top_contributors")]
    public List<DispersionContributor>? TopContributors { get; set; }
}

/// <summary>One contributor inside <see cref="DispersionResponse.TopContributors"/>.</summary>
public sealed class DispersionContributor
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    [JsonPropertyName("iv")]
    public double? Iv { get; set; }

    /// <summary><c>wᵢ × σᵢ</c>.</summary>
    [JsonPropertyName("contribution_to_basket_vol")]
    public double? ContributionToBasketVol { get; set; }
}

// ── /v1/expected-move/{symbol} (MCP get_expected_move) ───────────────────────

/// <summary>
/// Typed response for <c>GET /v1/expected-move/{symbol}</c> (MCP <c>get_expected_move</c>).
///
/// <para>Straddle-implied expected move per expiry, derived from ATM implied
/// volatility. Note: the top-level keys are snake_case but the items inside
/// <see cref="ExpectedMoves"/> use camelCase. Requires Basic+.</para>
/// </summary>
public sealed class ExpectedMoveResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Ordered by expiry.</summary>
    [JsonPropertyName("expected_moves")]
    public List<ExpectedMoveItem>? ExpectedMoves { get; set; }
}

/// <summary>One per-expiry expected move inside <see cref="ExpectedMoveResponse.ExpectedMoves"/>.</summary>
public sealed class ExpectedMoveItem
{
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("daysToExpiry")]
    public int? DaysToExpiry { get; set; }

    /// <summary>ATM IV as a decimal, or <c>null</c> when no ATM IV can be derived.</summary>
    [JsonPropertyName("atmIv")]
    public double? AtmIv { get; set; }

    /// <summary>1-σ move in price terms.</summary>
    [JsonPropertyName("expectedMove")]
    public double? ExpectedMove { get; set; }

    /// <summary>1-σ move as a percentage of spot.</summary>
    [JsonPropertyName("expectedMovePct")]
    public double? ExpectedMovePct { get; set; }

    [JsonPropertyName("lowerBound")]
    public double? LowerBound { get; set; }

    [JsonPropertyName("upperBound")]
    public double? UpperBound { get; set; }
}

// ── /v1/vrp/{symbol}/history (MCP get_vrp_history) ───────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/vrp/{symbol}/history</c> (MCP <c>get_vrp_history</c>).
///
/// <para>Daily VRP time series for charting and backtesting. Requires Alpha+.</para>
/// </summary>
public sealed class VrpHistoryResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("days")]
    public int? Days { get; set; }

    [JsonPropertyName("data_points")]
    public int? DataPoints { get; set; }

    [JsonPropertyName("history")]
    public List<VrpHistoryPoint>? History { get; set; }
}

/// <summary>One daily VRP snapshot inside <see cref="VrpHistoryResponse.History"/>.</summary>
public sealed class VrpHistoryPoint
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>Close price.</summary>
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    [JsonPropertyName("rv_5d")]
    public double? Rv5d { get; set; }

    [JsonPropertyName("rv_10d")]
    public double? Rv10d { get; set; }

    [JsonPropertyName("rv_20d")]
    public double? Rv20d { get; set; }

    [JsonPropertyName("rv_30d")]
    public double? Rv30d { get; set; }

    [JsonPropertyName("vrp_20d")]
    public double? Vrp20d { get; set; }

    [JsonPropertyName("straddle")]
    public double? Straddle { get; set; }

    [JsonPropertyName("expected_move_1d")]
    public double? ExpectedMove1d { get; set; }
}
