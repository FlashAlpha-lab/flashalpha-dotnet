using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/flow/zero-dte/snapshot/{symbol} (MCP get_zero_dte_flow) ──────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/zero-dte/snapshot/{symbol}</c>
/// (MCP <c>get_zero_dte_flow</c>).
///
/// <para>Live, intraday-aware view of today's 0DTE dealer-positioning landscape.
/// Computed on effective OI (settled + the simulator's intraday delta), so it
/// reflects how dealer GEX has shifted since open. Returns the same JSON shape
/// as <see cref="ZeroDteResponse"/> PLUS a <see cref="FlowDirection"/> block.
/// Requires Growth+.</para>
///
/// <para>Degraded shapes (all 200 OK): when there's no 0DTE expiry today
/// (<see cref="ZeroDteResponse.NoZeroDte"/> is <c>true</c>), or when the session
/// is closed (<see cref="SessionClosed"/> is <c>true</c>, with
/// <see cref="LastSession"/> populated and analytics fields <c>null</c>).</para>
/// </summary>
public sealed class ZeroDteFlowSnapshotResponse : ZeroDteResponse
{
    /// <summary>Flow-direction read: how the intraday tape has shifted dealer GEX since open.</summary>
    [JsonPropertyName("flow_direction")]
    public ZeroDteFlowDirection? FlowDirection { get; set; }

    /// <summary><c>true</c> when the market is closed (weekend / holiday / outside RTH).</summary>
    [JsonPropertyName("session_closed")]
    public bool? SessionClosed { get; set; }

    /// <summary>The last trading session date, populated alongside <see cref="SessionClosed"/>.</summary>
    [JsonPropertyName("last_session")]
    public string? LastSession { get; set; }
}

/// <summary>The <c>flow_direction</c> block of <see cref="ZeroDteFlowSnapshotResponse"/>.</summary>
public sealed class ZeroDteFlowDirection
{
    /// <summary><c>no_flow</c> / <c>neutral</c> / <c>amplifying</c> / <c>dampening</c> / <c>regime_flip</c>.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("settled_net_gex")]
    public double? SettledNetGex { get; set; }

    [JsonPropertyName("live_net_gex")]
    public double? LiveNetGex { get; set; }

    [JsonPropertyName("flow_gex_adjustment")]
    public double? FlowGexAdjustment { get; set; }

    /// <summary><c>null</c> when settled GEX is 0.</summary>
    [JsonPropertyName("flow_gex_pct_shift")]
    public double? FlowGexPctShift { get; set; }

    [JsonPropertyName("contracts_with_flow")]
    public int? ContractsWithFlow { get; set; }

    [JsonPropertyName("total_abs_delta_contracts")]
    public long? TotalAbsDeltaContracts { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

// ── /v1/flow/zero-dte/series/{symbol} ────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/zero-dte/series/{symbol}</c>.
///
/// <para>Intraday time series of today's 0DTE flow — one bar per sampled
/// interval — for charting headline metrics and cumulative dealer hedge-flow
/// over the session. Empty <see cref="Bars"/> when no samples in the window.
/// Requires Growth+.</para>
/// </summary>
public sealed class ZeroDteFlowSeriesResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>Today (ET); the 0DTE expiry being sampled.</summary>
    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Echoes the requested bar size (<c>30s</c> / <c>1m</c> / <c>5m</c> / <c>15m</c>).</summary>
    [JsonPropertyName("bar_size")]
    public string? BarSize { get; set; }

    [JsonPropertyName("bars")]
    public List<ZeroDteFlowSeriesBar>? Bars { get; set; }
}

/// <summary>One bar inside <see cref="ZeroDteFlowSeriesResponse.Bars"/>.</summary>
public sealed class ZeroDteFlowSeriesBar
{
    /// <summary>Bar timestamp (UTC, snapped to :00/:30 ET upstream).</summary>
    [JsonPropertyName("t")]
    public string? T { get; set; }

    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    [JsonPropertyName("call_wall")]
    public double? CallWall { get; set; }

