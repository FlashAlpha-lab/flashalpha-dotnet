using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/stock/{symbol}/summary</c> (Free).
///
/// <para>The single most-comprehensive stock snapshot in the FlashAlpha API —
/// price, volatility (ATM IV, HV, VRP, 25-delta skew, IV term structure),
/// options flow (OI/volume by side, P/C ratios), dealer exposure
/// (GEX/DEX/VEX/CHEX with regime, walls, max pain, top strikes, hedging
/// estimates, 0DTE breakdown), and a macro snapshot (VIX/VVIX/SKEW/MOVE/SPX,
/// VIX term structure, fear-and-greed). One call, one POCO, everything an
/// LLM agent needs to answer "what does positioning look like in &lt;symbol&gt;
/// right now?".</para>
///
/// <para><b>Dual-mode endpoint:</b> authenticated requests return LIVE data;
/// unauthenticated requests return the previous-day cached snapshot
/// (populated daily at market open). Use <see cref="MarketOpen"/> and
/// <see cref="AsOf"/> to disambiguate at the call site.</para>
///
/// <para><b>Null-safety hot spots:</b>
/// <list type="bullet">
///   <item><see cref="Exposure"/> is <c>null</c> when no options/greeks are
///     loaded for the symbol — common for thinly-traded names.</item>
///   <item><see cref="Macro"/> sub-fields (vix, vvix, skew, move, spx,
///     vix_term_structure, vix_futures, fear_and_greed) are <c>null</c>
///     individually when the external data source is unavailable.</item>
///   <item><see cref="StockSummaryHedgingEstimate.DealerShares"/> is
///     <b>magnitude</b> (always positive) — the <c>direction</c> string
///     carries the sign. This DIFFERS from the zero-DTE endpoint, which
///     returns signed values.</item>
/// </list></para>
/// </summary>
public sealed class StockSummaryResponse
{
    /// <summary>Echoed from the request path (e.g. <c>"SPY"</c>).</summary>
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>UTC timestamp this snapshot was computed for.</summary>
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary><c>true</c> if the US equity market is open at <see cref="AsOf"/>.</summary>
    [JsonPropertyName("market_open")]
    public bool? MarketOpen { get; set; }

    /// <summary>Top-of-book price block. See <see cref="StockSummaryPrice"/>.</summary>
    [JsonPropertyName("price")]
    public StockSummaryPrice? Price { get; set; }

    /// <summary>Volatility block (ATM IV, HV, VRP, 25-delta skew, IV term structure).</summary>
    [JsonPropertyName("volatility")]
    public StockSummaryVolatility? Volatility { get; set; }

    /// <summary>Aggregate options-flow stats (OI, volume, P/C ratios). See <see cref="StockSummaryOptionsFlow"/>.</summary>
    [JsonPropertyName("options_flow")]
    public StockSummaryOptionsFlow? OptionsFlow { get; set; }

    /// <summary>
    /// Dealer exposure block (GEX/DEX/VEX/CHEX, regime, walls, max pain, hedging
    /// estimates, 0DTE, top strikes). <c>null</c> if no options/greeks data
    /// is loaded for the symbol.
    /// </summary>
    [JsonPropertyName("exposure")]
    public StockSummaryExposure? Exposure { get; set; }

    /// <summary>Macro context block (VIX, VVIX, SKEW, MOVE, SPX, term structure, fear-and-greed).</summary>
    [JsonPropertyName("macro")]
    public StockSummaryMacro? Macro { get; set; }
}

/// <summary>Top-of-book price block (bid/ask/mid/last + last-update timestamp).</summary>
public sealed class StockSummaryPrice
{
    /// <summary>Best bid.</summary>
    [JsonPropertyName("bid")]
    public double? Bid { get; set; }

    /// <summary>Best ask.</summary>
    [JsonPropertyName("ask")]
    public double? Ask { get; set; }

    /// <summary>Mid price: <c>(bid + ask) / 2</c>.</summary>
    [JsonPropertyName("mid")]
    public double? Mid { get; set; }

    /// <summary>Last trade price.</summary>
    [JsonPropertyName("last")]
    public double? Last { get; set; }

    /// <summary>UTC timestamp of the most recent quote/trade behind this block.</summary>
    [JsonPropertyName("last_update")]
    public string? LastUpdate { get; set; }
}

