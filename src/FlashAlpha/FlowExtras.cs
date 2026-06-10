using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/flow/options/{symbol}/dealer-premium (MCP get_dealer_premium) ────────

/// <summary>
/// Typed response for <c>GET /v1/flow/options/{symbol}/dealer-premium</c>
/// (MCP <c>get_dealer_premium</c>).
///
/// <para>Full-tape Net Dealer Premium roll-up over a configurable window. Sums
/// each side of the customer-flow tape weighted by VWAP per minute bucket.
/// Requires Alpha+.</para>
/// </summary>
public sealed class FlowDealerPremiumResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("window_minutes")]
    public int? WindowMinutes { get; set; }

    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary>Dealer is the BUYER when customer hits the bid.</summary>
    [JsonPropertyName("dealer_buy_premium")]
    public double? DealerBuyPremium { get; set; }

    /// <summary>Dealer is the WRITER when customer lifts the ask.</summary>
    [JsonPropertyName("dealer_write_premium")]
    public double? DealerWritePremium { get; set; }

    /// <summary><c>dealer_buy_premium − dealer_write_premium</c>. Positive ⇒ dealers net long premium.</summary>
    [JsonPropertyName("net_dealer_premium")]
    public double? NetDealerPremium { get; set; }

    [JsonPropertyName("total_premium")]
    public double? TotalPremium { get; set; }

    [JsonPropertyName("trade_count")]
    public long? TradeCount { get; set; }

    [JsonPropertyName("bucket_count")]
    public int? BucketCount { get; set; }
}

// ── /v1/flow/stocks/{symbol}/bars (MCP n/a) ──────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/flow/stocks/{symbol}/bars</c>.
///
/// <para>Multi-resolution OHLCV+flow bars derived from the live trade tape,
/// returned oldest-first for chart consumers. Requires Alpha+.</para>
/// </summary>
public sealed class FlowStockBarsResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("minutes")]
    public int? Minutes { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>Timestamp of the oldest 1-second bucket in the ring (UTC), or <c>null</c> when no history exists.</summary>
    [JsonPropertyName("dataStartUtc")]
    public string? DataStartUtc { get; set; }

    /// <summary>Oldest-first bars.</summary>
    [JsonPropertyName("bars")]
    public List<FlowStockBar>? Bars { get; set; }
}

/// <summary>One OHLCV+flow bar inside <see cref="FlowStockBarsResponse.Bars"/>.</summary>
public sealed class FlowStockBar
{
    /// <summary>Bar start (UTC).</summary>
    [JsonPropertyName("ts")]
    public string? Ts { get; set; }

    /// <summary><c>true</c> once the bar is final and won't accept further updates.</summary>
    [JsonPropertyName("closed")]
    public bool? Closed { get; set; }

    [JsonPropertyName("open")]
    public double? Open { get; set; }

    [JsonPropertyName("high")]
    public double? High { get; set; }

    [JsonPropertyName("low")]
    public double? Low { get; set; }

    [JsonPropertyName("close")]
    public double? Close { get; set; }

    [JsonPropertyName("vwap")]
    public double? Vwap { get; set; }

    [JsonPropertyName("buyVolume")]
    public long? BuyVolume { get; set; }

    [JsonPropertyName("sellVolume")]
    public long? SellVolume { get; set; }

    [JsonPropertyName("midVolume")]
    public long? MidVolume { get; set; }

    [JsonPropertyName("netVolume")]
    public long? NetVolume { get; set; }

    [JsonPropertyName("tradeCount")]
    public long? TradeCount { get; set; }

    [JsonPropertyName("biggestTrade")]
    public double? BiggestTrade { get; set; }
}
