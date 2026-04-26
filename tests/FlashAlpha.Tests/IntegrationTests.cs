using System;
using System.Text.Json;
using System.Threading.Tasks;
using FlashAlpha;
using Xunit;

namespace FlashAlpha.Tests;

/// <summary>
/// Custom xunit fact that skips when FLASHALPHA_API_KEY is not set.
/// </summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FLASHALPHA_API_KEY")))
            Skip = "Set FLASHALPHA_API_KEY environment variable to run live integration tests.";
    }
}

/// <summary>
/// Live integration tests against the FlashAlpha API.
/// All tests are skipped unless the FLASHALPHA_API_KEY environment variable is set.
/// </summary>
public sealed class IntegrationTests
{
    private static FlashAlphaClient CreateClient() =>
        new FlashAlphaClient(Environment.GetEnvironmentVariable("FLASHALPHA_API_KEY")!);

    [LiveFact]
    public async Task Health_ReturnsSuccessResponse()
    {
        using var client = CreateClient();
        var result = await client.HealthAsync();
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Symbols_ReturnsList()
    {
        using var client = CreateClient();
        var result = await client.SymbolsAsync();
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Tickers_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.TickersAsync();
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Gex_Spy_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.GexAsync("SPY");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Dex_Spy_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.DexAsync("SPY");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task ExposureLevels_Spy_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.ExposureLevelsAsync("SPY");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Greeks_Call_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.GreeksAsync(spot: 500, strike: 505, dte: 30, sigma: 0.20, type: "call");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Iv_Call_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.IvAsync(spot: 500, strike: 505, dte: 30, price: 10.0, type: "call");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task StockQuote_AAPL_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.StockQuoteAsync("AAPL");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Surface_SPX_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.SurfaceAsync("SPX");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task ZeroDte_SPX_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.ZeroDteAsync("SPX");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    // Validate the full 0DTE response shape — fine-grained hedging buckets,
    // distance-to-flip in dollars/sigmas, pin sub-scores, flow concentration,
    // wall strength + level cluster, the new liquidity & metadata sections,
    // and per-strike greeks/quotes. Uses SPX which has daily 0DTE.
    [LiveFact]
    public async Task ZeroDte_SPX_IncludesAllNewFields()
    {
        using var client = CreateClient();
        var r = await client.ZeroDteAsync("SPX");
        Assert.Equal("SPX", r.GetProperty("symbol").GetString());

        if (r.TryGetProperty("no_zero_dte", out var noZ) && noZ.ValueKind == JsonValueKind.True)
        {
            Assert.True(r.TryGetProperty("next_zero_dte_expiry", out _));
            return;
        }

        // top-level
        foreach (var key in new[] { "underlying_price", "expiration", "as_of", "market_open",
                                    "time_to_close_hours", "time_to_close_pct" })
            Assert.True(r.TryGetProperty(key, out _), $"top-level {key} missing");

        // regime
        var regime = r.GetProperty("regime");
        foreach (var key in new[] { "label", "description", "gamma_flip", "spot_vs_flip", "spot_to_flip_pct",
                                    "distance_to_flip_dollars", "distance_to_flip_sigmas" })
            Assert.True(regime.TryGetProperty(key, out _), $"regime.{key} missing");

        // exposures
        var exposures = r.GetProperty("exposures");
        foreach (var key in new[] { "net_gex", "net_dex", "net_vex", "net_chex",
                                    "pct_of_total_gex", "total_chain_net_gex" })
            Assert.True(exposures.TryGetProperty(key, out _), $"exposures.{key} missing");

        // expected_move
        var em = r.GetProperty("expected_move");
        foreach (var key in new[] { "implied_1sd_dollars", "implied_1sd_pct", "remaining_1sd_dollars",
                                    "remaining_1sd_pct", "upper_bound", "lower_bound",
                                    "straddle_price", "atm_iv" })
            Assert.True(em.TryGetProperty(key, out _), $"expected_move.{key} missing");

        // pin_risk
        var pr = r.GetProperty("pin_risk");
        foreach (var key in new[] { "magnet_strike", "magnet_gex", "distance_to_magnet_pct",
                                    "pin_score", "components", "max_pain",
                                    "oi_concentration_top3_pct", "description" })
            Assert.True(pr.TryGetProperty(key, out _), $"pin_risk.{key} missing");
        var components = pr.GetProperty("components");
        foreach (var key in new[] { "oi_score", "proximity_score", "time_score", "gamma_score" })
            Assert.True(components.TryGetProperty(key, out _), $"pin_risk.components.{key} missing");

        // hedging — fine-grained buckets + convexity
        var hedging = r.GetProperty("hedging");
        foreach (var bucket in new[] { "spot_up_10bp", "spot_down_10bp",
                                       "spot_up_25bp", "spot_down_25bp",
                                       "spot_up_half_pct", "spot_down_half_pct",
                                       "spot_up_1pct", "spot_down_1pct" })
        {
            Assert.True(hedging.TryGetProperty(bucket, out var b), $"hedging.{bucket} missing");
            foreach (var key in new[] { "dealer_shares_to_trade", "direction", "notional_usd" })
                Assert.True(b.TryGetProperty(key, out _), $"hedging.{bucket}.{key} missing");
        }
        Assert.True(hedging.TryGetProperty("convexity_at_spot", out _));

        // decay
        var decay = r.GetProperty("decay");
        foreach (var key in new[] { "net_theta_dollars", "theta_per_hour_remaining", "charm_regime",
                                    "charm_description", "gamma_acceleration", "description" })
            Assert.True(decay.TryGetProperty(key, out _), $"decay.{key} missing");

        // vol_context
        var vc = r.GetProperty("vol_context");
        foreach (var key in new[] { "zero_dte_atm_iv", "seven_dte_atm_iv", "iv_ratio_0dte_7dte",
                                    "vix", "vanna_exposure", "vanna_interpretation", "description" })
            Assert.True(vc.TryGetProperty(key, out _), $"vol_context.{key} missing");

        // flow
        var flow = r.GetProperty("flow");
        foreach (var key in new[] { "total_volume", "call_volume", "put_volume",
                                    "net_call_minus_put_volume",
                                    "total_oi", "call_oi", "put_oi",
                                    "pc_ratio_volume", "pc_ratio_oi", "volume_to_oi_ratio",
                                    "atm_volume_share_pct", "top3_strike_volume_pct" })
            Assert.True(flow.TryGetProperty(key, out _), $"flow.{key} missing");

        // levels
        var levels = r.GetProperty("levels");
        foreach (var key in new[] { "call_wall", "call_wall_gex", "call_wall_strength",
                                    "distance_to_call_wall_pct",
                                    "put_wall", "put_wall_gex", "put_wall_strength",
                                    "distance_to_put_wall_pct",
                                    "distance_to_magnet_dollars",
                                    "highest_oi_strike", "highest_oi_total",
                                    "max_positive_gamma", "max_negative_gamma",
                                    "level_cluster_score" })
            Assert.True(levels.TryGetProperty(key, out _), $"levels.{key} missing");

        // liquidity (new section)
        var liquidity = r.GetProperty("liquidity");
        foreach (var key in new[] { "atm_spread_pct", "weighted_spread_pct", "execution_score" })
            Assert.True(liquidity.TryGetProperty(key, out _), $"liquidity.{key} missing");

        // metadata (new section)
        var metadata = r.GetProperty("metadata");
        foreach (var key in new[] { "snapshot_age_seconds", "chain_contract_count",
                                    "data_quality_score", "greek_smoothness_score" })
            Assert.True(metadata.TryGetProperty(key, out _), $"metadata.{key} missing");

        // per-strike entries
        var strikes = r.GetProperty("strikes");
        Assert.Equal(JsonValueKind.Array, strikes.ValueKind);
        if (strikes.GetArrayLength() > 0)
        {
            var s = strikes[0];
            foreach (var key in new[] { "strike", "distance_from_spot_pct",
                                        "call_symbol", "put_symbol",
                                        "call_gex", "put_gex", "net_gex",
                                        "call_dex", "put_dex", "net_dex",
                                        "net_vex", "net_chex",
                                        "call_oi", "put_oi", "call_volume", "put_volume",
                                        "gex_share_pct", "oi_share_pct", "volume_share_pct",
                                        "call_iv", "put_iv",
                                        "call_delta", "put_delta",
                                        "call_gamma", "put_gamma",
                                        "call_theta", "put_theta",
                                        "call_mid", "put_mid",
                                        "call_spread_pct", "put_spread_pct" })
                Assert.True(s.TryGetProperty(key, out _), $"strikes[0].{key} missing");
        }
    }

    [LiveFact]
    public async Task Volatility_SPY_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.VolatilityAsync("SPY");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    // ── Max Pain ──────────────────────────────────────────────────────────

    [LiveFact]
    public async Task MaxPain_SPY_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.MaxPainAsync("SPY");
        Assert.True(result.TryGetProperty("max_pain_strike", out _));
        Assert.True(result.TryGetProperty("pain_curve", out _));
        Assert.True(result.TryGetProperty("dealer_alignment", out _));
        Assert.True(result.TryGetProperty("pin_probability", out _));
    }

    [LiveFact]
    public async Task MaxPain_SPY_FieldsAreValid()
    {
        using var client = CreateClient();
        var result = await client.MaxPainAsync("SPY");
        var direction = result.GetProperty("distance").GetProperty("direction").GetString();
        Assert.Contains(direction, new[] { "above", "below", "at" });
        var signal = result.GetProperty("signal").GetString();
        Assert.Contains(signal, new[] { "bullish", "bearish", "neutral" });
    }

    [LiveFact]
    public async Task MaxPain_SPY_WithoutExpiration_HasMultiExpiry()
    {
        using var client = CreateClient();
        var result = await client.MaxPainAsync("SPY");
        if (result.TryGetProperty("max_pain_by_expiration", out var calendar) && calendar.ValueKind == JsonValueKind.Array)
        {
            Assert.True(calendar.GetArrayLength() > 0);
        }
    }

    // ── Screener ──────────────────────────────────────────────────────────

    [LiveFact]
    public async Task Screener_Empty_ReturnsMetaAndData()
    {
        using var client = CreateClient();
        var result = await client.ScreenerAsync(new ScreenerRequest());
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
        Assert.True(result.TryGetProperty("meta", out _));
        Assert.True(result.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }

    [LiveFact]
    public async Task Screener_SimpleFilter_ReturnsFilteredRows()
    {
        using var client = CreateClient();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf
            {
                Field = "regime",
                Operator = "in",
                Value = new[] { "positive_gamma", "negative_gamma" },
            },
            Select = new System.Collections.Generic.List<string> { "symbol", "regime", "price" },
            Limit = 5,
        };
        var result = await client.ScreenerAsync(request);
        Assert.True(result.TryGetProperty("data", out var data));
        foreach (var row in data.EnumerateArray())
        {
            var regime = row.GetProperty("regime").GetString();
            Assert.Contains(regime, new[] { "positive_gamma", "negative_gamma" });
        }
    }

    [LiveFact]
    public async Task Screener_AndGroup_WithSort_RespectsSortOrder()
    {
        using var client = CreateClient();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerGroup
            {
                Op = "and",
                Conditions = new System.Collections.Generic.List<object>
                {
                    new ScreenerLeaf { Field = "atm_iv", Operator = "gte", Value = 0 },
                    new ScreenerLeaf { Field = "atm_iv", Operator = "lte", Value = 500 },
                },
            },
            Sort = new System.Collections.Generic.List<ScreenerSort>
            {
                new() { Field = "atm_iv", Direction = "desc" },
            },
            Select = new System.Collections.Generic.List<string> { "symbol", "atm_iv" },
            Limit = 5,
        };
        var result = await client.ScreenerAsync(request);
        Assert.True(result.GetProperty("meta").GetProperty("returned_count").GetInt32() <= 5);

        double? prev = null;
        foreach (var row in result.GetProperty("data").EnumerateArray())
        {
            if (!row.TryGetProperty("atm_iv", out var ivProp) || ivProp.ValueKind == JsonValueKind.Null)
                continue;
            var iv = ivProp.GetDouble();
            if (prev.HasValue) Assert.True(iv <= prev.Value);
            prev = iv;
        }
    }