/// <summary>
/// Volatility block — ATM IV, historical vol, VRP, 25-delta skew, IV term
/// structure.
///
/// <para><b>Units:</b> <see cref="AtmIv"/>, <see cref="Hv20"/>,
/// <see cref="Hv60"/>, <see cref="Vrp"/> are PERCENT (e.g. <c>18.45</c> = 18.45%).
/// Do NOT divide by 100 before display.</para>
///
/// <para><b>Term-structure filter:</b> <see cref="IvTermStructure"/> rows
/// with IV below 5% or above 200% are filtered out server-side (treated as
/// bad SVI fits). The list may therefore be shorter than the full chain.</para>
/// </summary>
public sealed class StockSummaryVolatility
{
    /// <summary>At-the-money implied volatility, near-term expiry (annualised %).</summary>
    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    /// <summary>20-day trailing realized vol (annualised %).</summary>
    [JsonPropertyName("hv_20")]
    public double? Hv20 { get; set; }

    /// <summary>60-day trailing realized vol (annualised %).</summary>
    [JsonPropertyName("hv_60")]
    public double? Hv60 { get; set; }

    /// <summary>Variance risk premium: <c>atm_iv - hv_20</c>. Positive = options pricing more vol than realized.</summary>
    [JsonPropertyName("vrp")]
    public double? Vrp { get; set; }

    /// <summary>25-delta skew block. See <see cref="StockSummarySkew25d"/>.</summary>
    [JsonPropertyName("skew_25d")]
    public StockSummarySkew25d? Skew25d { get; set; }

    /// <summary>IV term structure (one row per liquid expiry). Rows with IV &lt;5% or &gt;200% are filtered out.</summary>
    [JsonPropertyName("iv_term_structure")]
    public List<StockSummaryIvTermItem>? IvTermStructure { get; set; }
}

/// <summary>
/// 25-delta skew block — directional pricing of OTM puts vs OTM calls at the
/// 25-delta wings.
///
/// <para><see cref="Skew25dValue"/> = <c>put_25d_iv - call_25d_iv</c>. Large
/// positive values indicate "expensive crash insurance" — typical for index
/// products. <see cref="SmileRatio"/> = <c>put_25d_iv / call_25d_iv</c>.</para>
/// </summary>
public sealed class StockSummarySkew25d
{
    /// <summary>Expiry this 25-delta skew was measured at.</summary>
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary>Days to expiry for this skew measurement.</summary>
    [JsonPropertyName("days_to_expiry")]
    public int? DaysToExpiry { get; set; }

    /// <summary>IV at the 25-delta put wing (annualised %).</summary>
    [JsonPropertyName("put_25d_iv")]
    public double? Put25dIv { get; set; }

    /// <summary>ATM IV for this expiry (annualised %).</summary>
    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }

    /// <summary>IV at the 25-delta call wing (annualised %).</summary>
    [JsonPropertyName("call_25d_iv")]
    public double? Call25dIv { get; set; }

    /// <summary><c>put_25d_iv - call_25d_iv</c>. Positive = put-skew (crash insurance richer than upside).</summary>
    [JsonPropertyName("skew_25d")]
    public double? Skew25dValue { get; set; }

    /// <summary><c>put_25d_iv / call_25d_iv</c>. Ratio form of the skew.</summary>
    [JsonPropertyName("smile_ratio")]
    public double? SmileRatio { get; set; }
}

/// <summary>One row of the IV term structure.</summary>
public sealed class StockSummaryIvTermItem
{
    /// <summary>Expiry date for this point on the term structure.</summary>
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary>ATM implied vol at this expiry (annualised %).</summary>
    [JsonPropertyName("iv")]
    public double? Iv { get; set; }

    /// <summary>Days to expiry from <c>as_of</c>.</summary>
    [JsonPropertyName("days_to_expiry")]
    public int? DaysToExpiry { get; set; }
}

/// <summary>
/// Aggregate options-flow stats — total OI and volume by side plus
/// put/call ratios.
/// </summary>
public sealed class StockSummaryOptionsFlow
{
    /// <summary>Sum of call open interest across all expirations.</summary>
    [JsonPropertyName("total_call_oi")]
    public long? TotalCallOi { get; set; }

    /// <summary>Sum of put open interest across all expirations.</summary>
    [JsonPropertyName("total_put_oi")]
    public long? TotalPutOi { get; set; }

    /// <summary>Sum of call volume across all expirations (today's trades).</summary>
    [JsonPropertyName("total_call_volume")]
    public long? TotalCallVolume { get; set; }

