using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/symbols</c> (Free+).
///
/// <para>The list of symbols that have been recently queried and currently have
/// live data cached at the FlashAlpha gateway. Pre-cached symbols return
/// analytics with sub-100ms latency; on-demand symbols are computed at first
/// request and cached for ~15 seconds.</para>
/// </summary>
public sealed class SymbolsResponse
{
    /// <summary>Symbols with live data cached (e.g. <c>["SPY", "QQQ"]</c>).</summary>
    [JsonPropertyName("symbols")]
    public string[]? Symbols { get; set; }

    /// <summary>Length of <see cref="Symbols"/>.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>Operator note, typically describing the on-demand caching policy.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>ISO-8601 timestamp of the last cache refresh.</summary>
    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }
}
