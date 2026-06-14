using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlashAlpha;

/// <summary>
/// Second partial of <see cref="FlashAlphaClient"/> covering the endpoint families
/// added for API parity: surface/SVI, the extended exposure suite (sheet,
/// term-structure, basket, OI-diff), liquidity, the volatility analytics suite
/// (skew-term, spot-vol correlation, dispersion, expected-move, VRP history),
/// macro/universe, dealer-premium + stock bars on the flow tier, the intraday
/// 0DTE flow family (snapshot/series/hedge-flow/heatmap/strike-flow), all ten
/// strategy-signal endpoints (sharing <see cref="StrategyDecisionResponse"/>),
/// the earnings analytics suite, the two pure-math structure POST endpoints, and
/// the screener-fields catalogue.
/// </summary>
public sealed partial class FlashAlphaClient
{
    private static string Inv(double v) => v.ToString(CultureInfo.InvariantCulture);

    // ── Market Data: SVI surface ──────────────────────────────────────────────

    /// <summary>Calibrated SVI surface parameters per expiry slice. Requires Alpha+.</summary>
    public Task<JsonElement> SurfaceSviAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/surface/svi/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="SurfaceSviAsync"/>.</summary>
    public async Task<SurfaceSviResponse?> SurfaceSviTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await SurfaceSviAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<SurfaceSviResponse>(PostSerializerOptions);
    }

    // ── Exposure Analytics (extended) ─────────────────────────────────────────

    /// <summary>Dealer exposure sheet: per-strike GEX/DEX/VEX/CHEX with totals, levels, and peaks. Requires Growth+.</summary>
    public Task<JsonElement> ExposureSheetAsync(string symbol, string? expiration = null, int? minOi = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        if (minOi.HasValue) p["min_oi"] = minOi.Value.ToString();
        return GetAsync($"/v1/exposure/sheet/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="ExposureSheetAsync"/>.</summary>
    public async Task<ExposureSheetResponse?> ExposureSheetTypedAsync(string symbol, string? expiration = null, int? minOi = null, CancellationToken ct = default)
    {
        var e = await ExposureSheetAsync(symbol, expiration, minOi, ct).ConfigureAwait(false);
        return e.Deserialize<ExposureSheetResponse>(PostSerializerOptions);
    }

    /// <summary>Net GEX/DEX/VEX/CHEX broken out by expiry bucket (term structure of exposure). Requires Growth+.</summary>
    public Task<JsonElement> ExposureTermStructureAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/term-structure/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="ExposureTermStructureAsync"/>.</summary>
    public async Task<ExposureTermStructureResponse?> ExposureTermStructureTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await ExposureTermStructureAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<ExposureTermStructureResponse>(PostSerializerOptions);
    }

    /// <summary>
    /// Weighted cross-symbol aggregate of GEX/DEX/VEX/CHEX across up to 50 symbols. Requires Growth+.
    /// </summary>
    /// <param name="symbols">Symbols to aggregate (comma-joined; max 50, de-duped).</param>
    /// <param name="weights">Optional weights, same length as <paramref name="symbols"/>; equal-weighted when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> ExposureBasketAsync(IEnumerable<string> symbols, IEnumerable<double>? weights = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?> { ["symbols"] = string.Join(",", symbols) };
        if (weights is not null)
        {
            var ws = new List<string>();
            foreach (var w in weights) ws.Add(Inv(w));
            p["weights"] = string.Join(",", ws);
        }
        return GetAsync("/v1/exposure/basket", p, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="ExposureBasketAsync"/>.</summary>
    public async Task<ExposureBasketResponse?> ExposureBasketTypedAsync(IEnumerable<string> symbols, IEnumerable<double>? weights = null, CancellationToken ct = default)
    {
        var e = await ExposureBasketAsync(symbols, weights, ct).ConfigureAwait(false);
        return e.Deserialize<ExposureBasketResponse>(PostSerializerOptions);
    }

    /// <summary>Largest open-interest changes (top-N strikes added/removed since the prior snapshot). Requires Growth+.</summary>
    public Task<JsonElement> ExposureOiDiffAsync(string symbol, int? topN = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (topN.HasValue) p["topN"] = topN.Value.ToString();
        return GetAsync($"/v1/exposure/oi-diff/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="ExposureOiDiffAsync"/>.</summary>
    public async Task<ExposureOiDiffResponse?> ExposureOiDiffTypedAsync(string symbol, int? topN = null, CancellationToken ct = default)
    {
        var e = await ExposureOiDiffAsync(symbol, topN, ct).ConfigureAwait(false);
        return e.Deserialize<ExposureOiDiffResponse>(PostSerializerOptions);
    }

    // ── Volatility (additional) ───────────────────────────────────────────────

    /// <summary>Per-expiry execution / liquidity score: ATM spread %, OI-weighted spread %, depth. Requires Growth+.</summary>
    public Task<JsonElement> LiquidityAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/liquidity/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="LiquidityAsync"/>.</summary>
    public async Task<LiquidityResponse?> LiquidityTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await LiquidityAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<LiquidityResponse>(PostSerializerOptions);
    }

    /// <summary>Skew and term-structure of implied volatility (25-delta risk reversals, ATM term curve). Requires Growth+.</summary>
    public Task<JsonElement> SkewTermAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/volatility/skew-term/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="SkewTermAsync"/>.</summary>
    public async Task<SkewTermResponse?> SkewTermTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await SkewTermAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<SkewTermResponse>(PostSerializerOptions);
    }

    /// <summary>Spot–vol correlation regime (leverage effect): rolling correlation of returns and IV changes. Requires Growth+.</summary>
    public Task<JsonElement> SpotVolCorrelationAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/volatility/spot-vol-correlation/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="SpotVolCorrelationAsync"/>.</summary>
    public async Task<SpotVolCorrelationResponse?> SpotVolCorrelationTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await SpotVolCorrelationAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<SpotVolCorrelationResponse>(PostSerializerOptions);
    }

    /// <summary>
    /// Implied vs realized correlation (dispersion / vol-arb) for an index versus a basket. Requires Alpha+.
    /// </summary>
    /// <param name="index">Index symbol (e.g. <c>SPX</c>, <c>NDX</c>, <c>RUT</c>).</param>
    /// <param name="symbols">Constituent symbols (comma-joined; max 50, de-duped).</param>
    /// <param name="weights">Optional weights; equal-weighted when omitted.</param>
    /// <param name="horizonDays">Lookback window for realized correlation (clamped to [5, 252]).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> DispersionAsync(string index, IEnumerable<string> symbols, IEnumerable<double>? weights = null, int? horizonDays = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>
        {
            ["index"] = index,
            ["symbols"] = string.Join(",", symbols),
        };
        if (weights is not null)
        {
            var ws = new List<string>();
            foreach (var w in weights) ws.Add(Inv(w));
            p["weights"] = string.Join(",", ws);
        }
        if (horizonDays.HasValue) p["horizon_days"] = horizonDays.Value.ToString();
        return GetAsync("/v1/dispersion", p, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="DispersionAsync"/>.</summary>
    public async Task<DispersionResponse?> DispersionTypedAsync(string index, IEnumerable<string> symbols, IEnumerable<double>? weights = null, int? horizonDays = null, CancellationToken ct = default)
    {
        var e = await DispersionAsync(index, symbols, weights, horizonDays, ct).ConfigureAwait(false);
        return e.Deserialize<DispersionResponse>(PostSerializerOptions);
    }

    /// <summary>Options-implied expected move (straddle-derived) per expiry. Requires Basic+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="expiry">Optional single expiry (<c>yyyy-MM-dd</c>); all listed expiries when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> ExpectedMoveAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/expected-move/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="ExpectedMoveAsync"/>.</summary>
    public async Task<ExpectedMoveResponse?> ExpectedMoveTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await ExpectedMoveAsync(symbol, expiry, ct).ConfigureAwait(false);
        return e.Deserialize<ExpectedMoveResponse>(PostSerializerOptions);
    }

    /// <summary>Range-based realized (historical) vol estimators (close-to-close, Parkinson, Garman-Klass, Rogers-Satchell, Yang-Zhang) over 10/20/30-day windows. Requires Alpha+.</summary>
    public Task<JsonElement> RealizedVolatilityAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/volatility/realized/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="RealizedVolatilityAsync"/>.</summary>
    public async Task<RealizedVolatilityResponse?> RealizedVolatilityTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await RealizedVolatilityAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<RealizedVolatilityResponse>(PostSerializerOptions);
    }

    /// <summary>Conditional vol forecasts: EWMA (λ=0.94), HAR-RV, and GARCH(1,1) MLE with a multi-horizon term structure. Requires Alpha+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="dist">Innovation distribution: <c>student_t</c> (default) or <c>gaussian</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> VolatilityForecastAsync(string symbol, string? dist = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (dist is not null) p["dist"] = dist;
        return GetAsync($"/v1/volatility/forecast/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="VolatilityForecastAsync"/>.</summary>
    public async Task<VolatilityForecastResponse?> VolatilityForecastTypedAsync(string symbol, string? dist = null, CancellationToken ct = default)
    {
        var e = await VolatilityForecastAsync(symbol, dist, ct).ConfigureAwait(false);
        return e.Deserialize<VolatilityForecastResponse>(PostSerializerOptions);
    }

    // ── VRP history ───────────────────────────────────────────────────────────

    /// <summary>Historical VRP (implied-vs-realized vol premium) time series. Requires Alpha+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="days">Lookback window in days.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> VrpHistoryAsync(string symbol, int? days = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (days.HasValue) p["days"] = days.Value.ToString();
        return GetAsync($"/v1/vrp/{Uri.EscapeDataString(symbol)}/history", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="VrpHistoryAsync"/>.</summary>
    public async Task<VrpHistoryResponse?> VrpHistoryTypedAsync(string symbol, int? days = null, CancellationToken ct = default)
    {
        var e = await VrpHistoryAsync(symbol, days, ct).ConfigureAwait(false);
        return e.Deserialize<VrpHistoryResponse>(PostSerializerOptions);
    }

    // ── Macro / Universe ──────────────────────────────────────────────────────

    /// <summary>VIX term-structure regime snapshot (contango/backwardation, VIX/VIX3M, percentiles). Requires Growth+.</summary>
    public Task<JsonElement> VixStateAsync(CancellationToken ct = default)
        => GetAsync("/v1/macro/vix-state", null, ct);

    /// <summary>Strongly-typed variant of <see cref="VixStateAsync"/>.</summary>
    public async Task<VixStateResponse?> VixStateTypedAsync(CancellationToken ct = default)
    {
        var e = await VixStateAsync(ct).ConfigureAwait(false);
        return e.Deserialize<VixStateResponse>(PostSerializerOptions);
    }

    /// <summary>Curated tier-1 / tier-2 pre-warmed symbol universe. Public — no auth required.</summary>
    /// <param name="sort"><c>tier</c> (default) or <c>symbol</c>.</param>
    /// <param name="limit">Clamped to [1, 1000] (default 200).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> UniverseAsync(string? sort = null, int? limit = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (sort is not null) p["sort"] = sort;
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        return GetAsync("/v1/universe", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="UniverseAsync"/>.</summary>
    public async Task<UniverseResponse?> UniverseTypedAsync(string? sort = null, int? limit = null, CancellationToken ct = default)
    {
        var e = await UniverseAsync(sort, limit, ct).ConfigureAwait(false);
        return e.Deserialize<UniverseResponse>(PostSerializerOptions);
    }

    // ── Flow: dealer premium + stock bars ─────────────────────────────────────

    /// <summary>Net dealer premium roll-up over the full tape (VWAP-weighted per minute bucket). Requires the Alpha plan.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="windowMinutes">Lookback window (clamped to [1, 10080], default 240).</param>
    /// <param name="expiry">Optional single expiry filter (<c>yyyy-MM-dd</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowDealerPremiumAsync(string symbol, int? windowMinutes = null, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (windowMinutes.HasValue) p["windowMinutes"] = windowMinutes.Value.ToString();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/options/{Uri.EscapeDataString(symbol)}/dealer-premium", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowDealerPremiumAsync"/>.</summary>
    public async Task<FlowDealerPremiumResponse?> FlowDealerPremiumTypedAsync(string symbol, int? windowMinutes = null, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowDealerPremiumAsync(symbol, windowMinutes, expiry, ct).ConfigureAwait(false);
        return e.Deserialize<FlowDealerPremiumResponse>(PostSerializerOptions);
    }

    /// <summary>Multi-resolution OHLCV + flow bars from the live trade tape (oldest-first). Requires the Alpha plan.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="resolution">Required: one of <c>1s</c>, <c>1m</c>, <c>5m</c>, <c>15m</c>, <c>30m</c>, <c>1h</c>, <c>4h</c>.</param>
    /// <param name="minutes">Look-back window (clamped to [1, 1440], default 60).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowStockBarsAsync(string symbol, string resolution, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?> { ["resolution"] = resolution };
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/stocks/{Uri.EscapeDataString(symbol)}/bars", p, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStockBarsAsync"/>.</summary>
    public async Task<FlowStockBarsResponse?> FlowStockBarsTypedAsync(string symbol, string resolution, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowStockBarsAsync(symbol, resolution, minutes, ct).ConfigureAwait(false);
        return e.Deserialize<FlowStockBarsResponse>(PostSerializerOptions);
    }

    // ── Zero-DTE Flow ─────────────────────────────────────────────────────────

    /// <summary>Live 0DTE flow snapshot (zero-dte analytics + intraday flow direction). Requires Growth+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="expiry">Optional 0DTE expiry override (<c>yyyy-MM-dd</c>); today's 0DTE expiry when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowZeroDteSnapshotAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/zero-dte/snapshot/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowZeroDteSnapshotAsync"/>.</summary>
    public async Task<ZeroDteFlowSnapshotResponse?> FlowZeroDteSnapshotTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowZeroDteSnapshotAsync(symbol, expiry, ct).ConfigureAwait(false);
        return e.Deserialize<ZeroDteFlowSnapshotResponse>(PostSerializerOptions);
    }

    /// <summary>Intraday 0DTE flow time series (one bar per interval). Requires Growth+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="bar">Bar size: <c>30s</c> (default), <c>1m</c>, <c>5m</c>, or <c>15m</c>.</param>
    /// <param name="minutes">Lookback window in minutes (clamped to [1, 390]).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowZeroDteSeriesAsync(string symbol, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (bar is not null) p["bar"] = bar;
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/zero-dte/series/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowZeroDteSeriesAsync"/>.</summary>
    public async Task<ZeroDteFlowSeriesResponse?> FlowZeroDteSeriesTypedAsync(string symbol, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowZeroDteSeriesAsync(symbol, bar, minutes, ct).ConfigureAwait(false);
        return e.Deserialize<ZeroDteFlowSeriesResponse>(PostSerializerOptions);
    }

    /// <summary>Estimated dealer hedge-flow time series for today's 0DTE chain. Requires Growth+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="side">Leg projection: <c>all</c> (default), <c>calls</c>, or <c>puts</c>.</param>
    /// <param name="bar">Bar size: <c>30s</c> (default), <c>1m</c>, <c>5m</c>, or <c>15m</c>.</param>
    /// <param name="minutes">Lookback window in minutes (clamped to [1, 390]).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowZeroDteHedgeFlowAsync(string symbol, string? side = null, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (side is not null) p["side"] = side;
        if (bar is not null) p["bar"] = bar;
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/zero-dte/hedge-flow/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowZeroDteHedgeFlowAsync"/>.</summary>
    public async Task<ZeroDteFlowHedgeFlowResponse?> FlowZeroDteHedgeFlowTypedAsync(string symbol, string? side = null, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowZeroDteHedgeFlowAsync(symbol, side, bar, minutes, ct).ConfigureAwait(false);
        return e.Deserialize<ZeroDteFlowHedgeFlowResponse>(PostSerializerOptions);
    }

    /// <summary>Per-strike value matrix for a strike × time 0DTE heatmap. Requires the Alpha plan.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="metric">One of <c>gex</c> (default), <c>dex</c>, <c>vex</c>, <c>chex</c>, <c>oi</c>, <c>signed_flow</c>.</param>
    /// <param name="mode"><c>raw</c> (default) absolute value, or <c>delta</c> bar-over-bar change.</param>
    /// <param name="bar">Bar size — only <c>1m</c> is supported in this phase.</param>
    /// <param name="minutes">Lookback window in minutes (clamped to [1, 390]).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowZeroDteHeatmapAsync(string symbol, string? metric = null, string? mode = null, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (metric is not null) p["metric"] = metric;
        if (mode is not null) p["mode"] = mode;
        if (bar is not null) p["bar"] = bar;
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/zero-dte/heatmap/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowZeroDteHeatmapAsync"/>.</summary>
    public async Task<ZeroDteFlowHeatmapResponse?> FlowZeroDteHeatmapTypedAsync(string symbol, string? metric = null, string? mode = null, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowZeroDteHeatmapAsync(symbol, metric, mode, bar, minutes, ct).ConfigureAwait(false);
        return e.Deserialize<ZeroDteFlowHeatmapResponse>(PostSerializerOptions);
    }

    /// <summary>Per-strike signed aggressor flow over today's 0DTE session. Requires the Alpha plan.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>SPY</c>).</param>
    /// <param name="bar">Bar size — only <c>1m</c> is supported in this phase.</param>
    /// <param name="minutes">Lookback window in minutes (clamped to [1, 390]).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowZeroDteStrikeFlowAsync(string symbol, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (bar is not null) p["bar"] = bar;
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/zero-dte/strike-flow/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowZeroDteStrikeFlowAsync"/>.</summary>
    public async Task<ZeroDteFlowStrikeFlowResponse?> FlowZeroDteStrikeFlowTypedAsync(string symbol, string? bar = null, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowZeroDteStrikeFlowAsync(symbol, bar, minutes, ct).ConfigureAwait(false);
        return e.Deserialize<ZeroDteFlowStrikeFlowResponse>(PostSerializerOptions);
    }

    /// <summary>Cross-symbol 0DTE leaderboard ranking the warm universe by a single metric. Requires the Alpha plan.</summary>
    /// <param name="metric">Ranking metric: <c>heat</c> (default), <c>pin_risk</c>, <c>abs_flow</c>, or <c>charm_intensity</c>.</param>
    /// <param name="n">Number of entries to return (clamped to [1, 100]).</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> FlowZeroDteLeaderboardAsync(string? metric = null, int? n = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (metric is not null) p["metric"] = metric;
        if (n.HasValue) p["n"] = n.Value.ToString();
        return GetAsync("/v1/flow/zero-dte/leaderboard", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowZeroDteLeaderboardAsync"/>.</summary>
    public async Task<ZeroDteFlowLeaderboardResponse?> FlowZeroDteLeaderboardTypedAsync(string? metric = null, int? n = null, CancellationToken ct = default)
    {
        var e = await FlowZeroDteLeaderboardAsync(metric, n, ct).ConfigureAwait(false);
        return e.Deserialize<ZeroDteFlowLeaderboardResponse>(PostSerializerOptions);
    }

    // ── Strategy Signals ──────────────────────────────────────────────────────
    //
    // All ten endpoints return the shared StrategyDecisionResponse envelope.
    // Each has an untyped *Async and a typed *TypedAsync helper.

    private async Task<StrategyDecisionResponse?> StrategyTypedAsync(JsonElement e)
        => await Task.FromResult(e.Deserialize<StrategyDecisionResponse>(PostSerializerOptions)).ConfigureAwait(false);

    /// <summary>Directional options-flow anomaly score + matching short vertical. Requires Growth+.</summary>
    public Task<JsonElement> StrategyFlowAnomalyAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/strategies/flow-anomaly/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyFlowAnomalyAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyFlowAnomalyTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
        => (await StrategyFlowAnomalyAsync(symbol, expiry, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>OPEX pin-risk score + iron-fly proposal for a single expiry. Requires Basic+.</summary>
    public Task<JsonElement> StrategyExpiryPositioningAsync(string symbol, string? expiry = null, int? minOpenInterest = null, double? wingWidth = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        if (minOpenInterest.HasValue) p["minOpenInterest"] = minOpenInterest.Value.ToString();
        if (wingWidth.HasValue) p["wingWidth"] = Inv(wingWidth.Value);
        return GetAsync($"/v1/strategies/expiry-positioning/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyExpiryPositioningAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyExpiryPositioningTypedAsync(string symbol, string? expiry = null, int? minOpenInterest = null, double? wingWidth = null, CancellationToken ct = default)
        => (await StrategyExpiryPositioningAsync(symbol, expiry, minOpenInterest, wingWidth, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>0DTE intraday structure proposal (iron fly / condor). Requires Growth+ and the 0DTE entitlement.</summary>
    public Task<JsonElement> StrategyZeroDteAsync(string symbol, string? expiry = null, int? minOpenInterest = null, double? wingWidth = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        if (minOpenInterest.HasValue) p["minOpenInterest"] = minOpenInterest.Value.ToString();
        if (wingWidth.HasValue) p["wingWidth"] = Inv(wingWidth.Value);
        return GetAsync($"/v1/strategies/zero-dte/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyZeroDteAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyZeroDteTypedAsync(string symbol, string? expiry = null, int? minOpenInterest = null, double? wingWidth = null, CancellationToken ct = default)
        => (await StrategyZeroDteAsync(symbol, expiry, minOpenInterest, wingWidth, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Dealer-regime read (positive vs negative gamma) from exposure positioning. Requires Growth+.</summary>
    public Task<JsonElement> StrategyDealerRegimeAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/strategies/dealer-regime/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyDealerRegimeAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyDealerRegimeTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
        => (await StrategyDealerRegimeAsync(symbol, expiry, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Vol-carry harvesting candidate (short premium under positive carry). Requires Alpha+.</summary>
    public Task<JsonElement> StrategyVolCarryAsync(string symbol, string? expiry = null, int? minOpenInterest = null, double? targetShortDelta = null, double? maxWidth = null, double? minCredit = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        if (minOpenInterest.HasValue) p["minOpenInterest"] = minOpenInterest.Value.ToString();
        if (targetShortDelta.HasValue) p["targetShortDelta"] = Inv(targetShortDelta.Value);
        if (maxWidth.HasValue) p["maxWidth"] = Inv(maxWidth.Value);
        if (minCredit.HasValue) p["minCredit"] = Inv(minCredit.Value);
        return GetAsync($"/v1/strategies/vol-carry/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyVolCarryAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyVolCarryTypedAsync(string symbol, string? expiry = null, int? minOpenInterest = null, double? targetShortDelta = null, double? maxWidth = null, double? minCredit = null, CancellationToken ct = default)
        => (await StrategyVolCarryAsync(symbol, expiry, minOpenInterest, targetShortDelta, maxWidth, minCredit, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Yield-enhancement overlay (covered-call / cash-secured-put) proposal. Requires Growth+.</summary>
    public Task<JsonElement> StrategyYieldEnhancementAsync(string symbol, string? expiry = null, double? targetDelta = null, int? minOpenInterest = null, string? structure = null, bool? excludeEarningsBeforeExpiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        if (targetDelta.HasValue) p["targetDelta"] = Inv(targetDelta.Value);
        if (minOpenInterest.HasValue) p["minOpenInterest"] = minOpenInterest.Value.ToString();
        if (structure is not null) p["structure"] = structure;
        if (excludeEarningsBeforeExpiry.HasValue) p["excludeEarningsBeforeExpiry"] = excludeEarningsBeforeExpiry.Value ? "true" : "false";
        return GetAsync($"/v1/strategies/yield-enhancement/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyYieldEnhancementAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyYieldEnhancementTypedAsync(string symbol, string? expiry = null, double? targetDelta = null, int? minOpenInterest = null, string? structure = null, bool? excludeEarningsBeforeExpiry = null, CancellationToken ct = default)
        => (await StrategyYieldEnhancementAsync(symbol, expiry, targetDelta, minOpenInterest, structure, excludeEarningsBeforeExpiry, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Surface-anomaly / vol-arbitrage signal from the calibrated surface. Requires Alpha+.</summary>
    public Task<JsonElement> StrategySurfaceAnomalyAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/strategies/surface-anomaly/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategySurfaceAnomalyAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategySurfaceAnomalyTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
        => (await StrategySurfaceAnomalyAsync(symbol, expiry, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Skew-trade signal (risk-reversal / put-skew richness). Requires Growth+.</summary>
    public Task<JsonElement> StrategySkewAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/strategies/skew/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategySkewAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategySkewTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
        => (await StrategySkewAsync(symbol, expiry, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Term-structure signal (calendar / diagonal opportunity). Requires Growth+.</summary>
    public Task<JsonElement> StrategyTermStructureAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/strategies/term-structure/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="StrategyTermStructureAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyTermStructureTypedAsync(string symbol, CancellationToken ct = default)
        => (await StrategyTermStructureAsync(symbol, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    /// <summary>Tail-pricing signal (wing richness / cheap convexity). Requires Growth+.</summary>
    public Task<JsonElement> StrategyTailPricingAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/strategies/tail-pricing/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="StrategyTailPricingAsync"/>.</summary>
    public async Task<StrategyDecisionResponse?> StrategyTailPricingTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
        => (await StrategyTailPricingAsync(symbol, expiry, ct).ConfigureAwait(false)).Deserialize<StrategyDecisionResponse>(PostSerializerOptions);

    // ── Earnings ──────────────────────────────────────────────────────────────

    /// <summary>Upcoming earnings calendar over a forward window. Requires Growth+.</summary>
    /// <param name="days">Forward window in days (clamped to [1, 90], default 14).</param>
    /// <param name="symbols">Optional symbol filter (comma-joined).</param>
    /// <param name="importance">Optional minimum importance rating.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> EarningsCalendarAsync(int? days = null, IEnumerable<string>? symbols = null, int? importance = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (days.HasValue) p["days"] = days.Value.ToString();
        if (symbols is not null) p["symbols"] = string.Join(",", symbols);
        if (importance.HasValue) p["importance"] = importance.Value.ToString();
        return GetAsync("/v1/earnings/calendar", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="EarningsCalendarAsync"/>.</summary>
    public async Task<EarningsCalendarResponse?> EarningsCalendarTypedAsync(int? days = null, IEnumerable<string>? symbols = null, int? importance = null, CancellationToken ct = default)
    {
        var e = await EarningsCalendarAsync(days, symbols, importance, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsCalendarResponse>(PostSerializerOptions);
    }

    /// <summary>Earnings-implied move (straddle-derived) for the next event. Requires Growth+.</summary>
    public Task<JsonElement> EarningsExpectedMoveAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/earnings/expected-move/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="EarningsExpectedMoveAsync"/>.</summary>
    public async Task<EarningsExpectedMoveResponse?> EarningsExpectedMoveTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await EarningsExpectedMoveAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsExpectedMoveResponse>(PostSerializerOptions);
    }

    /// <summary>Historical earnings reactions (implied vs realized move, surprises). Requires Growth+.</summary>
    /// <param name="symbol">Underlying symbol (e.g. <c>AAPL</c>).</param>
    /// <param name="limit">Maximum number of prior events to return.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> EarningsHistoryAsync(string symbol, int? limit = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        return GetAsync($"/v1/earnings/history/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="EarningsHistoryAsync"/>.</summary>
    public async Task<EarningsHistoryResponse?> EarningsHistoryTypedAsync(string symbol, int? limit = null, CancellationToken ct = default)
    {
        var e = await EarningsHistoryAsync(symbol, limit, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsHistoryResponse>(PostSerializerOptions);
    }

    /// <summary>Estimated post-earnings IV crush (front-month deflation). Requires Growth+.</summary>
    public Task<JsonElement> EarningsIvCrushAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/earnings/iv-crush/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="EarningsIvCrushAsync"/>.</summary>
    public async Task<EarningsIvCrushResponse?> EarningsIvCrushTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await EarningsIvCrushAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsIvCrushResponse>(PostSerializerOptions);
    }

    /// <summary>Earnings VRP (implied vs realized move premium) with surprise reactions. Requires Alpha+.</summary>
    public Task<JsonElement> EarningsVrpAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/earnings/vrp/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="EarningsVrpAsync"/>.</summary>
    public async Task<EarningsVrpResponse?> EarningsVrpTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await EarningsVrpAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsVrpResponse>(PostSerializerOptions);
    }

    /// <summary>Dealer positioning into the earnings event (GEX buckets, top strikes, levels). Requires Alpha+.</summary>
    public Task<JsonElement> EarningsDealerPositioningAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/earnings/dealer-positioning/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="EarningsDealerPositioningAsync"/>.</summary>
    public async Task<EarningsDealerPositioningResponse?> EarningsDealerPositioningTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await EarningsDealerPositioningAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsDealerPositioningResponse>(PostSerializerOptions);
    }

    /// <summary>Earnings-aware strategy scores + context (straddle/strangle/iron-condor). Requires Alpha+.</summary>
    public Task<JsonElement> EarningsStrategiesAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/earnings/strategies/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Strongly-typed variant of <see cref="EarningsStrategiesAsync"/>.</summary>
    public async Task<EarningsStrategiesResponse?> EarningsStrategiesTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await EarningsStrategiesAsync(symbol, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsStrategiesResponse>(PostSerializerOptions);
    }

    /// <summary>Cross-sectional earnings screener ranked by VRP richness / cheapest move / crush / importance. Requires Alpha+.</summary>
    /// <param name="sort"><c>vrp_richest</c> (default), <c>cheapest_move</c>, <c>highest_crush</c>, or <c>importance</c>.</param>
    /// <param name="limit">Max rows (clamped to [1, 50], default 20).</param>
    /// <param name="days">Forward window in days (clamped to [1, 60], default 14).</param>
    /// <param name="minImportance">Only include events with importance &gt;= this value.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<JsonElement> EarningsScreenerAsync(string? sort = null, int? limit = null, int? days = null, int? minImportance = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (sort is not null) p["sort"] = sort;
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        if (days.HasValue) p["days"] = days.Value.ToString();
        if (minImportance.HasValue) p["min_importance"] = minImportance.Value.ToString();
        return GetAsync("/v1/earnings/screener", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="EarningsScreenerAsync"/>.</summary>
    public async Task<EarningsScreenerResponse?> EarningsScreenerTypedAsync(string? sort = null, int? limit = null, int? days = null, int? minImportance = null, CancellationToken ct = default)
    {
        var e = await EarningsScreenerAsync(sort, limit, days, minImportance, ct).ConfigureAwait(false);
        return e.Deserialize<EarningsScreenerResponse>(PostSerializerOptions);
    }

    // ── Structures (pure-math POST) ───────────────────────────────────────────

    /// <summary>At-expiry P&amp;L curve, breakevens, and max profit/loss for a multi-leg structure. Requires Basic+.</summary>
    public Task<JsonElement> StructurePnlAsync(StructurePnlRequest request, CancellationToken ct = default)
        => PostAsync("/v1/structures/pnl", request, ct);

    /// <summary>Strongly-typed variant of <see cref="StructurePnlAsync(StructurePnlRequest, CancellationToken)"/>.</summary>
    public async Task<StructurePnlResponse?> StructurePnlTypedAsync(StructurePnlRequest request, CancellationToken ct = default)
    {
        var e = await StructurePnlAsync(request, ct).ConfigureAwait(false);
        return e.Deserialize<StructurePnlResponse>(PostSerializerOptions);
    }

    /// <summary>Aggregated, quantity-scaled, direction-signed position Greeks for a multi-leg structure. Requires Basic+.</summary>
    public Task<JsonElement> StructureGreeksAsync(StructureGreeksRequest request, CancellationToken ct = default)
        => PostAsync("/v1/structures/greeks", request, ct);

    /// <summary>Strongly-typed variant of <see cref="StructureGreeksAsync(StructureGreeksRequest, CancellationToken)"/>.</summary>
    public async Task<StructureGreeksResponse?> StructureGreeksTypedAsync(StructureGreeksRequest request, CancellationToken ct = default)
    {
        var e = await StructureGreeksAsync(request, ct).ConfigureAwait(false);
        return e.Deserialize<StructureGreeksResponse>(PostSerializerOptions);
    }

    // ── Screener fields catalogue ─────────────────────────────────────────────

    /// <summary>Catalogue of screener fields available for filters/sort/select/formulas. Requires any valid key (Free+).</summary>
    public Task<JsonElement> ScreenerFieldsAsync(CancellationToken ct = default)
        => GetAsync("/v1/screener/fields", null, ct);

    /// <summary>Strongly-typed variant of <see cref="ScreenerFieldsAsync"/>.</summary>
    public async Task<ScreenerFieldsResponse?> ScreenerFieldsTypedAsync(CancellationToken ct = default)
    {
        var e = await ScreenerFieldsAsync(ct).ConfigureAwait(false);
        return e.Deserialize<ScreenerFieldsResponse>(PostSerializerOptions);
    }
}
