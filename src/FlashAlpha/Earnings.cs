using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/earnings/calendar (MCP get_earnings_calendar) ────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/calendar</c> (MCP <c>get_earnings_calendar</c>).
///
/// <para>Upcoming earnings calendar over a forward window, optionally filtered to
/// specific symbols and a minimum importance rating. Requires Growth+.</para>
/// </summary>
public sealed class EarningsCalendarResponse
{
    [JsonPropertyName("events")]
    public List<EarningsCalendarEvent>? Events { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

/// <summary>One event inside <see cref="EarningsCalendarResponse.Events"/>.</summary>
public sealed class EarningsCalendarEvent
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    /// <summary><c>bmo</c> (before market open) / <c>amc</c> (after market close); may be null.</summary>
    [JsonPropertyName("timing")]
    public string? Timing { get; set; }

    [JsonPropertyName("is_confirmed")]
    public bool? IsConfirmed { get; set; }

    [JsonPropertyName("fiscal_period")]
    public string? FiscalPeriod { get; set; }

    [JsonPropertyName("fiscal_year")]
    public int? FiscalYear { get; set; }

    [JsonPropertyName("importance")]
    public int? Importance { get; set; }

    [JsonPropertyName("eps_estimate")]
    public double? EpsEstimate { get; set; }

    [JsonPropertyName("implied_move_pct")]
    public double? ImpliedMovePct { get; set; }

    [JsonPropertyName("days_to_event")]
    public int? DaysToEvent { get; set; }
}

// ── /v1/earnings/expected-move/{symbol} ──────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/expected-move/{symbol}</c>.
///
/// <para>Live earnings-implied move decomposition for the next event, splitting
/// the front-expiry straddle into the earnings-jump component vs. baseline
/// diffusion drift. Requires Growth+.</para>
/// </summary>
public sealed class EarningsExpectedMoveResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    [JsonPropertyName("session")]
    public string? Session { get; set; }

    [JsonPropertyName("days_to_event")]
    public int? DaysToEvent { get; set; }

    /// <summary><c>null</c> when the pre/post-event expiry IVs cannot be resolved.</summary>
    [JsonPropertyName("expected_move")]
    public EarningsExpectedMoveBlock? ExpectedMove { get; set; }
}

/// <summary>The <c>expected_move</c> block of <see cref="EarningsExpectedMoveResponse"/>.</summary>
public sealed class EarningsExpectedMoveBlock
{
    [JsonPropertyName("raw_straddle_pct")]
    public double? RawStraddlePct { get; set; }

    [JsonPropertyName("earnings_implied_pct")]
    public double? EarningsImpliedPct { get; set; }

    [JsonPropertyName("baseline_drift_pct")]
    public double? BaselineDriftPct { get; set; }

    [JsonPropertyName("earnings_iv")]
    public double? EarningsIv { get; set; }

    [JsonPropertyName("term_iv_post_event")]
    public double? TermIvPostEvent { get; set; }

    [JsonPropertyName("term_kink_pct")]
    public double? TermKinkPct { get; set; }
}

// ── /v1/earnings/history/{symbol} ────────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/history/{symbol}</c>.
///
/// <para>Past earnings events with EPS/revenue actuals and surprises, implied
/// vs. actual moves, and realized IV crush. Requires Growth+.</para>
/// </summary>
public sealed class EarningsHistoryResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("history")]
    public List<EarningsHistoryEvent>? History { get; set; }
}

/// <summary>One reported event inside <see cref="EarningsHistoryResponse.History"/>.</summary>
public sealed class EarningsHistoryEvent
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("fiscal_period")]
    public string? FiscalPeriod { get; set; }

    [JsonPropertyName("fiscal_year")]
    public int? FiscalYear { get; set; }

    [JsonPropertyName("eps_estimate")]
    public double? EpsEstimate { get; set; }

    [JsonPropertyName("eps_actual")]
    public double? EpsActual { get; set; }

    [JsonPropertyName("eps_surprise_pct")]
    public double? EpsSurprisePct { get; set; }

    [JsonPropertyName("revenue_actual")]
    public double? RevenueActual { get; set; }

    [JsonPropertyName("revenue_surprise_pct")]
    public double? RevenueSurprisePct { get; set; }

    [JsonPropertyName("implied_move_pct")]
    public double? ImpliedMovePct { get; set; }

    [JsonPropertyName("actual_move_pct")]
    public double? ActualMovePct { get; set; }

    [JsonPropertyName("iv_crush_pct")]
    public double? IvCrushPct { get; set; }

    [JsonPropertyName("pre_atm_iv")]
    public double? PreAtmIv { get; set; }

    [JsonPropertyName("post_atm_iv")]
    public double? PostAtmIv { get; set; }
}

