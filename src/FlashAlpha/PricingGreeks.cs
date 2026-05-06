using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/pricing/greeks</c> (Free).
///
/// <para>Pure Black-Scholes calculator — given (spot, strike, dte, sigma,
/// type, r, q), return the theoretical price plus a complete first-,
/// second-, and third-order greek set. Stateless and symbol-free; useful
/// for what-if analysis, position sizing, scenario tables, and any
/// LLM-driven workflow that needs analytic greeks for an arbitrary
/// hypothetical option.</para>
///
/// <para>Pricing endpoints are LIVE-ONLY by design — there is no historical
/// counterpart because the calculation is symbol-agnostic and depends only
/// on the inputs. Pass historical (spot, sigma) values to back-date the
/// calc.</para>
/// </summary>
public sealed class PricingGreeksResponse
{
    /// <summary>Echo of the input parameters (spot, strike, dte, sigma, type, risk_free_rate, dividend_yield).</summary>
    [JsonPropertyName("inputs")]
    public PricingGreeksInputs? Inputs { get; set; }

    /// <summary>Black-Scholes theoretical option price in dollars.</summary>
    [JsonPropertyName("theoretical_price")]
    public double? TheoreticalPrice { get; set; }

    /// <summary>First-order greeks (delta, gamma, theta, vega, rho). See <see cref="PricingFirstOrder"/>.</summary>
    [JsonPropertyName("first_order")]
    public PricingFirstOrder? FirstOrder { get; set; }

    /// <summary>Second-order greeks (vanna, charm, vomma, dual delta).</summary>
    [JsonPropertyName("second_order")]
    public PricingSecondOrder? SecondOrder { get; set; }

    /// <summary>Third-order greeks (speed, zomma, color, ultima).</summary>
    [JsonPropertyName("third_order")]
    public PricingThirdOrder? ThirdOrder { get; set; }

    /// <summary>Additional non-canonical greeks (lambda, veta).</summary>
    [JsonPropertyName("additional")]
    public PricingAdditional? Additional { get; set; }
}

/// <summary>Echo of the input parameters used to compute the greeks.</summary>
public sealed class PricingGreeksInputs
{
    /// <summary>Spot price of the underlying.</summary>
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    /// <summary>Strike price.</summary>
    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    /// <summary>Days to expiry.</summary>
    [JsonPropertyName("dte")]
    public double? Dte { get; set; }

    /// <summary>Implied volatility as a decimal (e.g. <c>0.18</c> = 18%).</summary>
    [JsonPropertyName("sigma")]
    public double? Sigma { get; set; }

    /// <summary><c>"call"</c> or <c>"put"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Risk-free rate as a decimal (e.g. <c>0.045</c> = 4.5%).</summary>
    [JsonPropertyName("risk_free_rate")]
    public double? RiskFreeRate { get; set; }

    /// <summary>Continuous dividend yield as a decimal (e.g. <c>0.013</c> = 1.3%).</summary>
    [JsonPropertyName("dividend_yield")]
    public double? DividendYield { get; set; }
}

/// <summary>
/// First-order greeks — first partial derivatives of the option value.
///
/// <para><c>delta = ∂V/∂S</c> · <c>gamma = ∂²V/∂S²</c> · <c>theta = ∂V/∂t</c>
/// · <c>vega = ∂V/∂σ</c> · <c>rho = ∂V/∂r</c>. Note gamma is technically
/// second-order in spot but is grouped here per market convention.</para>
/// </summary>
public sealed class PricingFirstOrder
{
    /// <summary>∂V/∂S — change in option value per $1 change in spot.</summary>
    [JsonPropertyName("delta")]
    public double? Delta { get; set; }

    /// <summary>∂²V/∂S² — change in delta per $1 change in spot.</summary>
    [JsonPropertyName("gamma")]
    public double? Gamma { get; set; }

    /// <summary>∂V/∂t — change in option value per day (in $/day).</summary>
    [JsonPropertyName("theta")]
    public double? Theta { get; set; }

    /// <summary>∂V/∂σ — change in option value per 1 vol-point (in $/vol-pt).</summary>
    [JsonPropertyName("vega")]
    public double? Vega { get; set; }

    /// <summary>∂V/∂r — change in option value per 1% change in the risk-free rate.</summary>
    [JsonPropertyName("rho")]
    public double? Rho { get; set; }
}

/// <summary>
/// Second-order greeks — second partial derivatives, mostly cross-effects.
///
/// <para><c>vanna = ∂²V/(∂S·∂σ) = ∂delta/∂σ</c> · <c>charm = ∂²V/(∂S·∂t) =
/// ∂delta/∂t</c> · <c>vomma = ∂²V/∂σ²</c> · <c>dual_delta = ∂V/∂K</c>.</para>
/// </summary>
public sealed class PricingSecondOrder
{
    /// <summary>∂delta/∂σ — sensitivity of delta to vol changes (cross-greek).</summary>
    [JsonPropertyName("vanna")]
    public double? Vanna { get; set; }

    /// <summary>∂delta/∂t — delta-decay through time.</summary>
    [JsonPropertyName("charm")]
    public double? Charm { get; set; }

    /// <summary>∂²V/∂σ² — convexity of the vol exposure (a.k.a. vega convexity / volga).</summary>
    [JsonPropertyName("vomma")]
    public double? Vomma { get; set; }

    /// <summary>∂V/∂K — sensitivity to strike (used in option-on-option pricing).</summary>
    [JsonPropertyName("dual_delta")]
    public double? DualDelta { get; set; }
}

/// <summary>
/// Third-order greeks — third partials for advanced risk modelling.
///
/// <para><c>speed = ∂gamma/∂S</c> · <c>zomma = ∂gamma/∂σ</c> ·
/// <c>color = ∂gamma/∂t</c> · <c>ultima = ∂vomma/∂σ</c>.</para>
/// </summary>
public sealed class PricingThirdOrder
{
    /// <summary>∂gamma/∂S — rate of change of gamma w.r.t. spot.</summary>
    [JsonPropertyName("speed")]
    public double? Speed { get; set; }

    /// <summary>∂gamma/∂σ — rate of change of gamma w.r.t. vol.</summary>
    [JsonPropertyName("zomma")]
    public double? Zomma { get; set; }

    /// <summary>∂gamma/∂t — rate of change of gamma w.r.t. time (gamma decay).</summary>
    [JsonPropertyName("color")]
    public double? Color { get; set; }

    /// <summary>∂vomma/∂σ — third-order vol sensitivity.</summary>
    [JsonPropertyName("ultima")]
    public double? Ultima { get; set; }
}

/// <summary>
/// Additional, non-canonical greeks.
///
/// <para><c>lambda</c> (a.k.a. omega) is the percent change in option
/// value per 1% change in spot — equivalent to <c>delta * spot /
/// theoretical_price</c>. It is <b>null</b> when
/// <see cref="PricingGreeksResponse.TheoreticalPrice"/> ≤ 0 (the division
/// is undefined).</para>
///
/// <para><c>veta</c> = <c>∂vega/∂t</c> — the rate at which vega decays
/// through time.</para>
/// </summary>
public sealed class PricingAdditional
{
    /// <summary>
    /// <c>delta * spot / theoretical_price</c> — % change in option value per
    /// 1% change in spot. <c>null</c> when <c>theoretical_price ≤ 0</c>.
    /// </summary>
    [JsonPropertyName("lambda")]
    public double? Lambda { get; set; }

    /// <summary>∂vega/∂t — rate at which vega decays through time.</summary>
    [JsonPropertyName("veta")]
    public double? Veta { get; set; }
}
