using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

// ── /v1/volatility/realized/{symbol} (MCP get_realized_vol) ──────────────────

/// <summary>
/// Typed response for <c>GET /v1/volatility/realized/{symbol}</c>
/// (MCP <c>get_realized_vol</c>).
///
/// <para>Range-based realized (historical) volatility estimators over 10/20/30-day
/// windows: close-to-close, Parkinson, Garman-Klass, Rogers-Satchell, and
/// Yang-Zhang. Each estimator exposes annualized realized vol (percent) for the
/// three windows. Requires Alpha+.</para>
/// </summary>
public sealed class RealizedVolatilityResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("estimators")]
    public RealizedVolEstimators? Estimators { get; set; }
}

/// <summary>The five range-based estimators inside <see cref="RealizedVolatilityResponse.Estimators"/>.</summary>
public sealed class RealizedVolEstimators
{
    /// <summary>Standard close-to-close log-return estimator.</summary>
    [JsonPropertyName("close_to_close")]
    public RealizedVolWindows? CloseToClose { get; set; }

    /// <summary>Parkinson high-low range estimator.</summary>
    [JsonPropertyName("parkinson")]
    public RealizedVolWindows? Parkinson { get; set; }

    /// <summary>Garman-Klass OHLC estimator.</summary>
    [JsonPropertyName("garman_klass")]
    public RealizedVolWindows? GarmanKlass { get; set; }

    /// <summary>Rogers-Satchell drift-independent estimator.</summary>
    [JsonPropertyName("rogers_satchell")]
    public RealizedVolWindows? RogersSatchell { get; set; }

    /// <summary>Yang-Zhang overnight-jump-aware estimator.</summary>
    [JsonPropertyName("yang_zhang")]
    public RealizedVolWindows? YangZhang { get; set; }
}

/// <summary>
/// Annualized realized vol (percent) across the 10/20/30-day windows. Reused for
/// every estimator in <see cref="RealizedVolEstimators"/>. Each window is
/// nullable when there is insufficient history.
/// </summary>
public sealed class RealizedVolWindows
{
    /// <summary>Annualized realized vol (percent) over the trailing 10 days.</summary>
    [JsonPropertyName("rv10")]
    public double? Rv10 { get; set; }

    /// <summary>Annualized realized vol (percent) over the trailing 20 days.</summary>
    [JsonPropertyName("rv20")]
    public double? Rv20 { get; set; }

    /// <summary>Annualized realized vol (percent) over the trailing 30 days.</summary>
    [JsonPropertyName("rv30")]
    public double? Rv30 { get; set; }
}

// ── /v1/volatility/forecast/{symbol} (MCP get_volatility_forecast) ───────────

/// <summary>
/// Typed response for <c>GET /v1/volatility/forecast/{symbol}</c>
/// (MCP <c>get_volatility_forecast</c>).
///
/// <para>Conditional volatility forecasts: EWMA (RiskMetrics λ=0.94), HAR-RV
/// (daily/weekly/monthly components), and a GARCH(1,1) MLE fit with a multi-horizon
/// forecast term structure. All vols are annualized percent. Requires Alpha+.</para>
/// </summary>
public sealed class VolatilityForecastResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("ewma")]
    public VolatilityForecastEwma? Ewma { get; set; }

    [JsonPropertyName("har_rv")]
    public VolatilityForecastHarRv? HarRv { get; set; }

    [JsonPropertyName("garch")]
    public VolatilityForecastGarch? Garch { get; set; }
}

/// <summary>EWMA leg of <see cref="VolatilityForecastResponse"/>.</summary>
public sealed class VolatilityForecastEwma
{
    /// <summary>Decay factor (RiskMetrics default 0.94).</summary>
    [JsonPropertyName("lambda")]
    public double? Lambda { get; set; }

    [JsonPropertyName("vol_annualized")]
    public double? VolAnnualized { get; set; }

    [JsonPropertyName("next_day_forecast")]
    public double? NextDayForecast { get; set; }
}

/// <summary>HAR-RV leg of <see cref="VolatilityForecastResponse"/>.</summary>
public sealed class VolatilityForecastHarRv
{
    [JsonPropertyName("vol_annualized")]
    public double? VolAnnualized { get; set; }

    [JsonPropertyName("components")]
    public VolatilityForecastHarComponents? Components { get; set; }

    [JsonPropertyName("next_day_forecast")]
    public double? NextDayForecast { get; set; }
}

/// <summary>Daily/weekly/monthly HAR-RV components inside <see cref="VolatilityForecastHarRv.Components"/>.</summary>
public sealed class VolatilityForecastHarComponents
{
    [JsonPropertyName("daily")]
    public double? Daily { get; set; }

    [JsonPropertyName("weekly")]
    public double? Weekly { get; set; }

    [JsonPropertyName("monthly")]
    public double? Monthly { get; set; }
}

/// <summary>GARCH(1,1) leg of <see cref="VolatilityForecastResponse"/>.</summary>
public sealed class VolatilityForecastGarch
{
    /// <summary>Model label, e.g. <c>garch_1_1</c>.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>Innovation distribution: <c>student_t</c> or <c>gaussian</c>.</summary>
    [JsonPropertyName("distribution")]
    public string? Distribution { get; set; }

    [JsonPropertyName("params")]
    public VolatilityForecastGarchParams? Params { get; set; }

    /// <summary><c>alpha + beta</c>; closer to 1 ⇒ more persistent.</summary>
    [JsonPropertyName("persistence")]
    public double? Persistence { get; set; }

    /// <summary>Unconditional (long-run) vol. <c>null</c> for a non-stationary fit.</summary>
    [JsonPropertyName("long_run_vol_annualized")]
    public double? LongRunVolAnnualized { get; set; }

    [JsonPropertyName("half_life_days")]
    public double? HalfLifeDays { get; set; }

    [JsonPropertyName("converged")]
    public bool? Converged { get; set; }

    /// <summary>Forecast term structure. <c>null</c> when the fit did not converge.</summary>
    [JsonPropertyName("forecast")]
    public List<VolatilityForecastGarchPoint>? Forecast { get; set; }
}

/// <summary>Fitted GARCH(1,1) parameters inside <see cref="VolatilityForecastGarch.Params"/>.</summary>
public sealed class VolatilityForecastGarchParams
{
    [JsonPropertyName("omega")]
    public double? Omega { get; set; }

    [JsonPropertyName("alpha")]
    public double? Alpha { get; set; }

    [JsonPropertyName("beta")]
    public double? Beta { get; set; }

    /// <summary>Student-t degrees of freedom. Present (non-null) only for <c>student_t</c>.</summary>
    [JsonPropertyName("dof")]
    public double? Dof { get; set; }
}

/// <summary>One horizon point in <see cref="VolatilityForecastGarch.Forecast"/>.</summary>
public sealed class VolatilityForecastGarchPoint
{
    [JsonPropertyName("horizon_days")]
    public int? HorizonDays { get; set; }

    [JsonPropertyName("vol_annualized")]
    public double? VolAnnualized { get; set; }
}
