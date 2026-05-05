using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/vrp/{symbol}</c> (Alpha+).
///
/// <para>The single most-misread response shape in the FlashAlpha API. Every
/// nested block exists for a reason — core metrics, directional skew, gamma
/// conditioning, vanna conditioning, regime, strategy scores, and macro
/// context are deliberately separated.</para>
///
/// <para><b>Common silent-null traps</b> (now type-checked at the SDK boundary):
/// <list type="bullet">
///   <item><c>response.ZScore</c> ✗ → use <c>response.Vrp.ZScore</c></item>
///   <item><c>response.Percentile</c> ✗ → use <c>response.Vrp.Percentile</c></item>
///   <item><c>response.AtmIv</c> ✗ → use <c>response.Vrp.AtmIv</c></item>
///   <item><c>response.PutVrp</c> ✗ → use <c>response.Directional.DownsideVrp</c></item>
///   <item><c>response.NetGex</c> ✗ → use <c>response.Regime.NetGex</c></item>
///   <item><c>response.HarvestScore</c> (top-level) ✗ → use
///     <c>response.GexConditioned.HarvestScore</c>;
///     <c>response.NetHarvestScore</c> is a SEPARATE composite.</item>
/// </list></para>
///
/// <para>Returns 403 <c>tier_restricted</c> for anything below Alpha plan.</para>
/// </summary>
public sealed class VrpResponse
{
    /// <summary>Echoed from the request path (e.g. <c>"SPY"</c>).</summary>
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>Spot mid at <c>as_of</c>.</summary>
    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    /// <summary>ET wall-clock timestamp this snapshot was computed for.</summary>
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary><c>true</c> if NYSE was open at <c>as_of</c>.</summary>
    [JsonPropertyName("market_open")]
    public bool? MarketOpen { get; set; }

    /// <summary>Core VRP metrics block. See <see cref="VrpCore"/>.</summary>
    [JsonPropertyName("vrp")]
    public VrpCore? Vrp { get; set; }

    /// <summary><c>vrp_20d / 100</c> as a decimal. Same as <c>Vrp.Vrp20d / 100</c>.</summary>
    [JsonPropertyName("variance_risk_premium")]
    public double? VarianceRiskPremium { get; set; }

    /// <summary><c>fair_vol - atm_iv</c>. Curvature premium between IV smile and var-swap fair vol.</summary>
    [JsonPropertyName("convexity_premium")]
    public double? ConvexityPremium { get; set; }

    /// <summary>Variance-swap fair vol (annualised %).</summary>
    [JsonPropertyName("fair_vol")]
    public double? FairVol { get; set; }

    /// <summary>Directional VRP skew (downside vs upside). See <see cref="VrpDirectional"/>.</summary>
    [JsonPropertyName("directional")]
    public VrpDirectional? Directional { get; set; }

    /// <summary>Term structure — array of <see cref="VrpTermItem"/>. Empty when surface fitting fails.</summary>
    [JsonPropertyName("term_vrp")]
    public List<VrpTermItem>? TermVrp { get; set; }

    /// <summary>GEX-conditioned harvest score. See <see cref="VrpGexConditioned"/>.</summary>
    [JsonPropertyName("gex_conditioned")]
    public VrpGexConditioned? GexConditioned { get; set; }

    /// <summary>Vanna-conditioned outlook. See <see cref="VrpVannaConditioned"/>.</summary>
    [JsonPropertyName("vanna_conditioned")]
    public VrpVannaConditioned? VannaConditioned { get; set; }

    /// <summary>Regime snapshot block. <c>NetGex</c> lives HERE, not at the top level.</summary>
    [JsonPropertyName("regime")]
    public VrpRegime? Regime { get; set; }

    /// <summary>0-100 strategy suitability scores. <c>null</c> on historical when warmup is short.</summary>
    [JsonPropertyName("strategy_scores")]
    public VrpStrategyScores? StrategyScores { get; set; }

    /// <summary>0-100 composite harvest signal. <c>null</c> on historical when warmup is short.</summary>
    [JsonPropertyName("net_harvest_score")]
    public int? NetHarvestScore { get; set; }

    /// <summary>0-100 — risk that dealer hedging flow disrupts a short-vol harvest.</summary>
    [JsonPropertyName("dealer_flow_risk")]
    public int? DealerFlowRisk { get; set; }

    /// <summary>Server-side warnings about data quality. Always present (possibly empty).</summary>
    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    /// <summary>Macro context. See <see cref="VrpMacro"/>.</summary>
    [JsonPropertyName("macro")]
    public VrpMacro? Macro { get; set; }
}