// ── /v1/earnings/iv-crush/{symbol} ───────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/iv-crush/{symbol}</c>.
///
/// <para>Expected IV crush for the next event plus the symbol's historical
/// IV-crush distribution. Requires Growth+.</para>
/// </summary>
public sealed class EarningsIvCrushResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Next event date; <c>null</c> when no upcoming event but history exists.</summary>
    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    /// <summary>Live crush estimate; <c>null</c> when no upcoming event or term structure unresolved.</summary>
    [JsonPropertyName("current_estimate")]
    public EarningsIvCrushEstimate? CurrentEstimate { get; set; }

    [JsonPropertyName("distribution")]
    public EarningsIvCrushDistribution? Distribution { get; set; }
}

/// <summary>The <c>current_estimate</c> block of <see cref="EarningsIvCrushResponse"/>.</summary>
public sealed class EarningsIvCrushEstimate
{
    [JsonPropertyName("expected_crush_pct")]
    public double? ExpectedCrushPct { get; set; }

    [JsonPropertyName("pre_iv")]
    public double? PreIv { get; set; }

    [JsonPropertyName("post_iv")]
    public double? PostIv { get; set; }
}

/// <summary>The historical <c>distribution</c> block of <see cref="EarningsIvCrushResponse"/>.</summary>
public sealed class EarningsIvCrushDistribution
{
    [JsonPropertyName("median")]
    public double? Median { get; set; }

    [JsonPropertyName("p25")]
    public double? P25 { get; set; }

    [JsonPropertyName("p75")]
    public double? P75 { get; set; }

    [JsonPropertyName("worst")]
    public double? Worst { get; set; }

    [JsonPropertyName("best")]
    public double? Best { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

// ── /v1/earnings/vrp/{symbol} ────────────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/vrp/{symbol}</c>.
///
/// <para>Earnings volatility-risk-premium: the live event-implied move vs. the
/// symbol's realized history of actual moves, with a richness assessment and
/// surprise-reaction breakdown. Requires Alpha+.</para>
/// </summary>
public sealed class EarningsVrpResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    [JsonPropertyName("days_to_event")]
    public int? DaysToEvent { get; set; }

    [JsonPropertyName("earnings_vrp")]
    public EarningsVrpBlock? EarningsVrp { get; set; }

    [JsonPropertyName("surprise_reaction")]
    public EarningsSurpriseReaction? SurpriseReaction { get; set; }
}

/// <summary>The <c>earnings_vrp</c> block of <see cref="EarningsVrpResponse"/>.</summary>
public sealed class EarningsVrpBlock
{
    [JsonPropertyName("implied_move_pct")]
    public double? ImpliedMovePct { get; set; }

    [JsonPropertyName("realized_median")]
    public double? RealizedMedian { get; set; }

    [JsonPropertyName("realized_mean")]
    public double? RealizedMean { get; set; }

    /// <summary><c>implied_move / realized_median</c>. &gt;1 means options are pricing more than history realized.</summary>
    [JsonPropertyName("premium_ratio")]
    public double? PremiumRatio { get; set; }

    /// <summary><c>null</c> when fewer than 5 historical moves.</summary>
    [JsonPropertyName("z_score")]
    public double? ZScore { get; set; }

    [JsonPropertyName("percentile")]
    public double? Percentile { get; set; }

    /// <summary><c>rich</c> / <c>slightly_rich</c> / <c>fair</c> / <c>slightly_cheap</c> / <c>cheap</c> / <c>insufficient_data</c>.</summary>
    [JsonPropertyName("assessment")]
    public string? Assessment { get; set; }

    /// <summary><c>downside_overpriced</c> / <c>upside_overpriced</c>; otherwise null.</summary>
    [JsonPropertyName("directional_bias")]
    public string? DirectionalBias { get; set; }
}

/// <summary>The <c>surprise_reaction</c> block of <see cref="EarningsVrpResponse"/>.</summary>
public sealed class EarningsSurpriseReaction
{
    [JsonPropertyName("beat_avg_move_pct")]
    public double? BeatAvgMovePct { get; set; }

    [JsonPropertyName("miss_avg_move_pct")]
    public double? MissAvgMovePct { get; set; }

    [JsonPropertyName("inline_avg_move_pct")]
    public double? InlineAvgMovePct { get; set; }
}

// ── /v1/earnings/dealer-positioning/{symbol} ─────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/dealer-positioning/{symbol}</c>.
///
/// <para>Dealer exposure analysis scoped to the earnings event: gamma flip /
/// call &amp; put walls on event-week expiries, net GEX bucketed by
/// pre-event / event-week / post-event, charm acceleration into the event, and
/// the top strikes by absolute net GEX. Requires Alpha+.</para>
/// </summary>
public sealed class EarningsDealerPositioningResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    /// <summary>Closest options expiry on or after the earnings date; <c>null</c> if none found.</summary>
    [JsonPropertyName("event_expiry")]
    public string? EventExpiry { get; set; }

    [JsonPropertyName("levels")]
    public EarningsDealerLevels? Levels { get; set; }

