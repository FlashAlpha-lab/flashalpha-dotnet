using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/pricing/kelly</c> (Growth+).
///
/// <para>Kelly criterion optimal position sizing for an options trade. Uses
/// numerical integration over the full lognormal distribution of the
/// underlying to maximize expected log-wealth growth, then halves and quarters
/// the full-Kelly fraction for practical use. All probabilities are
/// real-world (using <see cref="PricingKellyInputs.Mu"/>), not risk-neutral.</para>
///
/// <para>Returns 403 <c>tier_restricted</c> for Free / Basic.</para>
/// </summary>
public sealed class PricingKellyResponse
{
    /// <summary>Echo of the request inputs.</summary>
    [JsonPropertyName("inputs")]
    public PricingKellyInputs? Inputs { get; set; }

    /// <summary>Kelly fractions (full / half / quarter) plus their percent equivalents.</summary>
    [JsonPropertyName("sizing")]
    public PricingKellySizing? Sizing { get; set; }

    /// <summary>Expected payoff/ROI/probability metrics under the real-world measure.</summary>
    [JsonPropertyName("analysis")]
    public PricingKellyAnalysis? Analysis { get; set; }

    /// <summary>Plain-English sizing recommendation. Safe to surface verbatim.</summary>
    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }
}

/// <summary>Echo of the request inputs for <c>/v1/pricing/kelly</c>.</summary>
public sealed class PricingKellyInputs
{
    [JsonPropertyName("spot")]
    public double? Spot { get; set; }

    [JsonPropertyName("strike")]
    public double? Strike { get; set; }

    [JsonPropertyName("dte")]
    public double? Dte { get; set; }

    /// <summary>Annualised volatility as a decimal (e.g. <c>0.18</c> = 18%).</summary>
    [JsonPropertyName("sigma")]
    public double? Sigma { get; set; }

    /// <summary>Premium paid per share (i.e. the option price you're sizing against).</summary>
    [JsonPropertyName("premium")]
    public double? Premium { get; set; }

    /// <summary>Annualised expected return on the underlier as a decimal (e.g. <c>0.12</c> = 12%). Drives the real-world measure.</summary>
    [JsonPropertyName("mu")]
    public double? Mu { get; set; }

    /// <summary><c>"call"</c> or <c>"put"</c>.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Risk-free rate as a decimal (e.g. <c>0.045</c> = 4.5%).</summary>
    [JsonPropertyName("risk_free_rate")]
    public double? RiskFreeRate { get; set; }

    /// <summary>Dividend yield as a decimal (e.g. <c>0.013</c> = 1.3%).</summary>
    [JsonPropertyName("dividend_yield")]
    public double? DividendYield { get; set; }
}

/// <summary>Kelly fractions and their percent equivalents.</summary>
public sealed class PricingKellySizing
{
    /// <summary>Full Kelly — optimal fraction of bankroll to risk (0–1). Bounded to [0, 1] so will never recommend leveraging.</summary>
    [JsonPropertyName("kelly_fraction")]
    public double? KellyFraction { get; set; }

    /// <summary>Half Kelly — typically the recommended sizing in practice (less volatile than full Kelly).</summary>
    [JsonPropertyName("half_kelly")]
    public double? HalfKelly { get; set; }

    /// <summary>Quarter Kelly — very conservative.</summary>
    [JsonPropertyName("quarter_kelly")]
    public double? QuarterKelly { get; set; }

    /// <summary><see cref="KellyFraction"/> as a percent (e.g. <c>7.68</c>).</summary>
    [JsonPropertyName("kelly_pct")]
    public double? KellyPct { get; set; }

    /// <summary><see cref="HalfKelly"/> as a percent.</summary>
    [JsonPropertyName("half_kelly_pct")]
    public double? HalfKellyPct { get; set; }
}

/// <summary>Expected payoff and probability analytics for the trade under the real-world measure.</summary>
public sealed class PricingKellyAnalysis
{
    /// <summary>Expected return on investment as a decimal (e.g. <c>0.16</c> = 16% expected ROI).</summary>
    [JsonPropertyName("expected_roi")]
    public double? ExpectedRoi { get; set; }

    /// <summary><see cref="ExpectedRoi"/> as a percent.</summary>
    [JsonPropertyName("expected_roi_pct")]
    public double? ExpectedRoiPct { get; set; }

    /// <summary>Expected payoff per share at expiry under the real-world distribution.</summary>
    [JsonPropertyName("expected_payoff")]
    public double? ExpectedPayoff { get; set; }

    /// <summary>Probability the option is profitable at expiry (real-world measure).</summary>
    [JsonPropertyName("probability_of_profit")]
    public double? ProbabilityOfProfit { get; set; }

    /// <summary><see cref="ProbabilityOfProfit"/> as a percent.</summary>
    [JsonPropertyName("probability_of_profit_pct")]
    public double? ProbabilityOfProfitPct { get; set; }

    /// <summary>Probability the option is in-the-money at expiry (real-world measure).</summary>
    [JsonPropertyName("probability_itm")]
    public double? ProbabilityItm { get; set; }

    /// <summary><see cref="ProbabilityItm"/> as a percent.</summary>
    [JsonPropertyName("probability_itm_pct")]
    public double? ProbabilityItmPct { get; set; }

    /// <summary>Maximum loss per share (= premium paid for long options).</summary>
    [JsonPropertyName("max_loss")]
    public double? MaxLoss { get; set; }

    /// <summary>Underlier price needed at expiry to break even.</summary>
    [JsonPropertyName("breakeven")]
    public double? Breakeven { get; set; }

    /// <summary>Expected log-growth rate of bankroll at the full-Kelly fraction.</summary>
    [JsonPropertyName("expected_growth_rate")]
    public double? ExpectedGrowthRate { get; set; }
}
