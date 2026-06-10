using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// A single option leg for the pure-math <c>POST /v1/structures/*</c> endpoints.
///
/// <para>For <c>POST /v1/structures/pnl</c> supply <see cref="Action"/>,
/// <see cref="Type"/>, <see cref="Strike"/>, <see cref="Premium"/>, and
/// (optionally) <see cref="Quantity"/>. For <c>POST /v1/structures/greeks</c>
/// supply <see cref="Action"/>, <see cref="Type"/>, <see cref="Strike"/>,
/// <see cref="Expiry"/>, <see cref="ImpliedVol"/>, and (optionally)
/// <see cref="Quantity"/> — each leg carries its own expiry and IV so calendars
/// and diagonals aggregate correctly.</para>
/// </summary>
public sealed class StructureLeg
{
    /// <summary><c>buy</c> (alias <c>long</c>) or <c>sell</c> (alias <c>short</c>).</summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary><c>call</c> (alias <c>c</c>) or <c>put</c> (alias <c>p</c>).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Strike price. Must be &gt; 0.</summary>
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary>Per-contract premium (P&amp;L endpoint only). Must be &gt;= 0.</summary>
    [JsonPropertyName("premium")]
    public double? Premium { get; set; }

    /// <summary>Leg expiry, <c>YYYY-MM-DD</c> (Greeks endpoint only).</summary>
    [JsonPropertyName("expiry")]
    public string? Expiry { get; set; }

    /// <summary>Implied volatility as a decimal, e.g. <c>0.28</c> (Greeks endpoint only). Must be &gt; 0.</summary>
    [JsonPropertyName("impliedVol")]
    public double? ImpliedVol { get; set; }

    /// <summary>Number of contracts. Must be &gt; 0. Defaults to 1 server-side.</summary>
    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}

/// <summary>Request body for <c>POST /v1/structures/pnl</c>.</summary>
public sealed class StructurePnlRequest
{
    /// <summary>One or more legs. Must be non-empty.</summary>
    [JsonPropertyName("legs")]
    public List<StructureLeg>? Legs { get; set; }

    /// <summary>Lower bound of the underlying-price curve. Derived from strikes ±30% when omitted.</summary>
    [JsonPropertyName("minUnderlying")]
    public double? MinUnderlying { get; set; }

    /// <summary>Upper bound of the underlying-price curve. Derived when omitted.</summary>
    [JsonPropertyName("maxUnderlying")]
    public double? MaxUnderlying { get; set; }

    /// <summary>Number of equally-spaced curve sample points (default 81, min 2).</summary>
    [JsonPropertyName("points")]
    public int? Points { get; set; }
}

/// <summary>Typed response for <c>POST /v1/structures/pnl</c>.</summary>
public sealed class StructurePnlResponse
{
    /// <summary>Echo of the request legs.</summary>
    [JsonPropertyName("legs")]
    public List<StructureLeg>? Legs { get; set; }

    /// <summary>At-expiry P&amp;L curve, one <c>{ underlying, pnl }</c> point per sample.</summary>
    [JsonPropertyName("pnl_curve")]
    public List<StructurePnlPoint>? PnlCurve { get; set; }

    /// <summary>Underlying prices where P&amp;L crosses zero (may be empty).</summary>
    [JsonPropertyName("breakevens")]
    public double[]? Breakevens { get; set; }

    /// <summary>Bounded max profit, or <c>null</c> when unbounded on the upside.</summary>
    [JsonPropertyName("max_profit")]
    public double? MaxProfit { get; set; }

    /// <summary>Bounded max loss, or <c>null</c> when unbounded on the downside.</summary>
    [JsonPropertyName("max_loss")]
    public double? MaxLoss { get; set; }
}

/// <summary>A single <c>{ underlying, pnl }</c> point on the at-expiry curve.</summary>
public sealed class StructurePnlPoint
{
    [JsonPropertyName("underlying")]
    public double? Underlying { get; set; }

    [JsonPropertyName("pnl")]
    public double? Pnl { get; set; }
}

/// <summary>Request body for <c>POST /v1/structures/greeks</c>.</summary>
public sealed class StructureGreeksRequest
{
    /// <summary>One or more legs (each with <c>expiry</c> + <c>impliedVol</c>). Must be non-empty.</summary>
    [JsonPropertyName("legs")]
    public List<StructureLeg>? Legs { get; set; }

    /// <summary>Underlying spot price to price against. Must be &gt; 0.</summary>
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    /// <summary>Valuation date <c>YYYY-MM-DD</c> (defaults to today UTC).</summary>
    [JsonPropertyName("today")]
    public string? Today { get; set; }

    /// <summary>Risk-free rate (decimal, default 0.045).</summary>
    [JsonPropertyName("rate")]
    public double? Rate { get; set; }

    /// <summary>Continuous dividend yield (decimal, default 0.013).</summary>
    [JsonPropertyName("dividendYield")]
    public double? DividendYield { get; set; }
}

/// <summary>Typed response for <c>POST /v1/structures/greeks</c>.</summary>
public sealed class StructureGreeksResponse
{
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    /// <summary>The resolved <c>today</c> valuation date.</summary>
    [JsonPropertyName("valuation_date")]
    public string? ValuationDate { get; set; }

    [JsonPropertyName("rate")]
    public double? Rate { get; set; }

    [JsonPropertyName("dividend_yield")]
    public double? DividendYield { get; set; }

    /// <summary>Echo of the request legs.</summary>
    [JsonPropertyName("legs")]
    public List<StructureLeg>? Legs { get; set; }

    /// <summary>Aggregated, quantity-scaled, direction-signed position Greeks.</summary>
    [JsonPropertyName("position_greeks")]
    public StructurePositionGreeks? PositionGreeks { get; set; }
}

/// <summary>Aggregated position Greeks across all legs.</summary>
public sealed class StructurePositionGreeks
{
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

    [JsonPropertyName("vanna")]
    public double? Vanna { get; set; }

    [JsonPropertyName("charm")]
    public double? Charm { get; set; }
}
