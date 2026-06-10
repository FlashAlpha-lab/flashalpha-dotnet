using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Shared response envelope for every <c>GET /v1/strategies/*</c> endpoint.
///
/// <para>All ten strategy-signal endpoints (flow-anomaly, expiry-positioning,
/// zero-dte, dealer-regime, vol-carry, yield-enhancement, surface-anomaly, skew,
/// term-structure, tail-pricing) return this exact shape. Only <see cref="Regime"/>
/// and <see cref="Metrics"/> vary between strategies — everything else is fixed.</para>
///
/// <para><see cref="Metrics"/> is a raw <see cref="JsonElement"/> key/value bag: the
/// keys differ per strategy (documented per endpoint), but <c>underlying_price</c>
/// is always present.</para>
/// </summary>
public sealed class StrategyDecisionResponse
{
    /// <summary>The strategy that produced the result (e.g. <c>flow_anomaly</c>, <c>vol_carry</c>).</summary>
    [JsonPropertyName("strategy")]
    public string? Strategy { get; set; }

    /// <summary>Resolved, upper-cased underlying symbol.</summary>
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>ISO-8601 UTC timestamp the decision was built.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>One of <c>insufficient_data</c>, <c>avoid</c>, <c>neutral</c>, <c>candidate</c>.</summary>
    [JsonPropertyName("decision")]
    public string? Decision { get; set; }

    /// <summary>0–100 strategy score that drives the decision band.</summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>0–1 input-quality / sample-size weight.</summary>
    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    /// <summary>Strategy-specific regime label. Vocabulary differs per endpoint.</summary>
    [JsonPropertyName("regime")]
    public string? Regime { get; set; }

    /// <summary>Ranked candidate structures. Empty for pure-signal endpoints.</summary>
    [JsonPropertyName("best_structures")]
    public List<StrategyStructure>? BestStructures { get; set; }

    /// <summary>Strategy-specific key/value bag. Keys vary; <c>underlying_price</c> always present.</summary>
    [JsonPropertyName("metrics")]
    public JsonElement? Metrics { get; set; }

    /// <summary>Optional risk callouts (often empty).</summary>
    [JsonPropertyName("risk_flags")]
    public List<StrategyRiskFlag>? RiskFlags { get; set; }

    /// <summary>Human-readable rationale for the decision.</summary>
    [JsonPropertyName("why")]
    public List<string>? Why { get; set; }

    /// <summary>Conditions under which the read should be discarded.</summary>
    [JsonPropertyName("avoid_if")]
    public List<string>? AvoidIf { get; set; }

    /// <summary>Gate on this before trading: <c>score</c> (0–100) and <c>warnings[]</c>.</summary>
    [JsonPropertyName("data_quality")]
    public StrategyDataQuality? DataQuality { get; set; }
}

/// <summary>A ranked tradeable structure inside <see cref="StrategyDecisionResponse.BestStructures"/>.</summary>
public sealed class StrategyStructure
{
    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    /// <summary>Structure name (e.g. <c>short_put_spread</c>, <c>iron_condor</c>).</summary>
    [JsonPropertyName("structure")]
    public string? Structure { get; set; }

    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("legs")]
    public List<StrategyLeg>? Legs { get; set; }

    [JsonPropertyName("credit")]
    public double? Credit { get; set; }

    [JsonPropertyName("debit")]
    public double? Debit { get; set; }

    [JsonPropertyName("max_profit")]
    public double? MaxProfit { get; set; }

    [JsonPropertyName("max_loss")]
    public double? MaxLoss { get; set; }

    [JsonPropertyName("breakevens")]
    public double[]? Breakevens { get; set; }

    [JsonPropertyName("edge_score")]
    public double? EdgeScore { get; set; }

    [JsonPropertyName("liquidity_score")]
    public double? LiquidityScore { get; set; }
}

/// <summary>A single option leg of a <see cref="StrategyStructure"/>.</summary>
public sealed class StrategyLeg
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("delta")]
    public double? Delta { get; set; }

    [JsonPropertyName("premium")]
    public double? Premium { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}

/// <summary>A risk callout inside <see cref="StrategyDecisionResponse.RiskFlags"/>.</summary>
public sealed class StrategyRiskFlag
{
    /// <summary><c>low</c>, <c>medium</c>, or <c>high</c>.</summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>The <c>data_quality</c> gate block of <see cref="StrategyDecisionResponse"/>.</summary>
public sealed class StrategyDataQuality
{
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }
}
