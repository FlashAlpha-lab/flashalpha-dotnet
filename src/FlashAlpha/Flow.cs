using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// Typed response models for the live (simulation-aware) flow surface under
// /v1/flow/* (Alpha+). Two families:
//
//   * Analytics (/v1/flow/{levels,pin-risk,summary,oi,gex,dex,dealer-risk,
//     live}/{symbol}) — fold today's intraday trade tape onto the settled
//     book, so gamma flip / walls / GEX reflect today's flow. snake_case
//     wire shape; optional ?expiry=YYYY-MM-DD slices to one expiration cycle.
//
//   * Raw flow data (/v1/flow/options/*, /v1/flow/stocks/*) — the underlying
//     trade tape: prints, blocks, per-minute history, cumulative net-flow
//     series, cross-symbol leaderboards / outliers. Proxied verbatim so the
//     wire keys are camelCase and timestamps are ISO-8601 UTC strings.
//
// Flow gex/dex per-strike rows are the same wire shape as
// /v1/exposure/gex|dex, so they reuse GexStrikeRow / DexStrikeRow.

/// <summary>Typed response for <c>GET /v1/flow/levels/{symbol}</c> (Alpha+).
/// Gamma flip / call &amp; put walls / max pain recomputed against the live
/// (intraday-flow-adjusted) book. Each level is <c>null</c> when it can't be
/// located (e.g. no sign change in net gamma).</summary>
public sealed class FlowLevelsResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at <see cref="AsOf"/>.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back (<c>YYYY-MM-DD</c>), or null for the whole chain.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Spot where live net dealer gamma crosses zero. Null if no flip.</summary>
    [JsonPropertyName("live_gamma_flip")] public double? LiveGammaFlip { get; set; }
    /// <summary>Strike of the largest live call-gamma concentration (upside magnet).</summary>
    [JsonPropertyName("live_call_wall")] public double? LiveCallWall { get; set; }
    /// <summary>Strike of the largest live put-gamma concentration (downside magnet).</summary>
    [JsonPropertyName("live_put_wall")] public double? LivePutWall { get; set; }
    /// <summary>Live max-pain strike (most option value expires worthless).</summary>
    [JsonPropertyName("live_max_pain")] public double? LiveMaxPain { get; set; }
}

