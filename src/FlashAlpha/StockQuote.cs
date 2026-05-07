using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /stockquote/{ticker}</c> (Free+, live only).
///
/// <para>Bid/ask/mid plus last-trade price and timestamp for the underlier.
/// The thinnest live-quote endpoint in the FlashAlpha API — useful as a
/// liveness probe and for displaying spot alongside derived analytics.</para>
///
/// <para><b>Field-naming caveat:</b> <see cref="LastPrice"/> and
/// <see cref="LastUpdate"/> are camelCase on the wire (<c>lastPrice</c>,
/// <c>lastUpdate</c>) — they were preserved from the upstream feed and are
/// NOT renamed to snake_case at the FlashAlpha gateway.</para>
///
/// <para>Historical stock quotes have a separate endpoint
/// (<c>/historical/stockquote/{ticker}</c>) — this model is live-only.</para>
/// </summary>
public sealed class StockQuoteResponse
{
    [JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    [JsonPropertyName("bid")]
    public double? Bid { get; set; }

    [JsonPropertyName("ask")]
    public double? Ask { get; set; }

    /// <summary>Mid = (bid + ask) / 2.</summary>
    [JsonPropertyName("mid")]
    public double? Mid { get; set; }

    /// <summary>Last trade price. Wire field is camelCase (<c>lastPrice</c>).</summary>
    [JsonPropertyName("lastPrice")]
    public double? LastPrice { get; set; }

    /// <summary>Last quote update timestamp. Wire field is camelCase (<c>lastUpdate</c>).</summary>
    [JsonPropertyName("lastUpdate")]
    public string? LastUpdate { get; set; }
}
