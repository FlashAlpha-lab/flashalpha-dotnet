using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FlashAlpha;
using Xunit;

namespace FlashAlpha.Tests;

/// <summary>
/// Live integration coverage for the parity-extension endpoint families that
/// were added after the original <see cref="IntegrationTests"/> surface: the
/// volatility analytics suite (realized, forecast, skew-term, spot-vol
/// correlation, dispersion, expected-move, VRP history), the SVI surface, the
/// extended exposure suite (sheet / term-structure / basket / OI-diff),
/// liquidity, macro / universe, dealer-premium + stock bars, the intraday 0DTE
/// flow family, all ten strategy-signal endpoints, the earnings analytics
/// suite, the two pure-math structure POST endpoints, the screener-fields
/// catalogue, and the account endpoint.
///
/// <para>All tests skip unless <c>FLASHALPHA_API_KEY</c> is set. Because this
/// runs against the live API at any hour, each test treats the documented
/// no-data / no-entitlement outcomes as PASS:</para>
/// <list type="bullet">
///   <item>A 2xx with a schema-valid (possibly empty) body — verified by an
///   object-kind / symbol-echo assertion plus a clean typed deserialize.</item>
///   <item>A documented <see cref="TierRestrictedException"/> (403) when the
///   key's tier does not entitle the endpoint.</item>
///   <item>A documented <see cref="NotFoundException"/> (404) for a symbol /
///   event that has no data right now (e.g. no upcoming earnings).</item>
/// </list>
/// Any other status, or a deserialization exception, fails the test.
/// </summary>
public sealed class IntegrationTestsExtended
{
    private static FlashAlphaClient CreateClient() =>
        new FlashAlphaClient(Environment.GetEnvironmentVariable("FLASHALPHA_API_KEY")!);

    // Ramped, always-warm symbols.
    private const string Sym = "SPY";
    private const string Sym2 = "AAPL";

    /// <summary>
    /// Runs <paramref name="call"/> and asserts the untyped JSON is a 2xx object
    /// (or a documented 403/404), then runs <paramref name="typed"/> and asserts
    /// it deserializes without throwing. Returns the untyped element for callers
    /// that want extra field-level assertions, or <c>null</c> when the endpoint
    /// returned a documented 403/404.
    /// </summary>
    private static async Task<JsonElement?> ProbeAsync(
        Func<Task<JsonElement>> call,
        Func<Task>? typed = null)
    {
        JsonElement el;
        try
        {
            el = await call();
        }
        catch (TierRestrictedException) { return null; } // documented 403
        catch (NotFoundException) { return null; }       // documented 404 (no data)
        catch (ServerException ex) when (
            ex.Message.Contains("maintenance", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase))
        {
            // Documented transient backend-maintenance 5xx (e.g. the flow
            // analytics service announces "temporarily unavailable while we
            // complete maintenance"). The SDK correctly surfaces it as a typed
            // ServerException; treat the announced maintenance window as a
            // tolerated outcome rather than an SDK/schema failure. Any other
            // 5xx still propagates and fails the test.
            return null;
        }

        Assert.True(
            el.ValueKind == JsonValueKind.Object || el.ValueKind == JsonValueKind.Array,
            $"expected object/array body, got {el.ValueKind}");

        if (typed is not null)
        {
            // A field-mapping miss (renamed JsonPropertyName) or a malformed body
            // surfaces here as a JsonException and fails the test.
            await typed();
        }
        return el;
    }

    // ── Account ───────────────────────────────────────────────────────────────

    [LiveFact]
    public async Task Account_ReturnsObject()
    {
        using var client = CreateClient();
        await ProbeAsync(() => client.AccountAsync());
    }

    // ── Volatility analytics suite ────────────────────────────────────────────