/// <summary>
/// Core VRP metrics block — the heart of the response.
///
/// <para>The variance risk premium is the spread between IMPLIED volatility
/// (forward-looking, priced into options) and REALIZED volatility
/// (backward-looking, observed from spot returns). Positive VRP = options
/// pricing more vol than the underlying actually moved → premium for selling
/// vol. Negative VRP = options too cheap relative to realized → premium for
/// buying vol.</para>
///
/// <para>Nested under <c>response.Vrp</c> — NOT top-level.</para>
/// </summary>
public sealed class VrpCore
{
    /// <summary>At-the-money implied volatility (annualised %, e.g. 18.5 = 18.5%).</summary>
    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    /// <summary>Realized vol over trailing 5 trading days (annualised %).</summary>
    [JsonPropertyName("rv_5d")]
    public double? Rv5d { get; set; }

    [JsonPropertyName("rv_10d")]
    public double? Rv10d { get; set; }

    [JsonPropertyName("rv_20d")]
    public double? Rv20d { get; set; }

    [JsonPropertyName("rv_30d")]
    public double? Rv30d { get; set; }

    /// <summary>Variance risk premium at this horizon: <c>atm_iv - rv_Nd</c>.</summary>
    [JsonPropertyName("vrp_5d")]
    public double? Vrp5d { get; set; }

    [JsonPropertyName("vrp_10d")]
    public double? Vrp10d { get; set; }

    [JsonPropertyName("vrp_20d")]
    public double? Vrp20d { get; set; }

    [JsonPropertyName("vrp_30d")]
    public double? Vrp30d { get; set; }

    /// <summary>Z-score of current 20-day VRP vs trailing window. <c>null</c> when warmup is insufficient.</summary>
    [JsonPropertyName("z_score")]
    public double? ZScore { get; set; }

    /// <summary>Percentile rank (0-100) within trailing window. <c>null</c> when warmup is short.</summary>
    [JsonPropertyName("percentile")]
    public int? Percentile { get; set; }

    /// <summary>Trading days in the trailing percentile/z-score window.</summary>
    [JsonPropertyName("history_days")]
    public int? HistoryDays { get; set; }
}

/// <summary>
/// Directional VRP skew — separates upside-tail vs downside-tail premia.
///
/// <para>Splits the VRP by direction: DOWNSIDE (puts) vs UPSIDE (calls). A
/// large <c>DownsideVrp</c> with small <c>UpsideVrp</c> is the classic
/// "expensive crash insurance" pattern — premium for selling puts in calm
/// tape.</para>
///
/// <para>The canonical names are <c>downside_vrp</c> / <c>upside_vrp</c>.
/// Customers from other vendors type <c>put_vrp</c> / <c>call_vrp</c> —
/// those don't exist here.</para>
/// </summary>
public sealed class VrpDirectional
{
    /// <summary>IV at the 25-delta put wing (bottom-tail crash insurance pricing).</summary>
    [JsonPropertyName("put_wing_iv_25d")]
    public double? PutWingIv25d { get; set; }

    /// <summary>IV at the 25-delta call wing (top-tail upside insurance pricing).</summary>
    [JsonPropertyName("call_wing_iv_25d")]
    public double? CallWingIv25d { get; set; }

    [JsonPropertyName("downside_rv_20d")]
    public double? DownsideRv20d { get; set; }

    [JsonPropertyName("upside_rv_20d")]
    public double? UpsideRv20d { get; set; }

    /// <summary><c>put_wing_iv_25d - downside_rv_20d</c>. Positive = downside crash protection priced rich.</summary>
    [JsonPropertyName("downside_vrp")]
    public double? DownsideVrp { get; set; }

    /// <summary><c>call_wing_iv_25d - upside_rv_20d</c>. Positive = upside calls priced rich.</summary>
    [JsonPropertyName("upside_vrp")]
    public double? UpsideVrp { get; set; }
}

/// <summary>One row of the VRP term structure.</summary>
public sealed class VrpTermItem
{
    /// <summary>Days to expiry for this row (e.g. 7, 14, 30, 60, 90).</summary>
    [JsonPropertyName("dte")]
    public int? Dte { get; set; }

    /// <summary>Implied vol at this tenor (annualised %).</summary>
    [JsonPropertyName("iv")]
    public double? Iv { get; set; }

    /// <summary>Realized vol over a window matched to the tenor (annualised %).</summary>
    [JsonPropertyName("rv")]
    public double? Rv { get; set; }

