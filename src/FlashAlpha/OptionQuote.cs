using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /optionquote/{ticker}</c> (Growth+, live only).
///
/// <para>One option quote row with greeks (first- and second-order) plus
/// optional SVI-fitted vol. When the request specifies all three filters
/// (<c>expiry</c> + <c>strike</c> + <c>type</c>) the API returns a single
/// object of this shape; otherwise it returns an array of these (deserialise
/// to <c>List&lt;OptionQuote&gt;</c>).</para>
///
/// <para><b>Field-naming caveat:</b> several wire fields are camelCase rather
/// than snake_case (<c>lastUpdate</c>, <c>bidSize</c>, <c>askSize</c>) — these
/// are the ones the FlashAlpha gateway preserves from the upstream feed. The
/// JSON property attributes below capture the exact wire shape.</para>
///
/// <para>Historical option quotes have a separate endpoint
/// (<c>/historical/optionquote/{ticker}</c>) and a separate response shape —
/// this model is live-only.</para>
/// </summary>
public sealed class OptionQuote
{
    /// <summary><c>"call"</c> or <c>"put"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Expiration date (<c>yyyy-MM-dd</c>).</summary>
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("bid")]
    public double? Bid { get; set; }

    [JsonPropertyName("ask")]
    public double? Ask { get; set; }

    /// <summary>Mid = (bid + ask) / 2.</summary>
    [JsonPropertyName("mid")]
    public double? Mid { get; set; }

    /// <summary>Bid size in contracts. Wire field is camelCase.</summary>
    [JsonPropertyName("bidSize")]
    public int? BidSize { get; set; }

    /// <summary>Ask size in contracts. Wire field is camelCase.</summary>
    [JsonPropertyName("askSize")]
    public int? AskSize { get; set; }

    /// <summary>Last quote update timestamp. Wire field is camelCase (<c>lastUpdate</c>) — NOT <c>last_update</c>.</summary>
    [JsonPropertyName("lastUpdate")]
    public string? LastUpdate { get; set; }

    /// <summary>Underlying mid price. Present on unfiltered (array) responses; nullable on filtered single-object responses.</summary>
    [JsonPropertyName("underlying")]
    public double? Underlying { get; set; }

    /// <summary>Implied volatility from the mid price (annualised %, e.g. 18.5 = 18.5%).</summary>
    [JsonPropertyName("implied_vol")]
    public double? ImpliedVol { get; set; }

    /// <summary>IV inverted from the bid price.</summary>
    [JsonPropertyName("iv_bid")]
    public double? IvBid { get; set; }

    /// <summary>IV inverted from the ask price.</summary>
    [JsonPropertyName("iv_ask")]
    public double? IvAsk { get; set; }

    [JsonPropertyName("delta")]
    public double? Delta { get; set; }

    [JsonPropertyName("gamma")]
    public double? Gamma { get; set; }

    [JsonPropertyName("theta")]
    public double? Theta { get; set; }

    [JsonPropertyName("vega")]
    public double? Vega { get; set; }

    [JsonPropertyName("rho")]
    public double? Rho { get; set; }

    /// <summary>∂²V/∂S∂σ — sensitivity of delta to vol changes.</summary>
    [JsonPropertyName("vanna")]
    public double? Vanna { get; set; }

    /// <summary>∂²V/∂S∂t — sensitivity of delta to time decay.</summary>
    [JsonPropertyName("charm")]
    public double? Charm { get; set; }

    /// <summary>SVI-fitted vol at this strike/expiry. <c>null</c> when SVI is gated by tier.</summary>
    [JsonPropertyName("svi_vol")]
    public double? SviVol { get; set; }

    /// <summary>
    /// When SVI vol is gated, this carries the tier-gating reason
    /// (e.g. <c>"requires_alpha_tier"</c>). <c>null</c> when SVI is delivered.
    /// </summary>
    [JsonPropertyName("svi_vol_gated")]
    public string? SviVolGated { get; set; }

    [JsonPropertyName("open_interest")]
    public int? OpenInterest { get; set; }

    [JsonPropertyName("volume")]
    public int? Volume { get; set; }
}
