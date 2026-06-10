using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/exposure/sheet/{symbol} (MCP get_exposure_sheet) ─────────────────────

/// <summary>
/// Typed response for <c>GET /v1/exposure/sheet/{symbol}</c> (MCP <c>get_exposure_sheet</c>).
///
/// <para>Unified per-strike rowset joining GEX, DEX, VEX, CHEX, and DAG
/// (delta-adjusted gamma) in one response, plus chain totals, the
/// Line-in-the-Sand inflection strike, all gamma peaks (not just the single
/// wall), and OPEX / triple-witching flags when an expiration filter is
/// supplied. Requires Growth+.</para>
/// </summary>
public sealed class ExposureSheetResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    /// <summary>True when <c>expiration</c> is the holiday-adjusted monthly OPEX.</summary>
    [JsonPropertyName("is_opex")]
    public bool? IsOpex { get; set; }

    /// <summary>True when <see cref="IsOpex"/> AND month is Mar/Jun/Sep/Dec.</summary>
    [JsonPropertyName("is_triple_witching")]
    public bool? IsTripleWitching { get; set; }

    /// <summary>Chain totals for net GEX/DEX/VEX/CHEX/DAG.</summary>
    [JsonPropertyName("totals")]
    public ExposureSheetTotals? Totals { get; set; }

    /// <summary>Line-in-the-Sand inflection strike. <c>null</c> when undefinable.</summary>
    [JsonPropertyName("lis")]
    public ExposureSheetLis? Lis { get; set; }

    /// <summary>Local maxima of <c>|net_gex|</c> (all gamma peaks, not just the wall).</summary>
    [JsonPropertyName("peaks")]
    public List<ExposureSheetPeak>? Peaks { get; set; }

    [JsonPropertyName("strikes")]
    public List<ExposureSheetStrike>? Strikes { get; set; }
}

/// <summary>Chain-level totals block of <see cref="ExposureSheetResponse"/>.</summary>
public sealed class ExposureSheetTotals
{
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    /// <summary>Σ over strikes of DAG (delta-adjusted gamma).</summary>
    [JsonPropertyName("net_dag")]
    public double? NetDag { get; set; }
}

/// <summary>Line-in-the-Sand strike block of <see cref="ExposureSheetResponse"/>.</summary>
public sealed class ExposureSheetLis
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary>Currently always <c>1.0</c> (reserved for future relative-magnitude scoring).</summary>
    [JsonPropertyName("magnitude")]
    public double? Magnitude { get; set; }
}

/// <summary>One gamma peak inside <see cref="ExposureSheetResponse.Peaks"/>.</summary>
public sealed class ExposureSheetPeak
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    /// <summary>Fraction of max <c>|net_gex|</c> in the chain (0–1).</summary>
    [JsonPropertyName("strength")]
    public double? Strength { get; set; }

    /// <summary><c>call_wall</c> when <c>net_gex ≥ 0</c>, otherwise <c>put_wall</c>.</summary>
    [JsonPropertyName("side")]
    public string? Side { get; set; }
}

/// <summary>One per-strike row of <see cref="ExposureSheetResponse.Strikes"/>.</summary>
public sealed class ExposureSheetStrike
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("call_gex")]
    public double? CallGex { get; set; }

    [JsonPropertyName("put_gex")]
    public double? PutGex { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("call_dex")]
    public double? CallDex { get; set; }

    [JsonPropertyName("put_dex")]
    public double? PutDex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("call_vex")]
    public double? CallVex { get; set; }

    [JsonPropertyName("put_vex")]
    public double? PutVex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("call_chex")]
    public double? CallChex { get; set; }

    [JsonPropertyName("put_chex")]
    public double? PutChex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    /// <summary>Per-strike DAG aggregated across calls + puts.</summary>
    [JsonPropertyName("dag")]
    public double? Dag { get; set; }

    [JsonPropertyName("call_oi")]
    public long? CallOi { get; set; }

    [JsonPropertyName("put_oi")]
    public long? PutOi { get; set; }
}

// ── /v1/exposure/term-structure/{symbol} (MCP get_term_structure) ────────────

/// <summary>
/// Typed response for <c>GET /v1/exposure/term-structure/{symbol}</c>
/// (MCP <c>get_term_structure</c>).
///
/// <para>Per-greek exposure aggregated by DTE bucket and also rolled up per
/// expiry. Buckets: 0-7d / 8-30d / 31-60d / 61-180d / 180d+. Requires Growth+.</para>
/// </summary>
public sealed class ExposureTermStructureResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("by_dte_bucket")]
    public List<ExposureTermBucket>? ByDteBucket { get; set; }

    [JsonPropertyName("by_expiry")]
    public List<ExposureTermExpiry>? ByExpiry { get; set; }
}