    /// <summary>Sum of put volume across all expirations.</summary>
    [JsonPropertyName("total_put_volume")]
    public long? TotalPutVolume { get; set; }

    /// <summary><c>total_put_oi / total_call_oi</c>. &gt;1.0 = put-heavy positioning.</summary>
    [JsonPropertyName("pc_ratio_oi")]
    public double? PcRatioOi { get; set; }

    /// <summary><c>total_put_volume / total_call_volume</c>. Today's flow ratio.</summary>
    [JsonPropertyName("pc_ratio_volume")]
    public double? PcRatioVolume { get; set; }

    /// <summary>Number of distinct expirations contributing OI/volume.</summary>
    [JsonPropertyName("active_expirations")]
    public int? ActiveExpirations { get; set; }
}

/// <summary>
/// Dealer exposure block — GEX/DEX/VEX/CHEX with regime classification,
/// walls, max pain, hedging estimates, 0DTE breakdown, and top strikes.
///
/// <para><see cref="Regime"/> is one of <c>"positive_gamma"</c>,
/// <c>"negative_gamma"</c>, or <c>"unknown"</c> — derived from spot
/// vs <see cref="GammaFlip"/>. In positive gamma, dealers buy dips and sell
/// rips (vol-suppressing). In negative gamma, dealers chase the move
/// (vol-amplifying).</para>
///
/// <para><see cref="TopStrikes"/> returns up to 5 strikes ranked by absolute
/// net GEX.</para>
/// </summary>
public sealed class StockSummaryExposure
{
    /// <summary>Net dealer gamma exposure in dollars per 1% spot move.</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    /// <summary>Net dealer delta exposure in dollars.</summary>
    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    /// <summary>Net dealer vanna exposure (vol/spot cross-greek), dollars per 1 vol-pt × 1% spot.</summary>
    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    /// <summary>Net dealer charm exposure (delta-decay), dollars per day per 1% spot.</summary>
    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    /// <summary>Strike where net dealer gamma flips sign. Spot ABOVE = positive-gamma regime.</summary>
    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    /// <summary>Strike with the highest absolute call GEX — acts as resistance / dealer-sell zone.</summary>
    [JsonPropertyName("call_wall")]
    public double? CallWall { get; set; }

    /// <summary>Strike with the highest absolute put GEX — acts as support / dealer-buy zone.</summary>
    [JsonPropertyName("put_wall")]
    public double? PutWall { get; set; }

    /// <summary>Strike where total chain pain is minimized.</summary>
    [JsonPropertyName("max_pain")]
    public double? MaxPain { get; set; }

    /// <summary>Strike with the highest aggregate OI (calls + puts).</summary>
    [JsonPropertyName("highest_oi_strike")]
    public double? HighestOiStrike { get; set; }

    /// <summary>
    /// <c>"positive_gamma"</c> | <c>"negative_gamma"</c> | <c>"unknown"</c>.
    /// Derived from spot vs <see cref="GammaFlip"/>.
    /// </summary>
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    /// <summary>Short verbal explanations per greek. Safe to surface verbatim to end users / LLMs.</summary>
    [JsonPropertyName("interpretation")]
    public StockSummaryInterpretation? Interpretation { get; set; }

    /// <summary>Estimated dealer hedging response to ±1% spot moves. See <see cref="StockSummaryHedgingEstimateBlock"/>.</summary>
    [JsonPropertyName("hedging_estimate")]
    public StockSummaryHedgingEstimateBlock? HedgingEstimate { get; set; }

    /// <summary>0DTE-only slice of GEX. See <see cref="StockSummaryZeroDte"/>.</summary>
    [JsonPropertyName("zero_dte")]
    public StockSummaryZeroDte? ZeroDte { get; set; }

    /// <summary>Up to 5 strikes ranked by absolute net GEX.</summary>
    [JsonPropertyName("top_strikes")]
    public List<StockSummaryTopStrike>? TopStrikes { get; set; }

    /// <summary>OI-weighted average days-to-expiry across the chain.</summary>
    [JsonPropertyName("oi_weighted_dte")]
    public double? OiWeightedDte { get; set; }
}

/// <summary>
/// Plain-English explanations of the gamma / vanna / charm regime. Safe to
/// surface verbatim — these are deliberately written for end-user / LLM
/// consumption.
/// </summary>
public sealed class StockSummaryInterpretation
{
    /// <summary>Verbal description of the gamma regime and what dealer hedging implies.</summary>
    [JsonPropertyName("gamma")]
    public string? Gamma { get; set; }