    [LiveFact]
    public async Task RealizedVolatility_SPY_DeserializesEstimators()
    {
        using var client = CreateClient();
        var el = await ProbeAsync(
            () => client.RealizedVolatilityAsync(Sym),
            async () =>
            {
                var r = await client.RealizedVolatilityTypedAsync(Sym);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
                // estimators present on a populated body; tolerate empty.
                if (r.Estimators is not null)
                {
                    // At least one estimator window should map when data exists.
                    var c2c = r.Estimators.CloseToClose;
                    if (c2c is not null)
                        Assert.True(c2c.Rv10.HasValue || c2c.Rv20.HasValue || c2c.Rv30.HasValue);
                }
            });
        if (el is { } e && e.TryGetProperty("symbol", out var s))
            Assert.Equal(Sym, s.GetString());
    }

    [LiveFact]
    public async Task VolatilityForecast_SPY_DeserializesModels()
    {
        using var client = CreateClient();
        var el = await ProbeAsync(
            () => client.VolatilityForecastAsync(Sym),
            async () =>
            {
                var r = await client.VolatilityForecastTypedAsync(Sym);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
                // When EWMA is present its annualized vol must map to a number.
                if (r.Ewma is not null && r.Ewma.VolAnnualized is { } v)
                    Assert.True(v >= 0);
                // GARCH forecast term structure maps to a list when converged.
                if (r.Garch?.Forecast is { Count: > 0 } fc)
                    Assert.NotNull(fc[0]);
            });
        if (el is { } e && e.TryGetProperty("symbol", out var s))
            Assert.Equal(Sym, s.GetString());
    }