    /// <summary>Tenor-matched VRP: <c>iv - rv</c>.</summary>
    [JsonPropertyName("vrp")]
    public double? Vrp { get; set; }
}

/// <summary>VRP harvest score conditioned on the prevailing dealer-gamma regime.</summary>
public sealed class VrpGexConditioned
{
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    /// <summary>0-100 composite. >70 = strong harvest signal; &lt;30 = avoid.</summary>
    [JsonPropertyName("harvest_score")]
    public double? HarvestScore { get; set; }

    /// <summary>Plain-English explanation; safe to surface verbatim.</summary>
    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }
}

/// <summary>VRP outlook conditioned on net dealer vanna exposure.</summary>
public sealed class VrpVannaConditioned
{
    /// <summary>Forward-looking outlook label (e.g. <c>"vanna_supportive"</c>, <c>"vanna_cascade_risk"</c>).</summary>
    [JsonPropertyName("outlook")]
    public string? Outlook { get; set; }

    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }
}

/// <summary>
/// Regime snapshot block.
///
/// <para><c>NetGex</c> lives HERE, not at the top level. <c>response.NetGex</c>
/// is null/missing; use <c>response.Regime.NetGex</c>.</para>
/// </summary>
public sealed class VrpRegime
{
    /// <summary><c>"positive_gamma"</c> | <c>"negative_gamma"</c> | <c>"neutral"</c> | <c>"undetermined"</c>.</summary>
    [JsonPropertyName("gamma")]
    public string? Gamma { get; set; }

    /// <summary>
    /// <c>"harvestable"</c> | <c>"selling_too_cheap"</c> | etc. <c>null</c> on
    /// historical with insufficient warmup. (Property is named
    /// <c>VrpRegimeLabel</c> rather than <c>VrpRegime</c> because C# disallows
    /// a property name matching its enclosing class. JSON wire name is
    /// unchanged: <c>"vrp_regime"</c>.)
    /// </summary>
    [JsonPropertyName("vrp_regime")]
    public string? VrpRegimeLabel { get; set; }

    /// <summary>Net dealer gamma exposure in dollars per 1% spot move.</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }
}

/// <summary>
/// 0-100 suitability scores for canonical short-vol strategies.
///
/// <para>Higher = better fit for current market conditions. Each field can
/// be <c>null</c> on historical responses where the underlying inputs (e.g.
/// percentile) aren't computable.</para>
/// </summary>
public sealed class VrpStrategyScores
{
    /// <summary>Short put credit spread — sells downside VRP with capped loss.</summary>
    [JsonPropertyName("short_put_spread")]
    public int? ShortPutSpread { get; set; }

    /// <summary>Short strangle — sells both wings; max profit if spot pins.</summary>
    [JsonPropertyName("short_strangle")]
    public int? ShortStrangle { get; set; }

    /// <summary>Iron condor — defined-risk version of short strangle.</summary>
    [JsonPropertyName("iron_condor")]
    public int? IronCondor { get; set; }

    /// <summary>Calendar spread — sells front-month vol, buys back-month.</summary>
    [JsonPropertyName("calendar_spread")]
    public int? CalendarSpread { get; set; }
}

/// <summary>
/// Macro-context snapshot used to condition the VRP outlook.
///
/// <para>Note diffs across live vs historical:
/// <list type="bullet">
///   <item><c>HySpread</c>: live = <c>null</c>; historical = float.</item>
///   <item><c>FedFunds</c>: live = float; historical = field absent.</item>
/// </list></para>
/// </summary>
public sealed class VrpMacro
{
    /// <summary>CBOE VIX index level.</summary>
    [JsonPropertyName("vix")]
    public double? Vix { get; set; }

    /// <summary>CBOE VIX3M (3-month VIX).</summary>
    [JsonPropertyName("vix_3m")]
    public double? Vix3m { get; set; }

    /// <summary><c>(vix_3m - vix) / vix * 100</c> — positive = contango.</summary>
    [JsonPropertyName("vix_term_slope")]
    public double? VixTermSlope { get; set; }

    /// <summary>10-year US Treasury yield (%, FRED <c>DGS10</c>).</summary>
    [JsonPropertyName("dgs10")]
    public double? Dgs10 { get; set; }

    /// <summary>ICE BofA US HY OAS. Live currently <c>null</c>; historical populated.</summary>
    [JsonPropertyName("hy_spread")]
    public double? HySpread { get; set; }

    /// <summary>Fed Funds effective rate (%, FRED <c>DFF</c>). Live-only — absent on historical.</summary>
    [JsonPropertyName("fed_funds")]
    public double? FedFunds { get; set; }
}