    [JsonPropertyName("put_wall")]
    public double? PutWall { get; set; }

    [JsonPropertyName("magnet")]
    public double? Magnet { get; set; }

    [JsonPropertyName("pin_score")]
    public int? PinScore { get; set; }

    [JsonPropertyName("pin_probability_pct")]
    public double? PinProbabilityPct { get; set; }

    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    [JsonPropertyName("charm_dollars_per_hour")]
    public double? CharmDollarsPerHour { get; set; }

    /// <summary>$-units, cumulative since session open.</summary>
    [JsonPropertyName("hedge_flow_call_cumulative")]
    public double? HedgeFlowCallCumulative { get; set; }

    [JsonPropertyName("hedge_flow_put_cumulative")]
    public double? HedgeFlowPutCumulative { get; set; }

    /// <summary>Call + put cumulative.</summary>
    [JsonPropertyName("hedge_flow_cumulative_all")]
    public double? HedgeFlowCumulativeAll { get; set; }
}

// ── /v1/flow/zero-dte/hedge-flow/{symbol} ────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/zero-dte/hedge-flow/{symbol}</c>.
///
/// <para>Estimated dealer hedge-flow time series for today's 0DTE chain — signed
/// delta-dollars dealers are inferred to be buying/selling to stay hedged — as
/// per-bar increments and a running cumulative since session open, projectable
/// to <c>all</c> / <c>calls</c> / <c>puts</c> via <c>side</c>. Requires Growth+.</para>
/// </summary>
public sealed class ZeroDteFlowHedgeFlowResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Echoes the requested side (<c>all</c> / <c>calls</c> / <c>puts</c>).</summary>
    [JsonPropertyName("side")]
    public string? Side { get; set; }

    [JsonPropertyName("bar_size")]
    public string? BarSize { get; set; }

    [JsonPropertyName("bars")]
    public List<ZeroDteFlowHedgeFlowBar>? Bars { get; set; }
}

/// <summary>One bar inside <see cref="ZeroDteFlowHedgeFlowResponse.Bars"/>.</summary>
public sealed class ZeroDteFlowHedgeFlowBar
{
    /// <summary>Bar timestamp (UTC).</summary>
    [JsonPropertyName("t")]
    public string? T { get; set; }

    /// <summary>Per-bar signed delta-dollars in this bucket.</summary>
    [JsonPropertyName("bar")]
    public double? Bar { get; set; }

    /// <summary>Running sum since session open (for the requested side).</summary>
    [JsonPropertyName("cumulative")]
    public double? Cumulative { get; set; }
}

// ── /v1/flow/zero-dte/heatmap/{symbol} ───────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/zero-dte/heatmap/{symbol}</c>.
///
/// <para>Per-strike value matrix over today's 0DTE session — the data behind a
/// strike × time heatmap. <see cref="StrikesGrid"/> holds the strikes; each
/// bar's <see cref="ZeroDteFlowHeatmapBar.Values"/> array is parallel-by-index
/// to it (column-major). Requires Alpha+.</para>
/// </summary>
public sealed class ZeroDteFlowHeatmapResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    /// <summary>Echoes the requested metric (<c>gex</c>/<c>dex</c>/<c>vex</c>/<c>chex</c>/<c>oi</c>/<c>signed_flow</c>).</summary>
    [JsonPropertyName("metric")]
    public string? Metric { get; set; }

    /// <summary>Echoes the requested mode (<c>raw</c>/<c>delta</c>).</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("bar_size")]
    public string? BarSize { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("tier_used")]
    public string? TierUsed { get; set; }

    /// <summary>Strike axis; index-aligned to each bar's <see cref="ZeroDteFlowHeatmapBar.Values"/>.</summary>
    [JsonPropertyName("strikes_grid")]
    public double[]? StrikesGrid { get; set; }

    [JsonPropertyName("bars")]
    public List<ZeroDteFlowHeatmapBar>? Bars { get; set; }

    /// <summary>Reserved for sampler-gap intervals; not yet populated.</summary>
    [JsonPropertyName("gap_intervals")]
    public List<JsonElementBox>? GapIntervals { get; set; }
}