    [LiveFact]
    public async Task VolatilityForecast_SPY_GaussianDist_DeserializesModels()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.VolatilityForecastAsync(Sym, dist: "gaussian"),
            async () =>
            {
                var r = await client.VolatilityForecastTypedAsync(Sym, dist: "gaussian");
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
            });
    }

    [LiveFact]
    public async Task SkewTerm_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.SkewTermAsync(Sym),
            async () =>
            {
                var r = await client.SkewTermTypedAsync(Sym);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
            });
    }

    [LiveFact]
    public async Task SpotVolCorrelation_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.SpotVolCorrelationAsync(Sym),
            async () =>
            {
                var r = await client.SpotVolCorrelationTypedAsync(Sym);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
            });
    }

    [LiveFact]
    public async Task Dispersion_Spx_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.DispersionAsync("SPX", new[] { "AAPL", "MSFT", "NVDA" }),
            async () =>
            {
                var r = await client.DispersionTypedAsync("SPX", new[] { "AAPL", "MSFT", "NVDA" });
                Assert.NotNull(r);
            });
    }

    [LiveFact]
    public async Task ExpectedMove_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.ExpectedMoveAsync(Sym),
            async () =>
            {
                var r = await client.ExpectedMoveTypedAsync(Sym);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
            });
    }

    [LiveFact]
    public async Task VrpHistory_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.VrpHistoryAsync(Sym, days: 30),
            async () =>
            {
                var r = await client.VrpHistoryTypedAsync(Sym, days: 30);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
            });
    }

    // ── SVI surface ───────────────────────────────────────────────────────────

    [LiveFact]
    public async Task SurfaceSvi_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.SurfaceSviAsync(Sym),
            async () => Assert.NotNull(await client.SurfaceSviTypedAsync(Sym)));
    }

    // ── Extended exposure suite ───────────────────────────────────────────────

    [LiveFact]
    public async Task ExposureSheet_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.ExposureSheetAsync(Sym),
            async () => Assert.NotNull(await client.ExposureSheetTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task ExposureTermStructure_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.ExposureTermStructureAsync(Sym),
            async () => Assert.NotNull(await client.ExposureTermStructureTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task ExposureBasket_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.ExposureBasketAsync(new[] { Sym, Sym2 }),
            async () => Assert.NotNull(await client.ExposureBasketTypedAsync(new[] { Sym, Sym2 })));
    }

    [LiveFact]
    public async Task ExposureOiDiff_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.ExposureOiDiffAsync(Sym, topN: 5),
            async () => Assert.NotNull(await client.ExposureOiDiffTypedAsync(Sym, topN: 5)));
    }

    [LiveFact]
    public async Task Liquidity_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.LiquidityAsync(Sym),
            async () =>
            {
                var r = await client.LiquidityTypedAsync(Sym);
                Assert.NotNull(r);
                Assert.Equal(Sym, r!.Symbol);
            });
    }

    // ── Macro / universe ──────────────────────────────────────────────────────

    [LiveFact]
    public async Task VixState_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.VixStateAsync(),
            async () => Assert.NotNull(await client.VixStateTypedAsync()));
    }

    [LiveFact]
    public async Task Universe_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.UniverseAsync(limit: 50),
            async () => Assert.NotNull(await client.UniverseTypedAsync(limit: 50)));
    }

    // ── Flow: dealer-premium + stock bars ─────────────────────────────────────

    [LiveFact]
    public async Task FlowDealerPremium_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowDealerPremiumAsync(Sym, windowMinutes: 240),
            async () => Assert.NotNull(await client.FlowDealerPremiumTypedAsync(Sym, windowMinutes: 240)));
    }

    [LiveFact]
    public async Task FlowStockBars_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowStockBarsAsync(Sym, resolution: "5m", minutes: 60),
            async () => Assert.NotNull(await client.FlowStockBarsTypedAsync(Sym, resolution: "5m", minutes: 60)));
    }

    // ── Intraday 0DTE flow family ─────────────────────────────────────────────

    [LiveFact]
    public async Task FlowZeroDteSnapshot_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowZeroDteSnapshotAsync(Sym),
            async () => Assert.NotNull(await client.FlowZeroDteSnapshotTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task FlowZeroDteSeries_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowZeroDteSeriesAsync(Sym, bar: "5m", minutes: 60),
            async () => Assert.NotNull(await client.FlowZeroDteSeriesTypedAsync(Sym, bar: "5m", minutes: 60)));
    }

    [LiveFact]
    public async Task FlowZeroDteHedgeFlow_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowZeroDteHedgeFlowAsync(Sym, bar: "5m", minutes: 60),
            async () => Assert.NotNull(await client.FlowZeroDteHedgeFlowTypedAsync(Sym, bar: "5m", minutes: 60)));
    }

    [LiveFact]
    public async Task FlowZeroDteHeatmap_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowZeroDteHeatmapAsync(Sym, metric: "gex", minutes: 60),
            async () => Assert.NotNull(await client.FlowZeroDteHeatmapTypedAsync(Sym, metric: "gex", minutes: 60)));
    }

    [LiveFact]
    public async Task FlowZeroDteStrikeFlow_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.FlowZeroDteStrikeFlowAsync(Sym, minutes: 60),
            async () => Assert.NotNull(await client.FlowZeroDteStrikeFlowTypedAsync(Sym, minutes: 60)));
    }

    // ── Strategy signals (all ten share StrategyDecisionResponse) ─────────────

    [LiveFact]
    public async Task StrategyFlowAnomaly_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyFlowAnomalyAsync(Sym),
            async () => Assert.NotNull(await client.StrategyFlowAnomalyTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyExpiryPositioning_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyExpiryPositioningAsync(Sym),
            async () => Assert.NotNull(await client.StrategyExpiryPositioningTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyZeroDte_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyZeroDteAsync(Sym),
            async () => Assert.NotNull(await client.StrategyZeroDteTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyDealerRegime_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyDealerRegimeAsync(Sym),
            async () => Assert.NotNull(await client.StrategyDealerRegimeTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyVolCarry_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyVolCarryAsync(Sym),
            async () => Assert.NotNull(await client.StrategyVolCarryTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyYieldEnhancement_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyYieldEnhancementAsync(Sym),
            async () => Assert.NotNull(await client.StrategyYieldEnhancementTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategySurfaceAnomaly_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategySurfaceAnomalyAsync(Sym),
            async () => Assert.NotNull(await client.StrategySurfaceAnomalyTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategySkew_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategySkewAsync(Sym),
            async () => Assert.NotNull(await client.StrategySkewTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyTermStructure_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyTermStructureAsync(Sym),
            async () => Assert.NotNull(await client.StrategyTermStructureTypedAsync(Sym)));
    }

    [LiveFact]
    public async Task StrategyTailPricing_SPY_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.StrategyTailPricingAsync(Sym),
            async () => Assert.NotNull(await client.StrategyTailPricingTypedAsync(Sym)));
    }

    // ── Earnings analytics suite ──────────────────────────────────────────────

    [LiveFact]
    public async Task EarningsCalendar_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsCalendarAsync(days: 14),
            async () => Assert.NotNull(await client.EarningsCalendarTypedAsync(days: 14)));
    }

    [LiveFact]
    public async Task EarningsExpectedMove_AAPL_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsExpectedMoveAsync(Sym2),
            async () => Assert.NotNull(await client.EarningsExpectedMoveTypedAsync(Sym2)));
    }

    [LiveFact]
    public async Task EarningsHistory_AAPL_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsHistoryAsync(Sym2, limit: 8),
            async () => Assert.NotNull(await client.EarningsHistoryTypedAsync(Sym2, limit: 8)));
    }

    [LiveFact]
    public async Task EarningsIvCrush_AAPL_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsIvCrushAsync(Sym2),
            async () => Assert.NotNull(await client.EarningsIvCrushTypedAsync(Sym2)));
    }

    [LiveFact]
    public async Task EarningsVrp_AAPL_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsVrpAsync(Sym2),
            async () => Assert.NotNull(await client.EarningsVrpTypedAsync(Sym2)));
    }

    [LiveFact]
    public async Task EarningsDealerPositioning_AAPL_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsDealerPositioningAsync(Sym2),
            async () => Assert.NotNull(await client.EarningsDealerPositioningTypedAsync(Sym2)));
    }

    [LiveFact]
    public async Task EarningsStrategies_AAPL_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsStrategiesAsync(Sym2),
            async () => Assert.NotNull(await client.EarningsStrategiesTypedAsync(Sym2)));
    }

    [LiveFact]
    public async Task EarningsScreener_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.EarningsScreenerAsync(limit: 10),
            async () => Assert.NotNull(await client.EarningsScreenerTypedAsync(limit: 10)));
    }

    // ── Structures (pure-math POST) ───────────────────────────────────────────

    [LiveFact]
    public async Task StructurePnl_Deserializes()
    {
        using var client = CreateClient();
        var req = new StructurePnlRequest
        {
            Legs = new List<StructureLeg>
            {
                new() { Action = "buy", Type = "call", Strike = 505, Quantity = 1, Premium = 5.0 },
                new() { Action = "sell", Type = "call", Strike = 515, Quantity = 1, Premium = 2.0 },
            },
        };
        await ProbeAsync(
            () => client.StructurePnlAsync(req),
            async () => Assert.NotNull(await client.StructurePnlTypedAsync(req)));
    }

    [LiveFact]
    public async Task StructureGreeks_Deserializes()
    {
        using var client = CreateClient();
        // Use a far-future expiry so the leg always has positive time value.
        var expiry = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        var req = new StructureGreeksRequest
        {
            Spot = 500,
            Legs = new List<StructureLeg>
            {
                new() { Action = "buy", Type = "call", Strike = 505, Expiry = expiry, ImpliedVol = 0.20, Quantity = 1 },
                new() { Action = "buy", Type = "put", Strike = 495, Expiry = expiry, ImpliedVol = 0.20, Quantity = 1 },
            },
        };
        await ProbeAsync(
            () => client.StructureGreeksAsync(req),
            async () => Assert.NotNull(await client.StructureGreeksTypedAsync(req)));
    }

    // ── Screener fields catalogue ─────────────────────────────────────────────

    [LiveFact]
    public async Task ScreenerFields_Deserializes()
    {
        using var client = CreateClient();
        await ProbeAsync(
            () => client.ScreenerFieldsAsync(),
            async () => Assert.NotNull(await client.ScreenerFieldsTypedAsync()));
    }
}