    /// <summary>Verbal description of the vanna exposure (cross-effect of vol changes on dealer delta).</summary>
    [JsonPropertyName("vanna")]
    public string? Vanna { get; set; }

    /// <summary>Verbal description of charm (delta-decay / time-induced re-hedging).</summary>
    [JsonPropertyName("charm")]
    public string? Charm { get; set; }
}

/// <summary>
/// Estimated dealer hedging response to ±1% spot moves.
///
/// <para><b>Sign convention:</b> <see cref="StockSummaryHedgingEstimate.DealerShares"/>
/// is MAGNITUDE (always positive); <see cref="StockSummaryHedgingEstimate.Direction"/>
/// (<c>"buy"</c> / <c>"sell"</c>) carries the sign. This DIFFERS from the
/// zero-DTE endpoint which returns signed values directly.</para>
/// </summary>
public sealed class StockSummaryHedgingEstimateBlock
{
    /// <summary>Estimated dealer hedge if spot drops 1%.</summary>
    [JsonPropertyName("spot_down_1pct")]
    public StockSummaryHedgingEstimate? SpotDown1Pct { get; set; }

    /// <summary>Estimated dealer hedge if spot rises 1%.</summary>
    [JsonPropertyName("spot_up_1pct")]
    public StockSummaryHedgingEstimate? SpotUp1Pct { get; set; }
}

/// <summary>One side of the dealer-hedging estimate (down-1% or up-1%).</summary>
public sealed class StockSummaryHedgingEstimate
{
    /// <summary>Magnitude of shares the dealer must trade to re-hedge. Always positive — sign is in <see cref="Direction"/>.</summary>
    [JsonPropertyName("dealer_shares")]
    public long? DealerShares { get; set; }

    /// <summary><c>"buy"</c> or <c>"sell"</c>. Carries the sign that <see cref="DealerShares"/> drops.</summary>
    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    /// <summary>Notional dollar value of the hedging flow: <c>dealer_shares * spot</c>.</summary>
    [JsonPropertyName("notional_usd")]
    public double? NotionalUsd { get; set; }
}

/// <summary>
/// 0DTE-only slice of net GEX.
///
/// <para>0DTE flow disproportionately drives intraday vol because gamma
/// concentrates as expiry approaches. <see cref="PctOfTotal"/> captures how
/// much of today's exposure profile is dominated by same-day expiries.</para>
/// </summary>
public sealed class StockSummaryZeroDte
{
    /// <summary>Net GEX from 0DTE expiry only (dollars per 1% spot move).</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    /// <summary>0DTE GEX as a percent of full-chain net GEX.</summary>
    [JsonPropertyName("pct_of_total")]
    public double? PctOfTotal { get; set; }

    /// <summary>The 0DTE expiry date used for this slice.</summary>
    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }
}

/// <summary>One of the top-N strikes ranked by absolute net GEX.</summary>
public sealed class StockSummaryTopStrike
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary>Net dealer gamma at this strike (dollars per 1% spot move).</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("call_oi")]
    public long? CallOi { get; set; }

    [JsonPropertyName("put_oi")]
    public long? PutOi { get; set; }

    [JsonPropertyName("total_oi")]
    public long? TotalOi { get; set; }
}

/// <summary>
/// Macro context block — VIX/VVIX/SKEW/SPX/MOVE plus VIX term structure
/// and fear-and-greed.
///
/// <para><b>Null behavior:</b> any sub-field can be <c>null</c> if the
/// external data source (FRED / CBOE / CNN) is unavailable. Treat each
/// sub-block as best-effort.</para>
///
/// <para><b>Approximation note:</b>
/// <see cref="StockSummaryVixFutures.Basis"/> is approximated from VIX3M vs
/// VIX spot — NOT actual VX futures prices. Treat as an indicative
/// contango/backwardation label rather than a tradeable quote.</para>
/// </summary>
public sealed class StockSummaryMacro
{
    /// <summary>CBOE VIX (30-day implied vol on SPX).</summary>
    [JsonPropertyName("vix")]
    public StockSummaryMacroQuote? Vix { get; set; }

    /// <summary>VVIX — vol of vol on the VIX index itself.</summary>
    [JsonPropertyName("vvix")]
    public StockSummaryMacroQuote? Vvix { get; set; }

    /// <summary>CBOE SKEW index — tail-risk pricing on SPX puts.</summary>
    [JsonPropertyName("skew")]
    public StockSummaryMacroQuote? Skew { get; set; }