/// <summary>One bar inside <see cref="ZeroDteFlowHeatmapResponse.Bars"/>.</summary>
public sealed class ZeroDteFlowHeatmapBar
{
    [JsonPropertyName("t")]
    public string? T { get; set; }

    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    /// <summary>The metric value per strike — <c>values[i]</c> for <c>strikes_grid[i]</c>.</summary>
    [JsonPropertyName("values")]
    public double?[]? Values { get; set; }
}

// ── /v1/flow/zero-dte/strike-flow/{symbol} ───────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/zero-dte/strike-flow/{symbol}</c>.
///
/// <para>Per-strike signed aggressor flow over today's 0DTE session — per bar
/// and strike, the signed delta-dollars, signed gamma-dollars, and contract
/// count (per-bar increments). Three parallel arrays per bar, each index-aligned
/// to <see cref="StrikesGrid"/>. Requires Alpha+.</para>
/// </summary>
public sealed class ZeroDteFlowStrikeFlowResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("bar_size")]
    public string? BarSize { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("tier_used")]
    public string? TierUsed { get; set; }

    [JsonPropertyName("strikes_grid")]
    public double[]? StrikesGrid { get; set; }

    [JsonPropertyName("bars")]
    public List<ZeroDteFlowStrikeFlowBar>? Bars { get; set; }

    /// <summary>Reserved; not yet populated.</summary>
    [JsonPropertyName("gap_intervals")]
    public List<JsonElementBox>? GapIntervals { get; set; }
}

/// <summary>One bar inside <see cref="ZeroDteFlowStrikeFlowResponse.Bars"/>.</summary>
public sealed class ZeroDteFlowStrikeFlowBar
{
    [JsonPropertyName("t")]
    public string? T { get; set; }

    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    /// <summary>Per strike, index-aligned to <c>strikes_grid</c>.</summary>
    [JsonPropertyName("signed_delta_dollars")]
    public double?[]? SignedDeltaDollars { get; set; }

    [JsonPropertyName("signed_gamma_dollars")]
    public double?[]? SignedGammaDollars { get; set; }

    [JsonPropertyName("contracts")]
    public long?[]? Contracts { get; set; }
}

// ── /v1/flow/zero-dte/leaderboard ────────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/zero-dte/leaderboard</c>.
///
/// <para>Cross-symbol 0DTE leaderboard: ranks the pre-warmed universe by a single
/// metric (<c>heat</c> / <c>pin_risk</c> / <c>abs_flow</c> / <c>charm_intensity</c>)
/// and returns the top <see cref="N"/> entries, highest-first. Requires the Alpha
/// plan.</para>
/// </summary>
public sealed class ZeroDteFlowLeaderboardResponse
{
    /// <summary>Echoes the requested metric (<c>heat</c>/<c>pin_risk</c>/<c>abs_flow</c>/<c>charm_intensity</c>).</summary>
    [JsonPropertyName("metric")]
    public string? Metric { get; set; }

    /// <summary>Echoes the requested entry count (clamped to [1, 100]).</summary>
    [JsonPropertyName("n")]
    public int? N { get; set; }

    [JsonPropertyName("as_of")]
    public DateTime? AsOf { get; set; }

    [JsonPropertyName("market_open")]
    public bool? MarketOpen { get; set; }

    [JsonPropertyName("entries")]
    public List<ZeroDteFlowLeaderboardEntry>? Entries { get; set; }
}

/// <summary>One ranked symbol inside <see cref="ZeroDteFlowLeaderboardResponse.Entries"/>.</summary>
public sealed class ZeroDteFlowLeaderboardEntry
{
    /// <summary>1-based rank (highest metric value is rank 1).</summary>
    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>The metric value this entry was ranked by.</summary>
    [JsonPropertyName("value")]
    public double? Value { get; set; }
}

/// <summary>Opaque placeholder element used for reserved/forward-compatible arrays.</summary>
public sealed class JsonElementBox
{
}