/// <summary>One DTE bucket inside <see cref="ExposureTermStructureResponse.ByDteBucket"/>.</summary>
public sealed class ExposureTermBucket
{
    [JsonPropertyName("bucket")]
    public string? Bucket { get; set; }

    /// <summary>Inclusive <c>[lower, upper]</c> DTE bounds (final bucket carries int.MaxValue).</summary>
    [JsonPropertyName("dte_range")]
    public int[]? DteRange { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    [JsonPropertyName("contract_count")]
    public int? ContractCount { get; set; }
}

/// <summary>One per-expiry row inside <see cref="ExposureTermStructureResponse.ByExpiry"/>.</summary>
public sealed class ExposureTermExpiry
{
    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("dte")]
    public int? Dte { get; set; }

    [JsonPropertyName("is_opex")]
    public bool? IsOpex { get; set; }

    [JsonPropertyName("is_triple_witching")]
    public bool? IsTripleWitching { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    /// <summary>This expiry's <c>|net_gex|</c> as a share of all expiries (0–100).</summary>
    [JsonPropertyName("pct_of_chain_gex")]
    public double? PctOfChainGex { get; set; }
}

// ── /v1/exposure/basket (MCP get_exposure_basket) ────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/exposure/basket</c> (MCP <c>get_exposure_basket</c>).
///
/// <para>Weighted cross-symbol aggregate of GEX / DEX / VEX / CHEX across up to
/// 50 user-supplied symbols. Requires Growth+.</para>
/// </summary>
public sealed class ExposureBasketResponse
{
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Number of symbols that actually contributed (after drops).</summary>
    [JsonPropertyName("constituent_count")]
    public int? ConstituentCount { get; set; }

    /// <summary>Symbols requested but with no data.</summary>
    [JsonPropertyName("missing_symbols")]
    public List<string>? MissingSymbols { get; set; }

    /// <summary>Weighted aggregate exposures after weight renormalisation.</summary>
    [JsonPropertyName("aggregate")]
    public ExposureBasketAggregate? Aggregate { get; set; }

    [JsonPropertyName("constituents")]
    public List<ExposureBasketConstituent>? Constituents { get; set; }
}

/// <summary>Weighted aggregate block of <see cref="ExposureBasketResponse"/>.</summary>
public sealed class ExposureBasketAggregate
{
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }
}

/// <summary>One basket constituent inside <see cref="ExposureBasketResponse.Constituents"/>.</summary>
public sealed class ExposureBasketConstituent
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>Renormalised weight applied to this symbol (sums to 1 across the response).</summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    /// <summary>Weighted GEX contribution to the aggregate, 0–100.</summary>
    [JsonPropertyName("contribution_pct")]
    public double? ContributionPct { get; set; }

    /// <summary><c>positive_gamma</c> when <c>net_gex ≥ 0</c>, else <c>negative_gamma</c>.</summary>
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }
}

// ── /v1/exposure/oi-diff/{symbol} (MCP get_oi_diff) ──────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/exposure/oi-diff/{symbol}</c> (MCP <c>get_oi_diff</c>).
///
/// <para>Day-over-day open-interest deltas. Per-contract deltas, top-N changes
/// sorted by absolute magnitude, and call/put aggregate totals. Requires Growth+.</para>
/// </summary>
public sealed class ExposureOiDiffResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary><c>false</c> when no prior-day OI data has been written yet.</summary>
    [JsonPropertyName("prior_snapshot_available")]
    public bool? PriorSnapshotAvailable { get; set; }

    [JsonPropertyName("total_call_oi_change")]
    public long? TotalCallOiChange { get; set; }

    [JsonPropertyName("total_put_oi_change")]
    public long? TotalPutOiChange { get; set; }

    /// <summary>Sorted by <c>|oi_change|</c> descending.</summary>
    [JsonPropertyName("top_oi_changes")]
    public List<OiChange>? TopOiChanges { get; set; }
}

/// <summary>One contract's OI delta inside <see cref="ExposureOiDiffResponse.TopOiChanges"/>.</summary>
public sealed class OiChange
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary><c>C</c> or <c>P</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("today_oi")]
    public long? TodayOi { get; set; }

    [JsonPropertyName("prior_oi")]
    public long? PriorOi { get; set; }

    [JsonPropertyName("oi_change")]
    public long? OiChange_ { get; set; }
}
