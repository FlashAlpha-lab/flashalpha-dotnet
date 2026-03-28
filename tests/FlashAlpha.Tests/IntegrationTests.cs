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
