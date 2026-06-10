using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/macro/vix-state (MCP get_vix_state) ──────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/macro/vix-state</c> (MCP <c>get_vix_state</c>).
///
/// <para>"Overvixing / undervixing" regime label for the index complex —
/// compares spot VIX against SPX 20-day realized vol. Requires Growth+.</para>
/// </summary>
public sealed class VixStateResponse
{
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("vix")]
    public double? Vix { get; set; }

    /// <summary>SPX 20-day annualised realized vol (%).</summary>
    [JsonPropertyName("spx_rv_20d")]
    public double? SpxRv20d { get; set; }

    /// <summary><c>vix − spx_rv_20d</c> (vol points).</summary>
    [JsonPropertyName("spread")]
    public double? Spread { get; set; }

    /// <summary><c>vix / spx_rv_20d</c>. <c>null</c> when <c>spx_rv_20d == 0</c>.</summary>
    [JsonPropertyName("ratio")]
    public double? Ratio { get; set; }

    /// <summary><c>overvixing</c> (spread ≥ 5) / <c>undervixing</c> (spread ≤ 0) / <c>neutral</c>.</summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("interpretation")]
    public string? Interpretation { get; set; }
}

// ── /v1/universe (MCP get_universe) ──────────────────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/universe</c> (MCP <c>get_universe</c>).
///
/// <para>Curated tier-1 / tier-2 symbol directory — the symbols the screener
/// background loop keeps pre-warmed. Public — no auth required.</para>
/// </summary>
public sealed class UniverseResponse
{
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Total universe size (tier-1 ∪ tier-2, deduplicated).</summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary><c>min(count, limit)</c>.</summary>
    [JsonPropertyName("returned")]
    public int? Returned { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>Echoes the effective sort.</summary>
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }

    [JsonPropertyName("symbols")]
    public List<UniverseSymbol>? Symbols { get; set; }
}

/// <summary>One symbol inside <see cref="UniverseResponse.Symbols"/>.</summary>
public sealed class UniverseSymbol
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    /// <summary>1 = high-traffic loop (fast refresh); 2 = remaining curated symbols.</summary>
    [JsonPropertyName("tier")]
    public int? Tier { get; set; }

    [JsonPropertyName("is_pre_warmed")]
    public bool? IsPreWarmed { get; set; }
}

// ── /v1/screener/fields (MCP get_screener_fields) ────────────────────────────

/// <summary>
/// Typed response for <c>GET /v1/screener/fields</c>.
///
/// <para>Lists every field that can be referenced in a screener query's
/// <c>filters</c>, <c>sort</c>, <c>select</c>, or formula expressions, with its
/// value type. Requires an API key (any authenticated tier).</para>
/// </summary>
public sealed class ScreenerFieldsResponse
{
    /// <summary>Fields returned sorted by <c>name</c>.</summary>
    [JsonPropertyName("fields")]
    public List<ScreenerField>? Fields { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

/// <summary>One queryable screener field inside <see cref="ScreenerFieldsResponse.Fields"/>.</summary>
public sealed class ScreenerField
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>The value type used by the filter/sort engine (e.g. <c>number</c>, <c>string</c>).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