    /// <summary>SPX cash index level + change.</summary>
    [JsonPropertyName("spx")]
    public StockSummaryMacroQuote? Spx { get; set; }

    /// <summary>ICE BofA MOVE index — Treasury-vol equivalent of VIX.</summary>
    [JsonPropertyName("move")]
    public StockSummaryMacroQuote? Move { get; set; }

    /// <summary>VIX term structure across vix9d / vix / vix3m / vix6m + slope label.</summary>
    [JsonPropertyName("vix_term_structure")]
    public StockSummaryVixTermStructure? VixTermStructure { get; set; }

    /// <summary>VIX-futures snapshot (front-month vs spot, basis label). Approximated from VIX3M.</summary>
    [JsonPropertyName("vix_futures")]
    public StockSummaryVixFutures? VixFutures { get; set; }

    /// <summary>CNN Fear &amp; Greed score and verbal rating.</summary>
    [JsonPropertyName("fear_and_greed")]
    public StockSummaryFearAndGreed? FearAndGreed { get; set; }
}

/// <summary>One macro quote line — current value plus day change in absolute and percent.</summary>
public sealed class StockSummaryMacroQuote
{
    [JsonPropertyName("value")]
    public double? Value { get; set; }

    /// <summary>Absolute change vs prior close.</summary>
    [JsonPropertyName("change")]
    public double? Change { get; set; }

    /// <summary>Percent change vs prior close.</summary>
    [JsonPropertyName("change_pct")]
    public double? ChangePct { get; set; }
}

/// <summary>
/// VIX term structure across the standard CBOE tenors plus a slope label.
///
/// <para><see cref="Structure"/> is <c>"contango"</c> when longer tenors
/// trade above shorter, <c>"backwardation"</c> when inverted. Backwardation
/// is the canonical "fear-on" structure.</para>
/// </summary>
public sealed class StockSummaryVixTermStructure
{
    /// <summary>Levels block — VIX9D / VIX / VIX3M / VIX6M.</summary>
    [JsonPropertyName("levels")]
    public StockSummaryVixTermLevels? Levels { get; set; }

    /// <summary>Near slope as a percent: <c>(vix3m - vix) / vix * 100</c>.</summary>
    [JsonPropertyName("near_slope_pct")]
    public double? NearSlopePct { get; set; }

    /// <summary><c>"contango"</c> | <c>"backwardation"</c>.</summary>
    [JsonPropertyName("structure")]
    public string? Structure { get; set; }
}

/// <summary>VIX term-structure tenor levels.</summary>
public sealed class StockSummaryVixTermLevels
{
    [JsonPropertyName("vix9d")]
    public double? Vix9d { get; set; }

    [JsonPropertyName("vix")]
    public double? Vix { get; set; }

    [JsonPropertyName("vix3m")]
    public double? Vix3m { get; set; }

    [JsonPropertyName("vix6m")]
    public double? Vix6m { get; set; }
}

/// <summary>
/// VIX-futures snapshot (front-month vs spot).
///
/// <para><b>Approximation:</b> <see cref="Basis"/> is computed from VIX3M
/// vs VIX spot, not from actual VX futures prices. It's an indicative
/// label, not a tradeable quote.</para>
/// </summary>
public sealed class StockSummaryVixFutures
{
    /// <summary>Front-month VIX-future proxy (uses VIX3M).</summary>
    [JsonPropertyName("front_month")]
    public double? FrontMonth { get; set; }

    /// <summary>VIX spot.</summary>
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    /// <summary><c>front_month - spot</c>.</summary>
    [JsonPropertyName("spread")]
    public double? Spread { get; set; }

    /// <summary>Spread as percent of spot.</summary>
    [JsonPropertyName("basis_pct")]
    public double? BasisPct { get; set; }

    /// <summary><c>"contango"</c> | <c>"backwardation"</c>.</summary>
    [JsonPropertyName("basis")]
    public string? Basis { get; set; }
}

/// <summary>CNN Fear &amp; Greed score (0-100) plus verbal bucket.</summary>
public sealed class StockSummaryFearAndGreed
{
    /// <summary>0-100. Lower = fear, higher = greed.</summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>Verbal bucket: <c>"Extreme Fear"</c>, <c>"Fear"</c>, <c>"Neutral"</c>, <c>"Greed"</c>, <c>"Extreme Greed"</c>.</summary>
    [JsonPropertyName("rating")]
    public string? Rating { get; set; }
}
