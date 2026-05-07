using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/exposure/narrative/{symbol}</c> (Growth+).
///
/// <para>The <b>verbal, LLM-friendly</b> view of dealer positioning. Each
/// string under <see cref="NarrativeResponse.Narrative"/> is a hand-tuned,
/// numbers-aware sentence describing one facet of the current setup —
/// regime, gex change, key levels, flow, vanna, charm, 0DTE, and outlook.
/// Every line is safe to surface verbatim to an end user, an LLM agent, or
/// a chat UI without further post-processing.</para>
///
/// <para>The <see cref="NarrativeBlock.Data"/> sub-block carries the raw
/// numbers backing the prose — useful when you want the prose AND the
/// underlying values for charts or further computation in the same call.</para>
///
/// <para>Returns 403 <c>tier_restricted</c> below Growth plan.</para>
/// </summary>
public sealed class NarrativeResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    /// <summary>UTC timestamp this narrative was computed for.</summary>
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>The narrative payload — prose lines plus the raw <c>data</c> block.</summary>
    [JsonPropertyName("narrative")]
    public NarrativeBlock? Narrative { get; set; }
}

/// <summary>
/// The narrative payload — verbal lines plus raw numbers.
///
/// <para>Each prose field describes one facet of the dealer-flow picture.
/// They are independent — surface any subset (e.g. just <see cref="Regime"/>
/// + <see cref="Outlook"/> for a one-paragraph TL;DR; the full set for a
/// detailed report).</para>
/// </summary>
public sealed class NarrativeBlock
{
    /// <summary>Sentence describing the current gamma regime and what it means for vol.</summary>
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    /// <summary>Sentence describing how net GEX has shifted since the prior session.</summary>
    [JsonPropertyName("gex_change")]
    public string? GexChange { get; set; }

    /// <summary>Sentence naming the call wall, put wall, and gamma flip in plain English.</summary>
    [JsonPropertyName("key_levels")]
    public string? KeyLevels { get; set; }

    /// <summary>Sentence describing notable OI / volume changes by strike and side.</summary>
    [JsonPropertyName("flow")]
    public string? Flow { get; set; }

    /// <summary>Sentence describing net vanna exposure in the context of current vol.</summary>
    [JsonPropertyName("vanna")]
    public string? Vanna { get; set; }

    /// <summary>Sentence describing net charm exposure (delta-decay through time).</summary>
    [JsonPropertyName("charm")]
    public string? Charm { get; set; }

    /// <summary>Sentence describing the 0DTE share of total exposure.</summary>
    [JsonPropertyName("zero_dte")]
    public string? ZeroDte { get; set; }

    /// <summary>Forward-looking sentence summarising the setup. Closest thing to a recommendation.</summary>
    [JsonPropertyName("outlook")]
    public string? Outlook { get; set; }

    /// <summary>Raw numbers backing the prose. See <see cref="NarrativeData"/>.</summary>
    [JsonPropertyName("data")]
    public NarrativeData? Data { get; set; }
}

/// <summary>
/// Raw numbers backing the prose lines in <see cref="NarrativeBlock"/>.
///
/// <para>Every field here is the source-of-truth value referenced verbatim
/// by one of the <c>narrative.*</c> sentences. Use this block when you want
/// both the prose and the underlying numbers in a single call.</para>
/// </summary>
public sealed class NarrativeData
{
    /// <summary>Net dealer gamma exposure at <c>as_of</c> (dollars per 1% spot move).</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    /// <summary>Prior-session net GEX, used to compute <see cref="NetGexChangePct"/>.</summary>
    [JsonPropertyName("net_gex_prior")]
    public double? NetGexPrior { get; set; }

    /// <summary>Percent change in net GEX vs prior session.</summary>
    [JsonPropertyName("net_gex_change_pct")]
    public double? NetGexChangePct { get; set; }

    /// <summary>VIX level used as a vol-context input to the narrative.</summary>
    [JsonPropertyName("vix")]
    public double? Vix { get; set; }

    /// <summary>Strike where net dealer gamma flips sign.</summary>
    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    /// <summary>Highest call-GEX strike — resistance.</summary>
    [JsonPropertyName("call_wall")]
    public double? CallWall { get; set; }

    /// <summary>Highest put-GEX strike — support.</summary>
    [JsonPropertyName("put_wall")]
    public double? PutWall { get; set; }

    /// <summary><c>"positive_gamma"</c> | <c>"negative_gamma"</c> | <c>"unknown"</c>.</summary>
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    /// <summary>0DTE GEX as a percent of total chain GEX.</summary>
    [JsonPropertyName("zero_dte_pct")]
    public double? ZeroDtePct { get; set; }

    /// <summary>Top OI changes by strike — used to populate the <c>flow</c> sentence.</summary>
    [JsonPropertyName("top_oi_changes")]
    public List<NarrativeOiChange>? TopOiChanges { get; set; }
}

/// <summary>One row of "top OI change" — strike + side + delta + volume.</summary>
public sealed class NarrativeOiChange
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary><c>"C"</c> for call, <c>"P"</c> for put.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Change in open interest vs prior session at this strike/side.</summary>
    [JsonPropertyName("oi_change")]
    public long? OiChange { get; set; }

    /// <summary>Volume traded at this strike/side today.</summary>
    [JsonPropertyName("volume")]
    public long? Volume { get; set; }
}