/// <summary>Component scores (0–100) behind the <c>live_pin_risk</c> headline.</summary>
public sealed class FlowPinRiskBreakdown
{
    /// <summary>Open-interest concentration around the magnet strike.</summary>
    [JsonPropertyName("oi_score")] public int? OiScore { get; set; }
    /// <summary>How close spot is to the magnet strike.</summary>
    [JsonPropertyName("proximity_score")] public int? ProximityScore { get; set; }
    /// <summary>Time-to-close weighting (pin pressure rises into the cash close).</summary>
    [JsonPropertyName("time_score")] public int? TimeScore { get; set; }
    /// <summary>Dealer-gamma intensity at the magnet strike.</summary>
    [JsonPropertyName("gamma_score")] public int? GammaScore { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/pin-risk/{symbol}</c> (Alpha+).
/// A 0–100 composite pin-risk score plus the magnet strike and breakdown.</summary>
public sealed class FlowPinRiskResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Composite 0–100 pin-risk score (higher = stronger pin pull).</summary>
    [JsonPropertyName("live_pin_risk")] public int? LivePinRisk { get; set; }
    /// <summary>Pin magnet strike (argmax|net gamma|); null when no dominant strike.</summary>
    [JsonPropertyName("magnet_strike")] public double? MagnetStrike { get; set; }
    /// <summary>Signed % distance from spot to the magnet strike.</summary>
    [JsonPropertyName("distance_to_magnet_pct")] public double? DistanceToMagnetPct { get; set; }
    /// <summary>Hours remaining until the regular-session cash close.</summary>
    [JsonPropertyName("time_to_close_hours")] public double? TimeToCloseHours { get; set; }
    /// <summary>The four component scores behind <see cref="LivePinRisk"/>.</summary>
    [JsonPropertyName("breakdown")] public FlowPinRiskBreakdown Breakdown { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/summary/{symbol}</c> (Alpha+).
/// At-a-glance read on whether today's tape has shifted the dealer book.</summary>
public sealed class FlowSummaryResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Net classified direction of intraday flow (e.g. "bullish", "bearish", "neutral").</summary>
    [JsonPropertyName("flow_direction")] public string FlowDirection { get; set; } = "";
    /// <summary>Net change in simulated open interest since the open (contracts).</summary>
    [JsonPropertyName("intraday_oi_delta")] public long? IntradayOiDelta { get; set; }
    /// <summary>Contracts that have printed at least one trade today.</summary>
    [JsonPropertyName("contracts_with_flow")] public int? ContractsWithFlow { get; set; }
    /// <summary>Total contracts tracked for the underlying.</summary>
    [JsonPropertyName("contracts_total")] public int? ContractsTotal { get; set; }
    /// <summary>Live (flow-adjusted) net GEX (dollars per 1% spot move).</summary>
    [JsonPropertyName("live_gex")] public double? LiveGex { get; set; }
    /// <summary>% shift in net GEX caused by today's flow vs the settled book;
    /// null when the settled baseline is zero.</summary>
    [JsonPropertyName("flow_gex_pct_shift")] public double? FlowGexPctShift { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/oi/{symbol}</c> (Alpha+).
/// Settled (official) OI vs the intraday simulated OI. This endpoint does
/// NOT return <c>underlying_price</c>.</summary>
public sealed class FlowOiResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Official exchange OI from the settled snapshot (sum across the chain).</summary>
    [JsonPropertyName("official_oi")] public long? OfficialOi { get; set; }
    /// <summary>Intraday simulated OI (official + estimated open/close from the tape).</summary>
    [JsonPropertyName("simulated_oi")] public long? SimulatedOi { get; set; }
    /// <summary><see cref="SimulatedOi"/> − <see cref="OfficialOi"/> (signed).</summary>
    [JsonPropertyName("intraday_oi_delta")] public long? IntradayOiDelta { get; set; }
    /// <summary>Confidence 0–1 in the intraday OI estimate (trade-tape coverage).</summary>
    [JsonPropertyName("oi_delta_confidence")] public double? OiDeltaConfidence { get; set; }
    /// <summary>OI actually used by the live analytics (blended).</summary>
    [JsonPropertyName("effective_oi")] public long? EffectiveOi { get; set; }
    /// <summary>Total contracts tracked for the underlying.</summary>
    [JsonPropertyName("contracts_total")] public int? ContractsTotal { get; set; }
    /// <summary>Contracts that printed at least one trade today.</summary>
    [JsonPropertyName("contracts_with_flow")] public int? ContractsWithFlow { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/gex/{symbol}</c> (Alpha+).
/// Live (flow-adjusted) GEX with the same per-strike shape as
/// <see cref="GexResponse"/> (reuses <see cref="GexStrikeRow"/>).</summary>
public sealed class FlowGexResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Live net GEX across the chain (dollars per 1% spot move).</summary>
    [JsonPropertyName("live_net_gex")] public double? LiveNetGex { get; set; }
    /// <summary>Categorical regime label (e.g. "positive", "negative"). Safe to surface verbatim.</summary>
    [JsonPropertyName("live_net_gex_label")] public string LiveNetGexLabel { get; set; } = "";
    /// <summary>Live gamma-flip spot, or null if no sign change.</summary>
    [JsonPropertyName("live_gamma_flip")] public double? LiveGammaFlip { get; set; }
    /// <summary>Per-strike breakdown (identical schema to settled GEX).</summary>
    [JsonPropertyName("strikes")] public List<GexStrikeRow> Strikes { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/dex/{symbol}</c> (Alpha+).
/// Live (flow-adjusted) DEX with the same per-strike shape as
/// <see cref="DexResponse"/> (reuses <see cref="DexStrikeRow"/>).</summary>
public sealed class FlowDexResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Live net DEX across the chain (dollars).</summary>
    [JsonPropertyName("live_net_dex")] public double? LiveNetDex { get; set; }
    /// <summary>Per-strike DEX breakdown.</summary>
    [JsonPropertyName("strikes")] public List<DexStrikeRow> Strikes { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/dealer-risk/{symbol}</c> (Alpha+).
/// Side-by-side of the settled snapshot and the live flow-adjusted book,
/// with the dollar adjustment and % shift today's tape produced.</summary>
public sealed class FlowDealerRiskResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Net GEX from the settled (prior close) snapshot.</summary>
    [JsonPropertyName("settled_net_gex")] public double? SettledNetGex { get; set; }
    /// <summary>Net GEX from the live flow-adjusted book.</summary>
    [JsonPropertyName("live_net_gex")] public double? LiveNetGex { get; set; }
    /// <summary><see cref="LiveNetGex"/> − <see cref="SettledNetGex"/> (dollars).</summary>
    [JsonPropertyName("flow_gex_adjustment")] public double? FlowGexAdjustment { get; set; }
    /// <summary>% GEX shift from flow; null when the settled baseline is zero.</summary>
    [JsonPropertyName("flow_gex_pct_shift")] public double? FlowGexPctShift { get; set; }
    /// <summary>Net DEX from the settled snapshot.</summary>
    [JsonPropertyName("settled_net_dex")] public double? SettledNetDex { get; set; }
    /// <summary>Net DEX from the live flow-adjusted book.</summary>
    [JsonPropertyName("live_net_dex")] public double? LiveNetDex { get; set; }
    /// <summary><see cref="LiveNetDex"/> − <see cref="SettledNetDex"/> (dollars).</summary>
    [JsonPropertyName("flow_dex_adjustment")] public double? FlowDexAdjustment { get; set; }
    /// <summary>% DEX shift from flow; null when the settled baseline is zero.</summary>
    [JsonPropertyName("flow_dex_pct_shift")] public double? FlowDexPctShift { get; set; }
    /// <summary>Absolute delta-weighted contracts traded today (flow magnitude).</summary>
    [JsonPropertyName("total_abs_delta_contracts")] public long? TotalAbsDeltaContracts { get; set; }
    /// <summary>Contracts that printed at least one trade today.</summary>
    [JsonPropertyName("contracts_with_flow")] public int? ContractsWithFlow { get; set; }
    /// <summary>Net classified flow direction.</summary>
    [JsonPropertyName("flow_direction")] public string FlowDirection { get; set; } = "";
    /// <summary>Plain-English summary of whether flow has moved the dealer
    /// book. Safe to surface verbatim.</summary>
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

/// <summary>Nested dealer-risk block inside <see cref="FlowLiveResponse"/>.
/// Identical to <see cref="FlowDealerRiskResponse"/> minus
/// <c>contracts_with_flow</c> (carried on the parent live envelope).</summary>
public sealed class FlowAdjustedDealerRisk
{
    /// <summary>Net GEX from the settled snapshot.</summary>
    [JsonPropertyName("settled_net_gex")] public double? SettledNetGex { get; set; }
    /// <summary>Net GEX from the live flow-adjusted book.</summary>
    [JsonPropertyName("live_net_gex")] public double? LiveNetGex { get; set; }
    /// <summary><see cref="LiveNetGex"/> − <see cref="SettledNetGex"/> (dollars).</summary>
    [JsonPropertyName("flow_gex_adjustment")] public double? FlowGexAdjustment { get; set; }
    /// <summary>% GEX shift from flow; null when the settled baseline is zero.</summary>
    [JsonPropertyName("flow_gex_pct_shift")] public double? FlowGexPctShift { get; set; }
    /// <summary>Net DEX from the settled snapshot.</summary>
    [JsonPropertyName("settled_net_dex")] public double? SettledNetDex { get; set; }
    /// <summary>Net DEX from the live flow-adjusted book.</summary>
    [JsonPropertyName("live_net_dex")] public double? LiveNetDex { get; set; }
    /// <summary><see cref="LiveNetDex"/> − <see cref="SettledNetDex"/> (dollars).</summary>
    [JsonPropertyName("flow_dex_adjustment")] public double? FlowDexAdjustment { get; set; }
    /// <summary>% DEX shift from flow; null when the settled baseline is zero.</summary>
    [JsonPropertyName("flow_dex_pct_shift")] public double? FlowDexPctShift { get; set; }
    /// <summary>Absolute delta-weighted contracts traded today (flow magnitude).</summary>
    [JsonPropertyName("total_abs_delta_contracts")] public long? TotalAbsDeltaContracts { get; set; }
    /// <summary>Net classified flow direction.</summary>
    [JsonPropertyName("flow_direction")] public string FlowDirection { get; set; } = "";
    /// <summary>Plain-English summary. Safe to surface verbatim.</summary>
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

/// <summary>Typed response for <c>GET /v1/flow/live/{symbol}</c> (Alpha+).
/// Everything-at-once convenience bundle: OI simulator state + live exposure
/// + live levels + pin risk + the nested dealer-risk block.</summary>
public sealed class FlowLiveResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Total contracts tracked for the underlying.</summary>
    [JsonPropertyName("contracts")] public int? Contracts { get; set; }
    /// <summary>Contracts that printed at least one trade today.</summary>
    [JsonPropertyName("contracts_with_flow")] public int? ContractsWithFlow { get; set; }
    /// <summary>Official exchange OI from the settled snapshot.</summary>
    [JsonPropertyName("official_oi")] public long? OfficialOi { get; set; }
    /// <summary>Intraday simulated OI.</summary>
    [JsonPropertyName("simulated_oi")] public long? SimulatedOi { get; set; }
    /// <summary><see cref="SimulatedOi"/> − <see cref="OfficialOi"/> (signed).</summary>
    [JsonPropertyName("intraday_oi_delta")] public long? IntradayOiDelta { get; set; }
    /// <summary>Confidence 0–1 in the intraday OI estimate.</summary>
    [JsonPropertyName("oi_delta_confidence")] public double? OiDeltaConfidence { get; set; }
    /// <summary>OI actually used by the live analytics (blended).</summary>
    [JsonPropertyName("effective_oi")] public long? EffectiveOi { get; set; }
    /// <summary>Live net GEX (dollars per 1% spot move).</summary>
    [JsonPropertyName("live_gex")] public double? LiveGex { get; set; }
    /// <summary>Live net DEX (dollars). Named <c>live_gex_delta</c> on the wire.</summary>
    [JsonPropertyName("live_gex_delta")] public double? LiveGexDelta { get; set; }
    /// <summary>Live gamma-flip spot, or null.</summary>
    [JsonPropertyName("live_gamma_flip")] public double? LiveGammaFlip { get; set; }
    /// <summary>Live call wall strike, or null.</summary>
    [JsonPropertyName("live_call_wall")] public double? LiveCallWall { get; set; }
    /// <summary>Live put wall strike, or null.</summary>
    [JsonPropertyName("live_put_wall")] public double? LivePutWall { get; set; }
    /// <summary>Live max-pain strike, or null.</summary>
    [JsonPropertyName("live_max_pain")] public double? LiveMaxPain { get; set; }
    /// <summary>Composite 0–100 pin-risk score.</summary>
    [JsonPropertyName("live_pin_risk")] public int? LivePinRisk { get; set; }
    /// <summary>Nested settled-vs-live dealer-risk block.</summary>
    [JsonPropertyName("flow_adjusted_dealer_risk")] public FlowAdjustedDealerRisk FlowAdjustedDealerRisk { get; set; } = new();
}

// ── Raw flow data (camelCase wire keys) ─────────────────────────────────────

/// <summary>A single option trade print (a <c>trades[]</c> element).</summary>
public sealed class FlowOptionTrade
{
    /// <summary>Trade timestamp (ISO-8601 UTC).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>OPRA instrument id of the contract.</summary>
    [JsonPropertyName("instrumentId")] public long? InstrumentId { get; set; }
    /// <summary>Contract expiration (<c>YYYY-MM-DD</c>).</summary>
    [JsonPropertyName("expiry")] public string Expiry { get; set; } = "";
    /// <summary>Contract strike price.</summary>
    [JsonPropertyName("strike")] public double? Strike { get; set; }
    /// <summary><c>"C"</c> (call) or <c>"P"</c> (put).</summary>
    [JsonPropertyName("right")] public string Right { get; set; } = "";
    /// <summary>Trade price.</summary>
    [JsonPropertyName("price")] public double? Price { get; set; }
    /// <summary>Trade size in contracts.</summary>
    [JsonPropertyName("size")] public int? Size { get; set; }
    /// <summary>Side classification vs the NBBO at print ("buy"/"sell"/"mid").</summary>
    [JsonPropertyName("side")] public string Side { get; set; } = "";
    /// <summary>True when the print is at/above the block-size threshold.</summary>
    [JsonPropertyName("isBlock")] public bool? IsBlock { get; set; }
    /// <summary>NBBO bid at the moment of the trade.</summary>
    [JsonPropertyName("bid")] public double? Bid { get; set; }
    /// <summary>NBBO ask at the moment of the trade.</summary>
    [JsonPropertyName("ask")] public double? Ask { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/options/{symbol}/recent</c>
/// (Alpha+). Newest-first option trade tape. <c>expiry</c> is echoed only
/// when the filter is supplied.</summary>
public sealed class FlowOptionRecentResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Expiration filter echoed back when supplied, else null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Number of trades returned (capped by the limit).</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Unclamped total trade count.</summary>
    [JsonPropertyName("totalAvailable")] public int? TotalAvailable { get; set; }
    /// <summary>Newest-first list of trade prints.</summary>
    [JsonPropertyName("trades")] public List<FlowOptionTrade> Trades { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/options/{symbol}/summary</c>
/// (Alpha+). Per-underlying option-flow aggregates.</summary>
public sealed class FlowOptionSummaryResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Expiration filter echoed back when supplied, else null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Distinct contracts that printed at least one trade.</summary>
    [JsonPropertyName("contractsWithTrades")] public int? ContractsWithTrades { get; set; }
    /// <summary>Total number of trade prints.</summary>
    [JsonPropertyName("totalTrades")] public int? TotalTrades { get; set; }
    /// <summary>Buy-classified contract volume.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified contract volume.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Volume classified at the mid (uninformed).</summary>
    [JsonPropertyName("midVolume")] public long? MidVolume { get; set; }
    /// <summary><see cref="BuyVolume"/> − <see cref="SellVolume"/>.</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Largest single trade size.</summary>
    [JsonPropertyName("biggestSingleTrade")] public int? BiggestSingleTrade { get; set; }
    /// <summary>Timestamp of the most recent print; null when no trades.</summary>
    [JsonPropertyName("lastTradeUtc")] public string? LastTradeUtc { get; set; }
}

/// <summary>A single large option print (a <c>blocks[]</c> element).</summary>
public sealed class FlowOptionBlock
{
    /// <summary>Trade timestamp (ISO-8601 UTC).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Contract expiration (<c>YYYY-MM-DD</c>).</summary>
    [JsonPropertyName("expiry")] public string Expiry { get; set; } = "";
    /// <summary>Contract strike price.</summary>
    [JsonPropertyName("strike")] public double? Strike { get; set; }
    /// <summary><c>"C"</c> (call) or <c>"P"</c> (put).</summary>
    [JsonPropertyName("right")] public string Right { get; set; } = "";
    /// <summary>Trade price.</summary>
    [JsonPropertyName("price")] public double? Price { get; set; }
    /// <summary>Trade size in contracts.</summary>
    [JsonPropertyName("size")] public int? Size { get; set; }
    /// <summary>Side classification ("buy"/"sell"/"mid").</summary>
    [JsonPropertyName("side")] public string Side { get; set; } = "";
}

/// <summary>Typed response for <c>GET /v1/flow/options/{symbol}/blocks</c>
/// (Alpha+). All trades with <c>size &gt;= minSize</c>, newest-first.</summary>
public sealed class FlowOptionBlocksResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Expiration filter echoed back when supplied, else null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Minimum trade size that qualified as a block (echoed back).</summary>
    [JsonPropertyName("minSize")] public int? MinSize { get; set; }
    /// <summary>Number of blocks returned.</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Newest-first list of large prints.</summary>
    [JsonPropertyName("blocks")] public List<FlowOptionBlock> Blocks { get; set; } = new();
}

/// <summary>One per-minute option-flow bucket (a <c>buckets[]</c> element).</summary>
public sealed class FlowHistoryBucket
{
    /// <summary>Bucket start (ISO-8601 UTC, minute-aligned).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Buy-classified volume in the bucket.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified volume in the bucket.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Mid-classified volume in the bucket.</summary>
    [JsonPropertyName("midVolume")] public long? MidVolume { get; set; }
    /// <summary><see cref="BuyVolume"/> − <see cref="SellVolume"/>.</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Number of trades in the bucket.</summary>
    [JsonPropertyName("tradeCount")] public int? TradeCount { get; set; }
    /// <summary>Largest single trade size in the bucket.</summary>
    [JsonPropertyName("biggestTrade")] public int? BiggestTrade { get; set; }
    /// <summary>Volume-weighted average trade price across the bucket.</summary>
    [JsonPropertyName("vwap")] public double? Vwap { get; set; }
    /// <summary>Highest trade price in the bucket.</summary>
    [JsonPropertyName("high")] public double? High { get; set; }
    /// <summary>Lowest trade price in the bucket.</summary>
    [JsonPropertyName("low")] public double? Low { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/options/{symbol}/history</c>
/// (Alpha+). Newest-first per-minute buckets.</summary>
public sealed class FlowOptionHistoryResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Expiration filter echoed back when supplied, else null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Lookback window in minutes (echoed back).</summary>
    [JsonPropertyName("minutes")] public int? Minutes { get; set; }
    /// <summary>Number of buckets returned.</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Newest-first list of per-minute aggregates.</summary>
    [JsonPropertyName("buckets")] public List<FlowHistoryBucket> Buckets { get; set; } = new();
}

/// <summary>One point of a cumulative net-flow series (a <c>points[]</c>
/// element). Shared by the option and stock cumulative endpoints.</summary>
public sealed class FlowCumulativePoint
{
    /// <summary>Bucket start (ISO-8601 UTC, minute-aligned).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Net volume in this minute bucket.</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Running sum of <see cref="NetVolume"/> from the start of the
    /// window (the "HIRO-style" cumulative line).</summary>
    [JsonPropertyName("cumulative")] public long? Cumulative { get; set; }
    /// <summary>Volume-weighted average price in the bucket.</summary>
    [JsonPropertyName("vwap")] public double? Vwap { get; set; }
    /// <summary>Number of trades in the bucket.</summary>
    [JsonPropertyName("tradeCount")] public int? TradeCount { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/options/{symbol}/cumulative</c>
/// (Alpha+).</summary>
public sealed class FlowOptionCumulativeResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Expiration filter echoed back when supplied, else null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Lookback window in minutes (echoed back).</summary>
    [JsonPropertyName("minutes")] public int? Minutes { get; set; }
    /// <summary>Number of points returned.</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Chronological cumulative net-flow series.</summary>
    [JsonPropertyName("points")] public List<FlowCumulativePoint> Points { get; set; } = new();
}

/// <summary>A single stock trade print (a <c>trades[]</c> element).</summary>
public sealed class FlowStockTrade
{
    /// <summary>Trade timestamp (ISO-8601 UTC).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Trade price.</summary>
    [JsonPropertyName("price")] public double? Price { get; set; }
    /// <summary>Trade size in shares.</summary>
    [JsonPropertyName("size")] public int? Size { get; set; }
    /// <summary>Side classification ("buy"/"sell"/"mid").</summary>
    [JsonPropertyName("side")] public string Side { get; set; } = "";
    /// <summary>True when the print is at/above the block-size threshold.</summary>
    [JsonPropertyName("isBlock")] public bool? IsBlock { get; set; }
    /// <summary>NBBO bid at the moment of the trade.</summary>
    [JsonPropertyName("bid")] public double? Bid { get; set; }
    /// <summary>NBBO ask at the moment of the trade.</summary>
    [JsonPropertyName("ask")] public double? Ask { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/{symbol}/recent</c>
/// (Alpha+). Newest-first stock trade tape.</summary>
public sealed class FlowStockRecentResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Number of trades returned (capped by the limit).</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Unclamped total trade count.</summary>
    [JsonPropertyName("totalAvailable")] public int? TotalAvailable { get; set; }
    /// <summary>Newest-first list of trade prints.</summary>
    [JsonPropertyName("trades")] public List<FlowStockTrade> Trades { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/{symbol}/summary</c>
/// (Alpha+). Per-symbol stock-flow aggregates.</summary>
public sealed class FlowStockSummaryResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Total number of trade prints.</summary>
    [JsonPropertyName("totalTrades")] public int? TotalTrades { get; set; }
    /// <summary>Buy-classified share volume.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified share volume.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Volume classified at the mid (uninformed).</summary>
    [JsonPropertyName("midVolume")] public long? MidVolume { get; set; }
    /// <summary><see cref="BuyVolume"/> − <see cref="SellVolume"/>.</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Largest single trade size.</summary>
    [JsonPropertyName("biggestSingleTrade")] public int? BiggestSingleTrade { get; set; }
    /// <summary>Timestamp of the most recent print; null when no trades.</summary>
    [JsonPropertyName("lastTradeUtc")] public string? LastTradeUtc { get; set; }
}

/// <summary>A single large stock print (a <c>blocks[]</c> element).</summary>
public sealed class FlowStockBlock
{
    /// <summary>Trade timestamp (ISO-8601 UTC).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Trade price.</summary>
    [JsonPropertyName("price")] public double? Price { get; set; }
    /// <summary>Trade size in shares.</summary>
    [JsonPropertyName("size")] public int? Size { get; set; }
    /// <summary>Side classification ("buy"/"sell"/"mid").</summary>
    [JsonPropertyName("side")] public string Side { get; set; } = "";
    /// <summary>NBBO bid at the moment of the trade.</summary>
    [JsonPropertyName("bid")] public double? Bid { get; set; }
    /// <summary>NBBO ask at the moment of the trade.</summary>
    [JsonPropertyName("ask")] public double? Ask { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/{symbol}/blocks</c>
/// (Alpha+). All trades with <c>size &gt;= minSize</c>, newest-first.</summary>
public sealed class FlowStockBlocksResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Minimum trade size that qualified as a block (echoed back).</summary>
    [JsonPropertyName("minSize")] public int? MinSize { get; set; }
    /// <summary>Number of blocks returned.</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Newest-first list of large prints.</summary>
    [JsonPropertyName("blocks")] public List<FlowStockBlock> Blocks { get; set; } = new();
}

/// <summary>One per-minute stock-flow bucket. Like <see cref="FlowHistoryBucket"/>
/// but also carries OHLC of the print price.</summary>
public sealed class FlowStockHistoryBucket
{
    /// <summary>Bucket start (ISO-8601 UTC, minute-aligned).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Buy-classified volume in the bucket.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified volume in the bucket.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Mid-classified volume in the bucket.</summary>
    [JsonPropertyName("midVolume")] public long? MidVolume { get; set; }
    /// <summary><see cref="BuyVolume"/> − <see cref="SellVolume"/>.</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Number of trades in the bucket.</summary>
    [JsonPropertyName("tradeCount")] public int? TradeCount { get; set; }
    /// <summary>Largest single trade size in the bucket.</summary>
    [JsonPropertyName("biggestTrade")] public int? BiggestTrade { get; set; }
    /// <summary>Volume-weighted average trade price across the bucket.</summary>
    [JsonPropertyName("vwap")] public double? Vwap { get; set; }
    /// <summary>First trade price in the bucket.</summary>
    [JsonPropertyName("open")] public double? Open { get; set; }
    /// <summary>Last trade price in the bucket.</summary>
    [JsonPropertyName("close")] public double? Close { get; set; }
    /// <summary>Highest trade price in the bucket.</summary>
    [JsonPropertyName("high")] public double? High { get; set; }
    /// <summary>Lowest trade price in the bucket.</summary>
    [JsonPropertyName("low")] public double? Low { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/{symbol}/history</c>
/// (Alpha+). Newest-first per-minute buckets.</summary>
public sealed class FlowStockHistoryResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Lookback window in minutes (echoed back).</summary>
    [JsonPropertyName("minutes")] public int? Minutes { get; set; }
    /// <summary>Number of buckets returned.</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Newest-first list of per-minute aggregates.</summary>
    [JsonPropertyName("buckets")] public List<FlowStockHistoryBucket> Buckets { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/{symbol}/cumulative</c>
/// (Alpha+).</summary>
public sealed class FlowStockCumulativeResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Lookback window in minutes (echoed back).</summary>
    [JsonPropertyName("minutes")] public int? Minutes { get; set; }
    /// <summary>Number of points returned.</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Chronological cumulative net-flow series.</summary>
    [JsonPropertyName("points")] public List<FlowCumulativePoint> Points { get; set; } = new();
}

/// <summary>One ranked underlying in the option-flow leaderboard. Option rows
/// carry <see cref="AvgPremium"/>; the stock leaderboard uses <c>vwap</c>.</summary>
public sealed class FlowOptionLeaderRow
{
    /// <summary>Ranked underlying.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Net contracts (<c>buyVolume - sellVolume</c>).</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Net dollar option flow (≈ net contracts × avg premium × 100).</summary>
    [JsonPropertyName("netNotional")] public double? NetNotional { get; set; }
    /// <summary>Buy-classified contract volume.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified contract volume.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Volume-weighted average option premium over the window.</summary>
    [JsonPropertyName("avgPremium")] public double? AvgPremium { get; set; }
    /// <summary>Number of trades over the window.</summary>
    [JsonPropertyName("tradeCount")] public int? TradeCount { get; set; }
    /// <summary>Timestamp of the most recent print.</summary>
    [JsonPropertyName("lastTradeUtc")] public string LastTradeUtc { get; set; } = "";
}

/// <summary>Typed response for <c>GET /v1/flow/options/leaderboard</c>
/// (Alpha+). Top-N net-dollar buyers and sellers (cached ~30s).</summary>
public sealed class FlowOptionLeaderboardResponse
{
    /// <summary>When the cached snapshot was generated (ISO-8601 UTC).</summary>
    [JsonPropertyName("generatedUtc")] public string GeneratedUtc { get; set; } = "";
    /// <summary>Number of ranked rows requested per side.</summary>
    [JsonPropertyName("n")] public int? N { get; set; }
    /// <summary>Aggregation window in minutes.</summary>
    [JsonPropertyName("windowMinutes")] public int? WindowMinutes { get; set; }
    /// <summary>Top net-dollar buyers.</summary>
    [JsonPropertyName("buyers")] public List<FlowOptionLeaderRow> Buyers { get; set; } = new();
    /// <summary>Top net-dollar sellers.</summary>
    [JsonPropertyName("sellers")] public List<FlowOptionLeaderRow> Sellers { get; set; } = new();
}

/// <summary>One flagged underlying in an outliers table (shared by the
/// option and stock outliers endpoints).</summary>
public sealed class FlowOutlierRow
{
    /// <summary>Flagged underlying.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Number of trades over the window.</summary>
    [JsonPropertyName("tradeCount")] public int? TradeCount { get; set; }
    /// <summary>Buy-classified volume.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified volume.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Mid-classified volume.</summary>
    [JsonPropertyName("midVolume")] public long? MidVolume { get; set; }
    /// <summary><see cref="BuyVolume"/> − <see cref="SellVolume"/>.</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary><c>|buy-sell| / (buy+sell)</c> × 100: 0 = balanced, 100 = one-sided.</summary>
    [JsonPropertyName("imbalancePct")] public double? ImbalancePct { get; set; }
    /// <summary>Tiered skew label (FLAT/MILD_BUY/BUY/STRONG_BUY/…).</summary>
    [JsonPropertyName("skew")] public string Skew { get; set; } = "";
    /// <summary>Gross traded notional over the window (dollars).</summary>
    [JsonPropertyName("notional")] public double? Notional { get; set; }
    /// <summary>Net (signed) traded notional over the window (dollars).</summary>
    [JsonPropertyName("netNotional")] public double? NetNotional { get; set; }
    /// <summary>Largest single trade size.</summary>
    [JsonPropertyName("biggestTrade")] public int? BiggestTrade { get; set; }
    /// <summary>Timestamp of the biggest print; null if none in window.</summary>
    [JsonPropertyName("biggestTradeUtc")] public string? BiggestTradeUtc { get; set; }
    /// <summary>Age of the biggest print in seconds; -1 if none.</summary>
    [JsonPropertyName("biggestAgeSec")] public int? BiggestAgeSec { get; set; }
    /// <summary>VWAP of the most recent activity.</summary>
    [JsonPropertyName("lastVwap")] public double? LastVwap { get; set; }
    /// <summary>Timestamp of the last print; null if none.</summary>
    [JsonPropertyName("lastTradeUtc")] public string? LastTradeUtc { get; set; }
    /// <summary>Age of the last print in seconds; -1 if none.</summary>
    [JsonPropertyName("lastTradeAgeSec")] public int? LastTradeAgeSec { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/options/outliers</c>
/// (Alpha+, cached ~30s).</summary>
public sealed class FlowOptionOutliersResponse
{
    /// <summary>When the cached snapshot was generated (ISO-8601 UTC).</summary>
    [JsonPropertyName("generatedUtc")] public string GeneratedUtc { get; set; } = "";
    /// <summary>Aggregation window in minutes.</summary>
    [JsonPropertyName("windowMinutes")] public int? WindowMinutes { get; set; }
    /// <summary>Number of symbols evaluated.</summary>
    [JsonPropertyName("tracked")] public int? Tracked { get; set; }
    /// <summary>Symbols that met minTrades and had non-zero volume.</summary>
    [JsonPropertyName("qualified")] public int? Qualified { get; set; }
    /// <summary>Max rows requested.</summary>
    [JsonPropertyName("limit")] public int? Limit { get; set; }
    /// <summary>Imbalance-ranked flagged underlyings.</summary>
    [JsonPropertyName("outliers")] public List<FlowOutlierRow> Outliers { get; set; } = new();
}

/// <summary>One ranked symbol in the stock-flow leaderboard. Stock rows carry
/// <see cref="Vwap"/>; the option leaderboard uses <c>avgPremium</c>.</summary>
public sealed class FlowStockLeaderRow
{
    /// <summary>Ranked symbol.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Net shares (<c>buyVolume - sellVolume</c>).</summary>
    [JsonPropertyName("netVolume")] public long? NetVolume { get; set; }
    /// <summary>Net dollar flow (net shares × VWAP).</summary>
    [JsonPropertyName("netNotional")] public double? NetNotional { get; set; }
    /// <summary>Buy-classified share volume.</summary>
    [JsonPropertyName("buyVolume")] public long? BuyVolume { get; set; }
    /// <summary>Sell-classified share volume.</summary>
    [JsonPropertyName("sellVolume")] public long? SellVolume { get; set; }
    /// <summary>Volume-weighted average trade price over the window.</summary>
    [JsonPropertyName("vwap")] public double? Vwap { get; set; }
    /// <summary>Number of trades over the window.</summary>
    [JsonPropertyName("tradeCount")] public int? TradeCount { get; set; }
    /// <summary>Timestamp of the most recent print.</summary>
    [JsonPropertyName("lastTradeUtc")] public string LastTradeUtc { get; set; } = "";
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/leaderboard</c>
/// (Alpha+). Top-N net-dollar buyers and sellers (cached ~30s).</summary>
public sealed class FlowStockLeaderboardResponse
{
    /// <summary>When the cached snapshot was generated (ISO-8601 UTC).</summary>
    [JsonPropertyName("generatedUtc")] public string GeneratedUtc { get; set; } = "";
    /// <summary>Number of ranked rows requested per side.</summary>
    [JsonPropertyName("n")] public int? N { get; set; }
    /// <summary>Aggregation window in minutes.</summary>
    [JsonPropertyName("windowMinutes")] public int? WindowMinutes { get; set; }
    /// <summary>Top net-dollar buyers.</summary>
    [JsonPropertyName("buyers")] public List<FlowStockLeaderRow> Buyers { get; set; } = new();
    /// <summary>Top net-dollar sellers.</summary>
    [JsonPropertyName("sellers")] public List<FlowStockLeaderRow> Sellers { get; set; } = new();
}

/// <summary>Typed response for <c>GET /v1/flow/stocks/outliers</c>
/// (Alpha+, cached ~30s).</summary>
public sealed class FlowStockOutliersResponse
{
    /// <summary>When the cached snapshot was generated (ISO-8601 UTC).</summary>
    [JsonPropertyName("generatedUtc")] public string GeneratedUtc { get; set; } = "";
    /// <summary>Aggregation window in minutes.</summary>
    [JsonPropertyName("windowMinutes")] public int? WindowMinutes { get; set; }
    /// <summary>Number of symbols evaluated.</summary>
    [JsonPropertyName("tracked")] public int? Tracked { get; set; }
    /// <summary>Symbols that met minTrades and had non-zero volume.</summary>
    [JsonPropertyName("qualified")] public int? Qualified { get; set; }
    /// <summary>Max rows requested.</summary>
    [JsonPropertyName("limit")] public int? Limit { get; set; }
    /// <summary>Imbalance-ranked flagged symbols.</summary>
    [JsonPropertyName("outliers")] public List<FlowOutlierRow> Outliers { get; set; } = new();
}

// ── Flow signals (unusual-flow feed, Alpha+) ─────────────────────────
//
// Per-underlying scored/classified unusual-flow signals. Snake_case wire
// shape (analytics family). Both endpoints reuse FlowSignal.

/// <summary>Settled-chain reference levels echoed alongside the signals.
/// Computed once per request from the settled snapshot — independent of
/// the live flow surface. All fields are null when the chain snapshot is
/// unavailable.</summary>
public sealed class FlowSignalsChain
{
    /// <summary>Strike with the largest settled call GEX — upside dealer-defended level.</summary>
    [JsonPropertyName("call_wall")] public double? CallWall { get; set; }
    /// <summary>Strike with the largest settled put GEX — downside dealer-defended level.</summary>
    [JsonPropertyName("put_wall")] public double? PutWall { get; set; }
    /// <summary>Strike where total option-holder loss is maximized at expiry.</summary>
    [JsonPropertyName("max_pain")] public double? MaxPain { get; set; }
    /// <summary>Settled gamma-flip strike (sign change of net GEX across the chain).</summary>
    [JsonPropertyName("gamma_flip")] public double? GammaFlip { get; set; }
}

/// <summary>Component contributions that sum to the headline
/// <c>score</c>. Weights are server-tunable so absolute values may
/// shift, but the ordering of components is stable.</summary>
public sealed class FlowSignalScoreBreakdown
{
    /// <summary>Premium-size contribution (the larger the dollar premium, the more points).</summary>
    [JsonPropertyName("premium")] public int? Premium { get; set; }
    /// <summary>Print size relative to the contract's open interest.</summary>
    [JsonPropertyName("size_vs_oi")] public int? SizeVsOi { get; set; }
    /// <summary>NBBO aggressor strength — above-ask / at-ask earn more than mid.</summary>
    [JsonPropertyName("aggressor")] public int? Aggressor { get; set; }
    /// <summary>Sweep boost (≥2 same-side prints on one contract within ~500ms).</summary>
    [JsonPropertyName("sweep")] public int? Sweep { get; set; }
    /// <summary>OI-simulator opening-bias contribution.</summary>
    [JsonPropertyName("opening_bias")] public int? OpeningBias { get; set; }
    /// <summary>Tenor (DTE) contribution — short-dated prints score differently than long-dated.</summary>
    [JsonPropertyName("tenor")] public int? Tenor { get; set; }
}

/// <summary>Chain-derived context attached to a signal. All numeric
/// fields are null and <see cref="Moneyness"/> is <c>"unknown"</c> when
/// the contract isn't in the settled chain snapshot.</summary>
public sealed class FlowSignalEnrichment
{
    /// <summary>Contract implied vol (decimal, e.g. <c>0.62</c> = 62%).</summary>
    [JsonPropertyName("iv")] public double? Iv { get; set; }
    /// <summary>Contract delta (signed; positive for calls, negative for puts).</summary>
    [JsonPropertyName("delta")] public double? Delta { get; set; }
    /// <summary>Contract gamma (per-share).</summary>
    [JsonPropertyName("gamma")] public double? Gamma { get; set; }
    /// <summary>IV minus the nearest ATM IV (signed).</summary>
    [JsonPropertyName("iv_vs_atm")] public double? IvVsAtm { get; set; }
    /// <summary><c>"OTM"</c> / <c>"ATM"</c> / <c>"ITM"</c> / <c>"unknown"</c>.</summary>
    [JsonPropertyName("moneyness")] public string Moneyness { get; set; } = "";
    /// <summary>Estimated dollar delta-notional of this print.</summary>
    [JsonPropertyName("estimated_delta_notional")] public double? EstimatedDeltaNotional { get; set; }
    /// <summary>Standalone gamma-$ this print would add if it were
    /// opening and fully dealer-absorbed. <strong>Not</strong> applied to
    /// the live chain — don't sum it against <c>/v1/flow/gex</c>.</summary>
    [JsonPropertyName("hypothetical_gex_impact_if_opening")] public double? HypotheticalGexImpactIfOpening { get; set; }
}

/// <summary>One scored unusual-flow signal — a coalesced view of one
/// notable (block-sized) print on a single contract. Same shape across
/// <c>GET /v1/flow/signals/{symbol}</c> and the <c>top_signals</c> array
/// of <c>GET /v1/flow/signals/{symbol}/summary</c>.</summary>
public sealed class FlowSignal
{
    /// <summary>Trade timestamp (ISO-8601 UTC).</summary>
    [JsonPropertyName("ts")] public string Ts { get; set; } = "";
    /// <summary>Contract expiry (<c>YYYY-MM-DD</c>).</summary>
    [JsonPropertyName("expiry")] public string Expiry { get; set; } = "";
    /// <summary>Contract strike price.</summary>
    [JsonPropertyName("strike")] public double? Strike { get; set; }
    /// <summary><c>"C"</c> (call) or <c>"P"</c> (put).</summary>
    [JsonPropertyName("right")] public string Right { get; set; } = "";
    /// <summary>Upstream buy/sell/mid aggressor classification (distinct
    /// from the NBBO <see cref="Aggressor"/> label).</summary>
    [JsonPropertyName("side")] public string Side { get; set; } = "";
    /// <summary>Trade price.</summary>
    [JsonPropertyName("price")] public double? Price { get; set; }
    /// <summary>Trade size in contracts.</summary>
    [JsonPropertyName("size")] public long? Size { get; set; }
    /// <summary>Dollar premium of this print: <c>price * size * 100</c>.</summary>
    [JsonPropertyName("premium")] public double? Premium { get; set; }
    /// <summary>Days to expiry at trade time.</summary>
    [JsonPropertyName("dte")] public int? Dte { get; set; }
    /// <summary><c>"block"</c> (lone block-sized print) or <c>"sweep"</c>
    /// (≥2 same-side prints on one contract within ~500ms).</summary>
    [JsonPropertyName("structure")] public string Structure { get; set; } = "";
    /// <summary>NBBO position at trade: <c>"above_ask"</c> /
    /// <c>"at_ask"</c> / <c>"mid"</c> / <c>"at_bid"</c> /
    /// <c>"below_bid"</c>.</summary>
    [JsonPropertyName("aggressor")] public string Aggressor { get; set; } = "";
    /// <summary>Contract-level OI-simulator inference:
    /// <c>"opening_bias"</c> / <c>"closing_bias"</c> / <c>"unknown"</c>.
    /// Not a per-print label.</summary>
    [JsonPropertyName("open_close_bias")] public string OpenCloseBias { get; set; } = "";
    /// <summary>Simulator confidence weight for the bias above.</summary>
    [JsonPropertyName("open_close_confidence")] public double? OpenCloseConfidence { get; set; }
    /// <summary>Signed simulator estimate of contracts opened (+) or
    /// closed (−) today on this contract.</summary>
    [JsonPropertyName("contract_net_oi_delta")] public long? ContractNetOiDelta { get; set; }
    /// <summary><c>"bullish"</c> / <c>"bearish"</c> / <c>"neutral"</c>.
    /// Neutral whenever <see cref="OpenCloseBias"/> ==
    /// <c>"closing_bias"</c> (can't attribute on unwinds) or
    /// <see cref="Side"/> == <c>"mid"</c>.</summary>
    [JsonPropertyName("intent")] public string Intent { get; set; } = "";
    /// <summary>0–100 composite (<see cref="ScoreBreakdown"/> components
    /// sum to this).</summary>
    [JsonPropertyName("score")] public int? Score { get; set; }
    /// <summary><c>"low"</c> / <c>"medium"</c> / <c>"high"</c>.</summary>
    [JsonPropertyName("conviction")] public string Conviction { get; set; } = "";
    /// <summary>Subset of <c>"sweep"</c>, <c>"block"</c>, <c>"opening"</c>,
    /// <c>"closing"</c>, <c>"0dte"</c>, <c>"whale"</c> (premium ≥ $1M),
    /// <c>"golden"</c> (top decile in this response set AND score ≥ 70
    /// absolute).</summary>
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    /// <summary>Score components — sum to <see cref="Score"/>.</summary>
    [JsonPropertyName("score_breakdown")] public FlowSignalScoreBreakdown? ScoreBreakdown { get; set; }
    /// <summary>Chain-derived context (greeks, moneyness, estimated delta-notional).</summary>
    [JsonPropertyName("enrichment")] public FlowSignalEnrichment? Enrichment { get; set; }
}

/// <summary>Typed response for <c>GET /v1/flow/signals/{symbol}</c>
/// (Alpha+). Scored, classified unusual-flow feed — each notable print
/// in the look-back window is coalesced into a signal, scored 0–100,
/// and ranked highest score first.</summary>
public sealed class FlowSignalsResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Look-back window applied (minutes).</summary>
    [JsonPropertyName("window_minutes")] public int? WindowMinutes { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Settled-chain reference levels (computed once per request).</summary>
    [JsonPropertyName("chain")] public FlowSignalsChain? Chain { get; set; }
    /// <summary>Number of signals returned (after server-side filtering).</summary>
    [JsonPropertyName("count")] public int? Count { get; set; }
    /// <summary>Signals, highest score first.</summary>
    [JsonPropertyName("signals")] public List<FlowSignal> Signals { get; set; } = new();
}

/// <summary>Typed response for
/// <c>GET /v1/flow/signals/{symbol}/summary</c> (Alpha+). Sums
/// classified premium across the window into bullish/bearish and
/// opening/closing buckets — a cheap "smart-money tilt" read for one
/// underlying.</summary>
public sealed class FlowSignalsSummaryResponse
{
    /// <summary>Underlying ticker echoed from the request path.</summary>
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    /// <summary>Timestamp this snapshot was computed for (ISO-8601 UTC).</summary>
    [JsonPropertyName("as_of")] public string AsOf { get; set; } = "";
    /// <summary>Look-back window applied (minutes).</summary>
    [JsonPropertyName("window_minutes")] public int? WindowMinutes { get; set; }
    /// <summary>Expiration filter echoed back, or null.</summary>
    [JsonPropertyName("expiry")] public string? Expiry { get; set; }
    /// <summary>Spot mid at the snapshot time.</summary>
    [JsonPropertyName("underlying_price")] public double? UnderlyingPrice { get; set; }
    /// <summary>Total signal count in the window (full count, not the
    /// <see cref="TopSignals"/> length).</summary>
    [JsonPropertyName("signal_count")] public int? SignalCount { get; set; }
    /// <summary>Sum of signal premium with <c>intent == "bullish"</c>.</summary>
    [JsonPropertyName("bullish_premium")] public double? BullishPremium { get; set; }
    /// <summary>Sum of signal premium with <c>intent == "bearish"</c>.</summary>
    [JsonPropertyName("bearish_premium")] public double? BearishPremium { get; set; }
    /// <summary><c>bullish_premium - bearish_premium</c>.</summary>
    [JsonPropertyName("net_directional_premium")] public double? NetDirectionalPremium { get; set; }
    /// <summary>Sum of signal premium with
    /// <c>open_close_bias == "opening_bias"</c>.</summary>
    [JsonPropertyName("opening_premium")] public double? OpeningPremium { get; set; }
    /// <summary>Sum of signal premium with
    /// <c>open_close_bias == "closing_bias"</c>.</summary>
    [JsonPropertyName("closing_premium")] public double? ClosingPremium { get; set; }
    /// <summary>Highest-scoring signals (≤ 10). Same shape as
    /// <see cref="FlowSignal"/>.</summary>
    [JsonPropertyName("top_signals")] public List<FlowSignal> TopSignals { get; set; } = new();
}
