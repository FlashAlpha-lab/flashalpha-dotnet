using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/exposure/levels/{symbol}</c> (Free).
///
/// <para>Distilled set of "where do dealer flows pin price" levels —
/// gamma flip, max-positive / max-negative gamma strikes, call/put walls,
/// highest-OI strike, and the 0DTE magnet. Use this when you want a
/// minimal, structured key-levels block (e.g. drawing horizontal lines on
/// a chart, or feeding price-target context into an LLM) without the
/// full GEX-by-strike payload.</para>
/// </summary>
public sealed class ExposureLevelsResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    /// <summary>UTC timestamp this snapshot was computed for.</summary>
    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>The levels block. See <see cref="ExposureLevels"/>.</summary>
    [JsonPropertyName("levels")]
    public ExposureLevels? Levels { get; set; }
}

/// <summary>
/// Distilled key levels derived from dealer-options exposure.
///
/// <para><b>Trading-context cheat sheet:</b>
/// <list type="bullet">
///   <item><see cref="GammaFlip"/>: Spot ABOVE = positive-gamma regime
///     (vol-suppressing dealer hedging); spot BELOW = negative-gamma
///     regime (vol-amplifying).</item>
///   <item><see cref="CallWall"/>: highest call-GEX strike — acts as
///     resistance / dealer-sell zone.</item>
///   <item><see cref="PutWall"/>: highest put-GEX strike — acts as
///     support / dealer-buy zone.</item>
///   <item><see cref="ZeroDteMagnet"/>: 0DTE strike with the highest GEX
///     — acts as an intraday price magnet on expiry day.</item>
/// </list></para>
/// </summary>
public sealed class ExposureLevels
{
    /// <summary>Strike where net dealer gamma flips sign. Spot ABOVE = positive gamma; spot BELOW = negative gamma.</summary>
    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    /// <summary>Strike with the most positive net dealer gamma — strongest pinning above this level.</summary>
    [JsonPropertyName("max_positive_gamma")]
    public double? MaxPositiveGamma { get; set; }

    /// <summary>Strike with the most negative net dealer gamma — vol-amplifying zone.</summary>
    [JsonPropertyName("max_negative_gamma")]
    public double? MaxNegativeGamma { get; set; }

    /// <summary>Highest call-GEX strike — acts as resistance / dealer-sell zone.</summary>
    [JsonPropertyName("call_wall")]
    public double? CallWall { get; set; }

    /// <summary>Highest put-GEX strike — acts as support / dealer-buy zone.</summary>
    [JsonPropertyName("put_wall")]
    public double? PutWall { get; set; }

    /// <summary>Strike with the highest aggregate OI (calls + puts).</summary>
    [JsonPropertyName("highest_oi_strike")]
    public double? HighestOiStrike { get; set; }

    /// <summary>0DTE strike with the highest GEX — acts as an intraday price magnet on expiry day.</summary>
    [JsonPropertyName("zero_dte_magnet")]
    public double? ZeroDteMagnet { get; set; }
}
