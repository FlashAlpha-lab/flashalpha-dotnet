using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/symbols</c> (Free+).
///
/// <para>The list of symbols that have been recently queried and currently have
/// live data. These symbols return analytics with sub-100ms latency; other
/// symbols are computed at first request.</para>
/// </summary>
public sealed class SymbolsResponse : FlashAlphaResponse
{
    /// <summary>Symbols with live data (e.g. <c>["SPY", "QQQ"]</c>).</summary>
    [JsonPropertyName("symbols")]
    public string[]? Symbols { get; set; }

    /// <summary>Length of <see cref="Symbols"/>.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>Operator note about data freshness and plan access.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>ISO-8601 timestamp of the last refresh.</summary>
    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }
}