    [JsonPropertyName("gex_by_dte_bucket")]
    public List<EarningsGexBucket>? GexByDteBucket { get; set; }

    /// <summary>Up to 5 strikes ranked by absolute net GEX.</summary>
    [JsonPropertyName("top_strikes")]
    public List<EarningsTopStrike>? TopStrikes { get; set; }

    /// <summary>Ratio of event-expiry CHEX to full-chain CHEX; <c>null</c> when not computable.</summary>
    [JsonPropertyName("charm_acceleration")]
    public double? CharmAcceleration { get; set; }

    /// <summary><c>positive_gamma</c> / <c>negative_gamma</c> / <c>undetermined</c>.</summary>
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }
}

/// <summary>The <c>levels</c> block of <see cref="EarningsDealerPositioningResponse"/>.</summary>
public sealed class EarningsDealerLevels
{
    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    [JsonPropertyName("call_wall")]
    public double? CallWall { get; set; }

    [JsonPropertyName("put_wall")]
    public double? PutWall { get; set; }

    [JsonPropertyName("highest_oi_strike")]
    public double? HighestOiStrike { get; set; }
}

/// <summary>One DTE bucket inside <see cref="EarningsDealerPositioningResponse.GexByDteBucket"/>.</summary>
public sealed class EarningsGexBucket
{
    /// <summary><c>pre_event</c> / <c>event_week</c> / <c>post_event</c>.</summary>
    [JsonPropertyName("bucket")]
    public string? Bucket { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("contract_count")]
    public int? ContractCount { get; set; }
}

/// <summary>One strike inside <see cref="EarningsDealerPositioningResponse.TopStrikes"/>.</summary>
public sealed class EarningsTopStrike
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("call_oi")]
    public long? CallOi { get; set; }

    [JsonPropertyName("put_oi")]
    public long? PutOi { get; set; }
}

// ── /v1/earnings/strategies/{symbol} ─────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/strategies/{symbol}</c>.
///
/// <para>Strategy-suitability scores (0–100) for the upcoming event across common
/// earnings structures, blending implied move, VRP premium ratio, expected IV
/// crush, ATM liquidity, and the gamma regime. Requires Alpha+.</para>
/// </summary>
public sealed class EarningsStrategiesResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    [JsonPropertyName("scores")]
    public EarningsStrategyScores? Scores { get; set; }

    [JsonPropertyName("context")]
    public EarningsStrategyContext? Context { get; set; }
}

/// <summary>The <c>scores</c> block of <see cref="EarningsStrategiesResponse"/>.</summary>
public sealed class EarningsStrategyScores
{
    [JsonPropertyName("long_straddle")]
    public int? LongStraddle { get; set; }

    [JsonPropertyName("short_strangle")]
    public int? ShortStrangle { get; set; }

    [JsonPropertyName("iron_condor")]
    public int? IronCondor { get; set; }

    [JsonPropertyName("calendar_spread")]
    public int? CalendarSpread { get; set; }

    [JsonPropertyName("earnings_diagonal")]
    public int? EarningsDiagonal { get; set; }
}

/// <summary>The <c>context</c> block of <see cref="EarningsStrategiesResponse"/>.</summary>
public sealed class EarningsStrategyContext
{
    [JsonPropertyName("premium_ratio")]
    public double? PremiumRatio { get; set; }

    [JsonPropertyName("iv_crush_median")]
    public double? IvCrushMedian { get; set; }

    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    [JsonPropertyName("implied_move_pct")]
    public double? ImpliedMovePct { get; set; }
}

// ── /v1/earnings/screener (MCP get_earnings_screener) ────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/earnings/screener</c> (MCP <c>get_earnings_screener</c>).
///
/// <para>Cross-sectional screener over upcoming earnings in a forward window,
/// ranked by VRP richness, cheapest implied move, highest historical crush, or
/// importance. Requires Alpha+.</para>
/// </summary>
public sealed class EarningsScreenerResponse
{
    [JsonPropertyName("events")]
    public List<EarningsScreenerEvent>? Events { get; set; }

    /// <summary>Total matched events before <c>limit</c> is applied.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

/// <summary>One event inside <see cref="EarningsScreenerResponse.Events"/>.</summary>
public sealed class EarningsScreenerEvent
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("company_name")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("earnings_date")]
    public string? EarningsDate { get; set; }

    [JsonPropertyName("days_to_event")]
    public int? DaysToEvent { get; set; }

    [JsonPropertyName("timing")]
    public string? Timing { get; set; }

    [JsonPropertyName("importance")]
    public int? Importance { get; set; }

    [JsonPropertyName("implied_move_pct")]
    public double? ImpliedMovePct { get; set; }

    [JsonPropertyName("premium_ratio")]
    public double? PremiumRatio { get; set; }

    [JsonPropertyName("iv_crush_median")]
    public double? IvCrushMedian { get; set; }

    [JsonPropertyName("assessment")]
    public string? Assessment { get; set; }
}
