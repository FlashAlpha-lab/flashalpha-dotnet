using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>POST /v1/screener</c> (Free+).
///
/// <para>The screener envelope: a strongly-typed <see cref="Meta"/> block plus
/// a flexible <see cref="Data"/> array. Each row in <see cref="Data"/> is a
/// <see cref="JsonElement"/> because the row's field set is determined by the
/// <c>select</c> parameter on the request — there's no fixed schema.</para>
///
/// <para>Common fields on rows when no <c>select</c> is provided (or when
/// <c>select: ["*"]</c> is used) include <c>symbol</c>, <c>price</c>,
/// <c>regime</c>, <c>net_gex</c>, <c>atm_iv</c>, <c>vrp_20d</c>, and
/// <c>harvest_score</c> — but the available column set varies by tier and
/// by what fields the universe supports. Use
/// <see cref="JsonElement.TryGetProperty(string, out JsonElement)"/> on row
/// elements rather than assuming a fixed shape.</para>
///
/// <para>The unfiltered <see cref="FlashAlphaClient.ScreenerAsync(ScreenerRequest, System.Threading.CancellationToken)"/>
/// remains available and returns a raw <see cref="JsonElement"/> for callers
/// that need full JSON access.</para>
/// </summary>
public sealed class ScreenerResponse
{
    /// <summary>Strongly-typed envelope metadata: counts, universe size, tier, as-of.</summary>
    [JsonPropertyName("meta")]
    public ScreenerMeta? Meta { get; set; }

    /// <summary>
    /// Result rows. Field set per row depends on the <c>select</c> parameter
    /// on the request — accessed via <see cref="JsonElement"/> rather than a
    /// strongly-typed POCO. Call <c>TryGetProperty</c> on each element with
    /// the field names you requested.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement[]? Data { get; set; }
}

/// <summary>
/// Screener metadata envelope — counts, universe stats, tier, and as-of.
/// </summary>
public sealed class ScreenerMeta
{
    /// <summary>Total rows matched by the filter (before <c>limit</c> / <c>offset</c>).</summary>
    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }

    /// <summary>Rows actually returned in <see cref="ScreenerResponse.Data"/>.</summary>
    [JsonPropertyName("returned_count")]
    public int? ReturnedCount { get; set; }

    /// <summary>Size of the active screener universe (Growth ~10, Alpha ~250).</summary>
    [JsonPropertyName("universe_size")]
    public int? UniverseSize { get; set; }

    /// <summary>Pagination offset echoed from the request.</summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    /// <summary>Row cap echoed from the request.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>Caller's tier label — typically <c>"growth"</c> or <c>"alpha"</c>.</summary>
    [JsonPropertyName("tier")]
    public string? Tier { get; set; }

    /// <summary>ISO-8601 timestamp of the underlying snapshot the screener queried.</summary>
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }
}
