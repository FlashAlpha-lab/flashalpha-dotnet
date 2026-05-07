using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/exposure/gex/{symbol}</c> (Free+).
///
/// <para>Strike-by-strike net dealer gamma exposure. Includes <see cref="GammaFlip"/>
/// (the strike where net dealer gamma crosses zero — regime boundary) and
/// the headline <see cref="NetGex"/> with a coarse <see cref="NetGexLabel"/>.</para>
///
/// <para>Sister endpoints DEX / VEX / CHEX share the same response shape but
/// with the corresponding <c>*_dex</c> / <c>*_vex</c> / <c>*_chex</c> fields
/// per strike — see <see cref="DexResponse"/>, <see cref="VexResponse"/>, and
/// <see cref="ChexResponse"/>.</para>
/// </summary>
public sealed class GexResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Strike where net dealer gamma crosses zero. <c>null</c> when no zero crossing exists in the chain.</summary>
    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    /// <summary>Headline net dealer gamma exposure in dollars per 1% spot move.</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    /// <summary>Coarse label — typically <c>"positive"</c> / <c>"negative"</c>.</summary>
    [JsonPropertyName("net_gex_label")]
    public string? NetGexLabel { get; set; }

    /// <summary>Per-strike breakdown. Matches the chain at <c>as_of</c>.</summary>
    [JsonPropertyName("strikes")]
    public List<GexStrikeRow>? Strikes { get; set; }
}

/// <summary>One row of the per-strike GEX table.</summary>
public sealed class GexStrikeRow
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary>Dollar gamma exposure from calls at this strike.</summary>
    [JsonPropertyName("call_gex")]
    public double? CallGex { get; set; }

    /// <summary>Dollar gamma exposure from puts at this strike.</summary>
    [JsonPropertyName("put_gex")]
    public double? PutGex { get; set; }

    /// <summary><c>call_gex + put_gex</c> at this strike.</summary>
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("call_oi")]
    public int? CallOi { get; set; }

    [JsonPropertyName("put_oi")]
    public int? PutOi { get; set; }

    [JsonPropertyName("call_volume")]
    public int? CallVolume { get; set; }

    [JsonPropertyName("put_volume")]
    public int? PutVolume { get; set; }

    /// <summary>Day-over-day change in call OI at this strike.</summary>
    [JsonPropertyName("call_oi_change")]
    public int? CallOiChange { get; set; }

    /// <summary>Day-over-day change in put OI at this strike.</summary>
    [JsonPropertyName("put_oi_change")]
    public int? PutOiChange { get; set; }
}

/// <summary>
/// Typed response model for <c>GET /v1/exposure/dex/{symbol}</c>.
///
/// <para>Strike-by-strike dealer delta exposure (DEX). Same shape as GEX but
/// keyed on delta dollars per 1% spot move.</para>
/// </summary>
public sealed class DexResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Net dealer delta exposure in dollars.</summary>
    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("strikes")]
    public List<DexStrikeRow>? Strikes { get; set; }
}

/// <summary>One row of the per-strike DEX table.</summary>
public sealed class DexStrikeRow
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("call_dex")]
    public double? CallDex { get; set; }

    [JsonPropertyName("put_dex")]
    public double? PutDex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }
}

/// <summary>
/// Typed response model for <c>GET /v1/exposure/vex/{symbol}</c>.
///
/// <para>Strike-by-strike dealer vanna exposure. Includes a textual
/// <see cref="VexInterpretation"/> describing the directional vol-spot
/// linkage at the current regime.</para>
/// </summary>
public sealed class VexResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Net dealer vanna exposure.</summary>
    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    /// <summary>Plain-English explanation of the vanna regime. Safe to surface verbatim.</summary>
    [JsonPropertyName("vex_interpretation")]
    public string? VexInterpretation { get; set; }

    [JsonPropertyName("strikes")]
    public List<VexStrikeRow>? Strikes { get; set; }
}

/// <summary>One row of the per-strike VEX table.</summary>
public sealed class VexStrikeRow
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("call_vex")]
    public double? CallVex { get; set; }

    [JsonPropertyName("put_vex")]
    public double? PutVex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }
}

/// <summary>
/// Typed response model for <c>GET /v1/exposure/chex/{symbol}</c>.
///
/// <para>Strike-by-strike dealer charm exposure (delta decay per unit time).
/// Includes a textual <see cref="ChexInterpretation"/>.</para>
/// </summary>
public sealed class ChexResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>Net dealer charm exposure.</summary>
    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    /// <summary>Plain-English explanation of the charm regime. Safe to surface verbatim.</summary>
    [JsonPropertyName("chex_interpretation")]
    public string? ChexInterpretation { get; set; }

    [JsonPropertyName("strikes")]
    public List<ChexStrikeRow>? Strikes { get; set; }
}

/// <summary>One row of the per-strike CHEX table.</summary>
public sealed class ChexStrikeRow
{
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("call_chex")]
    public double? CallChex { get; set; }

    [JsonPropertyName("put_chex")]
    public double? PutChex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }
}