    [LiveFact]
    public async Task Screener_Between_ReturnsData()
    {
        using var client = CreateClient();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf { Field = "atm_iv", Operator = "between", Value = new[] { 0, 500 } },
            Limit = 3,
        };
        var result = await client.ScreenerAsync(request);
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
    }

    [LiveFact]
    public async Task Screener_SelectStar_ReturnsFullObject()
    {
        using var client = CreateClient();
        var request = new ScreenerRequest
        {
            Select = new System.Collections.Generic.List<string> { "*" },
            Limit = 1,
        };
        var result = await client.ScreenerAsync(request);
        var data = result.GetProperty("data");
        if (data.GetArrayLength() > 0)
        {
            var row = data[0];
            Assert.True(row.TryGetProperty("symbol", out _));
            Assert.True(row.TryGetProperty("price", out _));
        }
    }

    [LiveFact]
    public async Task Screener_LimitRespected()
    {
        using var client = CreateClient();
        var request = new ScreenerRequest { Limit = 3 };
        var result = await client.ScreenerAsync(request);
        Assert.True(result.GetProperty("meta").GetProperty("returned_count").GetInt32() <= 3);
        Assert.True(result.GetProperty("data").GetArrayLength() <= 3);
    }

    [LiveFact]
    public async Task Screener_InvalidField_ThrowsError()
    {
        using var client = CreateClient();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf { Field = "not_a_real_field_xyz", Operator = "eq", Value = 1 },
        };
        await Assert.ThrowsAsync<FlashAlphaException>(() => client.ScreenerAsync(request));
    }

    /// <summary>
    /// This test does NOT require a valid key because it expects a 401 back.
    /// </summary>
    [Fact]
    public async Task InvalidApiKey_Throws401()
    {
        using var client = new FlashAlphaClient("deliberately-invalid-key");
        await Assert.ThrowsAsync<AuthenticationException>(() => client.AccountAsync());
    }

    // ── Customer regression tests ─────────────────────────────────────────
    //
    // Each test below replays one of the bugs an Alpha-tier user hit during
    // integration of an automated trading daemon, and asserts the SDK's
    // public surface now behaves correctly. All tests call PUBLIC SDK
    // methods only — no raw HttpClient, no mocking, no reflection into
    // private state.

    // Issue #5 — SDK was missing Vrp(). Customer had to hand-roll a REST
    // fallback. The method is now on the client.

    [LiveFact]
    public void Vrp_MethodExistsOnClient()
    {
        using var client = CreateClient();
        var t = typeof(FlashAlphaClient);
        Assert.NotNull(t.GetMethod(nameof(FlashAlphaClient.VrpAsync)));
    }

    [LiveFact]
    public async Task Vrp_ReturnsFullPayload()
    {
        using var client = CreateClient();
        var r = await client.VrpAsync("SPY");

        // Top-level scalars
        Assert.Equal("SPY", r.GetProperty("symbol").GetString());
        Assert.True(r.GetProperty("underlying_price").GetDouble() > 0);
        Assert.False(string.IsNullOrEmpty(r.GetProperty("as_of").GetString()));
        Assert.Contains(r.GetProperty("market_open").ValueKind, new[] { JsonValueKind.True, JsonValueKind.False });
        Assert.Equal(JsonValueKind.Number, r.GetProperty("net_harvest_score").ValueKind);
        Assert.Equal(JsonValueKind.Number, r.GetProperty("dealer_flow_risk").ValueKind);

        // result["vrp"] — core metrics
        var core = r.GetProperty("vrp");
        foreach (var key in new[] { "atm_iv", "rv_5d", "rv_10d", "rv_20d", "rv_30d",
                                    "vrp_5d", "vrp_10d", "vrp_20d", "vrp_30d",
                                    "z_score", "percentile", "history_days" })
        {
            Assert.True(core.TryGetProperty(key, out _), $"vrp.{key} missing");
        }
        Assert.Equal(JsonValueKind.Number, core.GetProperty("z_score").ValueKind);
        var pct = core.GetProperty("percentile").GetInt32();
        Assert.InRange(pct, 0, 100);

        // result["directional"] — skew metrics
        var d = r.GetProperty("directional");
        foreach (var key in new[] { "put_wing_iv_25d", "call_wing_iv_25d",
                                    "downside_rv_20d", "upside_rv_20d",
                                    "downside_vrp", "upside_vrp" })
        {
            Assert.True(d.TryGetProperty(key, out _), $"directional.{key} missing");
        }

        // result["regime"] — regime snapshot
        var reg = r.GetProperty("regime");
        var gamma = reg.GetProperty("gamma").GetString();
        Assert.Contains(gamma, new[] { "positive_gamma", "negative_gamma", "neutral" });
        Assert.Equal(JsonValueKind.Number, reg.GetProperty("net_gex").ValueKind);
        Assert.True(reg.TryGetProperty("vrp_regime", out _));

        // result["term_vrp"] — array of term-structure points
        var term = r.GetProperty("term_vrp");
        Assert.Equal(JsonValueKind.Array, term.ValueKind);
        if (term.GetArrayLength() > 0)
        {
            var pt = term[0];
            foreach (var key in new[] { "dte", "iv", "rv", "vrp" })
                Assert.True(pt.TryGetProperty(key, out _), $"term_vrp[0].{key} missing");
        }

        // result["gex_conditioned"] — nullable harvest scoring
        if (r.TryGetProperty("gex_conditioned", out var gc) && gc.ValueKind == JsonValueKind.Object)
        {
            Assert.True(gc.TryGetProperty("harvest_score", out var hs));
            Assert.Equal(JsonValueKind.Number, hs.ValueKind);
            Assert.True(gc.TryGetProperty("regime", out _));
            Assert.True(gc.TryGetProperty("interpretation", out _));
        }

        // result["strategy_scores"] — nullable 0-100 scores
        if (r.TryGetProperty("strategy_scores", out var ss) && ss.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "short_put_spread", "short_strangle",
                                        "iron_condor", "calendar_spread" })
            {
                if (ss.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number)
                {
                    var score = v.GetDouble();
                    Assert.InRange(score, 0, 100);
                }
            }
        }

        // result["macro"] — optional macro context
        if (r.TryGetProperty("macro", out var macro) && macro.ValueKind == JsonValueKind.Object)
        {
            Assert.True(macro.TryGetProperty("vix", out _));
        }
    }

    // Issue #1 — Nested response structures. Customer accessed
    // result["z_score"] directly and got null. Field lives at result["vrp"]["z_score"].

    [LiveFact]
    public async Task Vrp_CoreMetrics_AreNested_NotTopLevel()
    {
        using var client = CreateClient();
        var r = await client.VrpAsync("SPY");
        Assert.False(r.TryGetProperty("z_score", out _), "z_score must NOT be top-level (customer trap)");
        Assert.False(r.TryGetProperty("percentile", out _));
        Assert.False(r.TryGetProperty("atm_iv", out _));
        var core = r.GetProperty("vrp");
        foreach (var key in new[] { "z_score", "percentile", "atm_iv", "rv_20d", "vrp_20d" })
            Assert.True(core.TryGetProperty(key, out _), $"vrp.{key} missing");
    }

    [LiveFact]
    public async Task Vrp_HarvestScore_IsUnderGexConditioned()
    {
        using var client = CreateClient();
        var r = await client.VrpAsync("SPY");
        Assert.False(r.TryGetProperty("harvest_score", out _), "harvest_score must NOT be top-level");
        if (r.TryGetProperty("gex_conditioned", out var gc) && gc.ValueKind == JsonValueKind.Object)
        {
            Assert.True(gc.TryGetProperty("harvest_score", out _));
            Assert.True(gc.TryGetProperty("regime", out _));
        }
    }

    [LiveFact]
    public async Task Vrp_NetGex_IsUnderRegime()
    {
        using var client = CreateClient();
        var r = await client.VrpAsync("SPY");
        Assert.False(r.TryGetProperty("net_gex", out _), "net_gex must NOT be top-level on vrp");
        Assert.False(r.TryGetProperty("gamma_flip", out _));
        var reg = r.GetProperty("regime");
        Assert.True(reg.TryGetProperty("net_gex", out _));
        Assert.True(reg.TryGetProperty("gamma", out _));
    }

    [LiveFact]
    public async Task Vrp_CompositeScores_AreTopLevel()
    {
        using var client = CreateClient();
        var r = await client.VrpAsync("SPY");
        Assert.True(r.TryGetProperty("net_harvest_score", out _));
        Assert.True(r.TryGetProperty("dealer_flow_risk", out _));
    }

    [LiveFact]
    public async Task ExposureSummary_NetGex_IsUnderExposures()
    {
        using var client = CreateClient();
        var r = await client.ExposureSummaryAsync("SPY");
        Assert.Equal("SPY", r.GetProperty("symbol").GetString());
        Assert.False(r.TryGetProperty("net_gex", out _), "net_gex must NOT be top-level (customer trap)");

        var exp = r.GetProperty("exposures");
        Assert.True(exp.TryGetProperty("net_gex", out var ng));
        Assert.Equal(JsonValueKind.Number, ng.ValueKind);
        foreach (var key in new[] { "net_dex", "net_vex", "net_chex" })
        {
            if (exp.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null)
                Assert.Equal(JsonValueKind.Number, v.ValueKind);
        }

        Assert.True(r.TryGetProperty("regime", out var regimeProp));
        Assert.Contains(regimeProp.GetString(), new[] { "positive_gamma", "negative_gamma", "neutral" });
    }

    // Issue #2 — Field naming. Customer used put_vrp/call_vrp. Canonical
    // names are downside_vrp/upside_vrp.

    [LiveFact]
    public async Task Vrp_Directional_UsesDownsideUpsideNames()
    {
        using var client = CreateClient();
        var d = (await client.VrpAsync("SPY")).GetProperty("directional");
        Assert.True(d.TryGetProperty("downside_vrp", out _));
        Assert.True(d.TryGetProperty("upside_vrp", out _));
        Assert.True(d.TryGetProperty("put_wing_iv_25d", out _));
        Assert.True(d.TryGetProperty("call_wing_iv_25d", out _));
        Assert.False(d.TryGetProperty("put_vrp", out _));
        Assert.False(d.TryGetProperty("call_vrp", out _));
    }

    // Issue #3 — URL pattern mix. Customer guessed /v1/summary/{sym} and
    // got a silent 404. The SDK methods route to canonical paths.

    [LiveFact]
    public async Task StockSummary_MethodRoutesCorrectly()
    {
        using var client = CreateClient();
        var r = await client.StockSummaryAsync("SPY");
        Assert.Equal("SPY", r.GetProperty("symbol").GetString());
        Assert.True(r.TryGetProperty("price", out _));
    }

    [LiveFact]
    public async Task StockQuote_MethodRoutesCorrectly()
    {
        using var client = CreateClient();
        var r = await client.StockQuoteAsync("SPY");
        Assert.Equal("SPY", r.GetProperty("ticker").GetString());
    }

    [LiveFact]
    public async Task AllExposureMethods_RouteCorrectly()
    {
        using var client = CreateClient();
        Assert.Equal("SPY", (await client.GexAsync("SPY")).GetProperty("symbol").GetString());
        Assert.Equal("SPY", (await client.DexAsync("SPY")).GetProperty("symbol").GetString());
        Assert.Equal("SPY", (await client.VexAsync("SPY")).GetProperty("symbol").GetString());
        Assert.Equal("SPY", (await client.ChexAsync("SPY")).GetProperty("symbol").GetString());
        Assert.Equal("SPY", (await client.ExposureSummaryAsync("SPY")).GetProperty("symbol").GetString());
        Assert.Equal("SPY", (await client.ExposureLevelsAsync("SPY")).GetProperty("symbol").GetString());
    }

    [LiveFact]
    public async Task Vrp_MethodRoutesCorrectly()
    {
        using var client = CreateClient();
        var r = await client.VrpAsync("SPY");
        Assert.Equal("SPY", r.GetProperty("symbol").GetString());
    }

    // Issue #4 — Screener URL. SDK's ScreenerAsync POSTs to /v1/screener
    // (canonical since v0.3.1).

    [LiveFact]
    public async Task Screener_ReturnsValidEnvelope()
    {
        using var client = CreateClient();
        var r = await client.ScreenerAsync(new ScreenerRequest { Limit = 5 });
        Assert.True(r.TryGetProperty("meta", out var meta));
        Assert.True(r.TryGetProperty("data", out _));
        foreach (var key in new[] { "total_count", "returned_count", "universe_size", "tier", "as_of" })
            Assert.True(meta.TryGetProperty(key, out _), $"meta.{key} missing");
        Assert.True(meta.GetProperty("returned_count").GetInt32() <= 5);
        Assert.Contains(meta.GetProperty("tier").GetString(), new[] { "growth", "alpha" });
    }

    [LiveFact]
    public async Task Screener_FullRow_IsReadable()
    {
        using var client = CreateClient();
        var r = await client.ScreenerAsync(new ScreenerRequest
        {
            Select = new System.Collections.Generic.List<string> { "*" },
            Limit = 1,
        });
        var data = r.GetProperty("data");
        if (data.GetArrayLength() == 0) return; // no rows in universe
        var row = data[0];
        foreach (var key in new[] { "symbol", "price", "regime" })
            Assert.True(row.TryGetProperty(key, out _), $"row missing {key}");
    }
}
