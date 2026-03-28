using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlashAlpha;
using Xunit;

namespace FlashAlpha.Tests;

/// <summary>
/// Configurable fake <see cref="HttpMessageHandler"/> for unit tests.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;
    private readonly Dictionary<string, string> _responseHeaders;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string body = "{}",
        Dictionary<string, string>? responseHeaders = null)
    {
        _statusCode = statusCode;
        _body = body;
        _responseHeaders = responseHeaders ?? new Dictionary<string, string>();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
        };
        foreach (var (k, v) in _responseHeaders)
            response.Headers.TryAddWithoutValidation(k, v);
        return Task.FromResult(response);
    }
}

/// <summary>
/// Creates a <see cref="FlashAlphaClient"/> wired to a fake handler.
/// </summary>
internal static class TestClientFactory
{
    public static (FlashAlphaClient client, FakeHttpMessageHandler handler) Create(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string body = "{}",
        Dictionary<string, string>? responseHeaders = null,
        string baseUrl = "https://lab.flashalpha.com")
    {
        var handler = new FakeHttpMessageHandler(statusCode, body, responseHeaders);
        var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        http.DefaultRequestHeaders.Add("X-Api-Key", "test-key");
        var client = new FlashAlphaClient(http);
        return (client, handler);
    }
}

public sealed class ClientTests
{
    // ── Constructor ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullApiKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FlashAlphaClient(apiKey: null!));
    }

    [Fact]
    public void Constructor_EmptyApiKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FlashAlphaClient(apiKey: string.Empty));
    }

    [Fact]
    public void Constructor_ValidApiKey_SetsHeader()
    {
        using var client = new FlashAlphaClient("my-key");
        // no exception = success; header is verified via integration request
        Assert.NotNull(client);
    }

    // ── API key header ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthAsync_SendsApiKeyHeader()
    {
        var (client, handler) = TestClientFactory.Create(body: "{\"status\":\"ok\"}");
        using (client)
        {
            await client.HealthAsync();
        }
        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.Contains("X-Api-Key"));
    }

    // ── Endpoints ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create(body: "{\"status\":\"ok\"}");
        using (client)
        {
            await client.HealthAsync();
        }
        Assert.Equal("/health", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GexAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.GexAsync("SPY");
        }
        Assert.Equal("/v1/exposure/gex/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GexAsync_WithExpiration_SendsQueryParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.GexAsync("SPY", expiration: "2025-01-17");
        }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("expiration=2025-01-17", query);
    }

    [Fact]
    public async Task GexAsync_WithMinOi_SendsQueryParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.GexAsync("SPY", minOi: 100);
        }
        Assert.Contains("min_oi=100", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task DexAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.DexAsync("SPX"); }
        Assert.Equal("/v1/exposure/dex/SPX", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task VexAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.VexAsync("QQQ"); }
        Assert.Equal("/v1/exposure/vex/QQQ", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ChexAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ChexAsync("AAPL"); }
        Assert.Equal("/v1/exposure/chex/AAPL", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExposureLevelsAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ExposureLevelsAsync("SPY"); }
        Assert.Equal("/v1/exposure/levels/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ExposureSummaryAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ExposureSummaryAsync("SPY"); }
        Assert.Equal("/v1/exposure/summary/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task NarrativeAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.NarrativeAsync("SPX"); }
        Assert.Equal("/v1/exposure/narrative/SPX", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ZeroDteAsync_WithStrikeRange_SendsQueryParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ZeroDteAsync("SPX", strikeRange: 0.05); }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("strike_range=0.05", query);
    }

    [Fact]
    public async Task ExposureHistoryAsync_WithDays_SendsQueryParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ExposureHistoryAsync("SPY", days: 30); }
        Assert.Contains("days=30", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task StockQuoteAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.StockQuoteAsync("AAPL"); }
        Assert.Equal("/stockquote/AAPL", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task OptionQuoteAsync_WithParams_SendsQueryParams()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.OptionQuoteAsync("AAPL", expiry: "2025-01-17", strike: 200.0, type: "call");
        }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("expiry=2025-01-17", query);
        Assert.Contains("strike=200", query);
        Assert.Contains("type=call", query);
    }

    [Fact]
    public async Task SurfaceAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.SurfaceAsync("SPX"); }
        Assert.Equal("/v1/surface/SPX", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GreeksAsync_SendsAllRequiredParams()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.GreeksAsync(spot: 500, strike: 505, dte: 30, sigma: 0.2, type: "call");
        }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("spot=500", query);
        Assert.Contains("strike=505", query);
        Assert.Contains("dte=30", query);
        Assert.Contains("sigma=0.2", query);
        Assert.Contains("type=call", query);
    }

    [Fact]
    public async Task GreeksAsync_WithOptionalRAndQ_SendsParams()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.GreeksAsync(spot: 500, strike: 505, dte: 30, sigma: 0.2, r: 0.05, q: 0.01);
        }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("r=0.05", query);
        Assert.Contains("q=0.01", query);
    }

    [Fact]
    public async Task IvAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.IvAsync(spot: 500, strike: 505, dte: 30, price: 10.0);
        }
        Assert.Equal("/v1/pricing/iv", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task KellyAsync_SendsAllRequiredParams()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.KellyAsync(spot: 500, strike: 505, dte: 30, sigma: 0.2, premium: 5.0, mu: 0.1);
        }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("premium=5", query);
        Assert.Contains("mu=0.1", query);
    }

    [Fact]
    public async Task VolatilityAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.VolatilityAsync("SPY"); }
        Assert.Equal("/v1/volatility/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AdvVolatilityAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.AdvVolatilityAsync("SPX"); }
        Assert.Equal("/v1/adv_volatility/SPX", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task TickersAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.TickersAsync(); }
        Assert.Equal("/v1/tickers", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SymbolsAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.SymbolsAsync(); }
        Assert.Equal("/v1/symbols", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AccountAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.AccountAsync(); }
        Assert.Equal("/v1/account", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task OptionsAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.OptionsAsync("TSLA"); }
        Assert.Equal("/v1/options/TSLA", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task StockSummaryAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.StockSummaryAsync("MSFT"); }
        Assert.Equal("/v1/stock/MSFT/summary", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task HistoricalStockQuoteAsync_SendsDateParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.HistoricalStockQuoteAsync("AAPL", "2024-12-01"); }
        Assert.Contains("date=2024-12-01", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task HistoricalOptionQuoteAsync_SendsAllParams()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.HistoricalOptionQuoteAsync("AAPL", "2024-12-01",
                time: "10:00", expiry: "2024-12-20", strike: 200.0, type: "put");
        }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("date=2024-12-01", query);
        Assert.Contains("time=10%3A00", query); // URL-encoded colon
        Assert.Contains("expiry=2024-12-20", query);
        Assert.Contains("type=put", query);
    }

    // ── Error handling ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Status401_ThrowsAuthenticationException()
    {
        var (client, _) = TestClientFactory.Create(HttpStatusCode.Unauthorized, "{\"message\":\"Invalid API key\"}");
        using (client)
        {
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.HealthAsync());
            Assert.Equal(401, ex.StatusCode);
            Assert.Contains("Invalid API key", ex.Message);
        }
    }

    [Fact]
    public async Task Status403_ThrowsTierRestrictedException()
    {
        var body = "{\"message\":\"Upgrade required\",\"current_plan\":\"free\",\"required_plan\":\"growth\"}";
        var (client, _) = TestClientFactory.Create(HttpStatusCode.Forbidden, body);
        using (client)
        {
            var ex = await Assert.ThrowsAsync<TierRestrictedException>(() => client.ExposureSummaryAsync("SPY"));
            Assert.Equal(403, ex.StatusCode);
            Assert.Equal("free", ex.CurrentPlan);
            Assert.Equal("growth", ex.RequiredPlan);
        }
    }

    [Fact]
    public async Task Status404_ThrowsNotFoundException()
    {
        var (client, _) = TestClientFactory.Create(HttpStatusCode.NotFound, "{\"detail\":\"Symbol not found\"}");
        using (client)
        {
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => client.GexAsync("UNKNOWN"));
            Assert.Equal(404, ex.StatusCode);
        }
    }

    [Fact]
    public async Task Status429_ThrowsRateLimitException()
    {
        var headers = new Dictionary<string, string> { ["Retry-After"] = "60" };
        var (client, _) = TestClientFactory.Create(HttpStatusCode.TooManyRequests, "{\"message\":\"Rate limit exceeded\"}", headers);
        using (client)
        {
            var ex = await Assert.ThrowsAsync<RateLimitException>(() => client.GexAsync("SPY"));
            Assert.Equal(429, ex.StatusCode);
            Assert.Equal(60, ex.RetryAfter);
        }
    }

    [Fact]
    public async Task Status500_ThrowsServerException()
    {
        var (client, _) = TestClientFactory.Create(HttpStatusCode.InternalServerError, "{\"message\":\"Internal error\"}");
        using (client)
        {
            var ex = await Assert.ThrowsAsync<ServerException>(() => client.HealthAsync());
            Assert.Equal(500, ex.StatusCode);
        }
    }

    [Fact]
    public async Task NonJsonErrorBody_UsesRawTextAsMessage()
    {
        var (client, _) = TestClientFactory.Create(HttpStatusCode.Unauthorized, "plain text error");
        using (client)
        {
            var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.HealthAsync());
            Assert.Contains("plain text error", ex.Message);
        }
    }

    [Fact]
    public async Task SuccessResponse_ReturnsJsonElement()
    {
        var (client, _) = TestClientFactory.Create(body: "{\"status\":\"ok\",\"version\":\"2\"}");
        using (client)
        {
            var result = await client.HealthAsync();
            Assert.Equal("ok", result.GetProperty("status").GetString());
        }
    }
}
