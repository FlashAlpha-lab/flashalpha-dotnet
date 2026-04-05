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

    [LiveFact]
    public async Task Volatility_SPY_ReturnsData()
    {
        using var client = CreateClient();
        var result = await client.VolatilityAsync("SPY");
        Assert.Equal(JsonValueKind.Object, result.ValueKind);
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
}
