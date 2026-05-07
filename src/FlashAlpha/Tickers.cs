using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/tickers</c> (Free+).
///
/// <para>Lists every ticker the FlashAlpha catalog covers. The catalog is the
/// universe of symbols that <i>could</i> be queried — the live-cached subset
/// is exposed via <see cref="SymbolsResponse"/> instead.</para>
/// </summary>
public sealed class TickersResponse
{
    /// <summary>All available stock tickers (e.g. <c>["AAPL", "QQQ", "SPY", ...]</c>).</summary>
    [JsonPropertyName("tickers")]
    public string[]? Tickers { get; set; }

    /// <summary>Length of <see cref="Tickers"/>.</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}
