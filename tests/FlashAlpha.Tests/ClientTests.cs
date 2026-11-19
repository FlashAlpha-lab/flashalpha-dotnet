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
    public async Task FlowZeroDteSnapshotAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.FlowZeroDteSnapshotAsync("SPY"); }
        Assert.Equal("/v1/flow/zero-dte/snapshot/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
        // No expiry passed => no query string.
        Assert.Equal(string.Empty, handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task FlowZeroDteSnapshotAsync_WithExpiry_SendsQueryParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.FlowZeroDteSnapshotAsync("SPY", expiry: "2026-06-14"); }
        Assert.Equal("/v1/flow/zero-dte/snapshot/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("expiry=2026-06-14", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task FlowZeroDteLeaderboardAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.FlowZeroDteLeaderboardAsync(); }
        Assert.Equal("/v1/flow/zero-dte/leaderboard", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task FlowZeroDteLeaderboardAsync_WithParams_SendsQueryParams()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.FlowZeroDteLeaderboardAsync(metric: "pin_risk", n: 25); }
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("metric=pin_risk", query);
        Assert.Contains("n=25", query);
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
    public async Task RealizedVolatilityAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.RealizedVolatilityAsync("AAPL"); }
        Assert.Equal("/v1/volatility/realized/AAPL", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RealizedVolatilityTypedAsync_ParsesEstimators()
    {
        var body = "{\"symbol\":\"AAPL\",\"underlying_price\":201.5,\"estimators\":{\"yang_zhang\":{\"rv10\":17.0,\"rv20\":19.7,\"rv30\":20.3}}}";
        var (client, _) = TestClientFactory.Create(body: body);
        using (client)
        {
            var result = await client.RealizedVolatilityTypedAsync("AAPL");
            Assert.Equal("AAPL", result!.Symbol);
            Assert.Equal(17.0, result.Estimators!.YangZhang!.Rv10);
            Assert.Equal(20.3, result.Estimators.YangZhang.Rv30);
        }
    }

    [Fact]
    public async Task VolatilityForecastAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.VolatilityForecastAsync("AAPL"); }
        Assert.Equal("/v1/volatility/forecast/AAPL", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task VolatilityForecastAsync_WithDist_SendsQueryParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.VolatilityForecastAsync("AAPL", dist: "gaussian"); }
        Assert.Contains("dist=gaussian", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task VolatilityForecastAsync_OmitsDistWhenNull()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.VolatilityForecastAsync("AAPL"); }
        Assert.DoesNotContain("dist", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task VolatilityForecastTypedAsync_ParsesGarch()
    {
        var body = "{\"symbol\":\"AAPL\",\"garch\":{\"model\":\"garch_1_1\",\"distribution\":\"student_t\",\"params\":{\"omega\":0.000003,\"alpha\":0.078,\"beta\":0.905,\"dof\":6.2},\"persistence\":0.983,\"converged\":true,\"forecast\":[{\"horizon_days\":1,\"vol_annualized\":20.1}]}}";
        var (client, _) = TestClientFactory.Create(body: body);
        using (client)
        {
            var result = await client.VolatilityForecastTypedAsync("AAPL");
            Assert.Equal("garch_1_1", result!.Garch!.Model);
            Assert.Equal(6.2, result.Garch.Params!.Dof);
            Assert.True(result.Garch.Converged);
            Assert.Equal(1, result.Garch.Forecast![0].HorizonDays);
        }
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
    public async Task MaxPainAsync_CallsCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.MaxPainAsync("SPY"); }
        Assert.Equal("/v1/maxpain/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("GET", handler.LastRequest.Method.Method);
    }

    [Fact]
    public async Task MaxPainAsync_SendsExpirationParam()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.MaxPainAsync("SPY", expiration: "2026-04-17"); }
        Assert.Contains("expiration=2026-04-17", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task MaxPainAsync_OmitsExpirationWhenNull()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.MaxPainAsync("SPY"); }
        Assert.DoesNotContain("expiration", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task MaxPainAsync_ParsesResponse()
    {
        var body = "{\"max_pain_strike\":545,\"pin_probability\":68,\"dealer_alignment\":{\"alignment\":\"converging\"}}";
        var (client, _) = TestClientFactory.Create(body: body);
        using (client)
        {
            var result = await client.MaxPainAsync("SPY");
            Assert.Equal(545, result.GetProperty("max_pain_strike").GetInt32());
            Assert.Equal(68, result.GetProperty("pin_probability").GetInt32());
            Assert.Equal("converging", result.GetProperty("dealer_alignment").GetProperty("alignment").GetString());
        }
    }

    [Fact]
    public async Task MaxPainAsync_Throws403TierRestricted()
    {
        var errBody = "{\"status\":\"ERROR\",\"error\":\"tier_restricted\",\"message\":\"Requires Growth.\",\"current_plan\":\"Free\",\"required_plan\":\"Growth\"}";
        var (client, _) = TestClientFactory.Create(statusCode: System.Net.HttpStatusCode.Forbidden, body: errBody);
        using (client)
        {
            var ex = await Assert.ThrowsAsync<TierRestrictedException>(() => client.MaxPainAsync("SPY"));
            Assert.Equal("Free", ex.CurrentPlan);
        }
    }

    [Fact]
    public async Task ScreenerAsync_PostsToCorrectPath()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ScreenerAsync(new ScreenerRequest()); }
        Assert.Equal("/v1/screener", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("POST", handler.LastRequest.Method.Method);
    }

    [Fact]
    public async Task ScreenerAsync_SerializesFiltersAndLimit()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerGroup
            {
                Op = "and",
                Conditions = new System.Collections.Generic.List<object>
                {
                    new ScreenerLeaf { Field = "regime", Operator = "eq", Value = "positive_gamma" },
                    new ScreenerLeaf { Field = "harvest_score", Operator = "gte", Value = 65 },
                },
            },
            Sort = new System.Collections.Generic.List<ScreenerSort>
            {
                new() { Field = "harvest_score", Direction = "desc" },
            },
            Select = new System.Collections.Generic.List<string> { "symbol", "price", "harvest_score" },
            Limit = 20,
        };
        using (client) { await client.ScreenerAsync(request); }

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"op\":\"and\"", body);
        Assert.Contains("positive_gamma", body);
        Assert.Contains("\"limit\":20", body);
        Assert.Contains("harvest_score", body);
    }

    [Fact]
    public async Task ScreenerAsync_WithRawObject_SerializesCorrectly()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client)
        {
            await client.ScreenerAsync((object)new { limit = 5, select = new[] { "symbol" } });
        }

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"limit\":5", body);
        Assert.Contains("symbol", body);
    }

    [Fact]
    public async Task ScreenerAsync_SendsJsonContentType()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ScreenerAsync(new ScreenerRequest()); }
        Assert.Equal("application/json", handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task ScreenerAsync_EmptyRequestSerializesToEmptyObject()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ScreenerAsync(new ScreenerRequest()); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        // With null-ignoring serializer, empty request -> "{}"
        Assert.Equal("{}", body);
    }

    [Fact]
    public async Task ScreenerAsync_LeafFilter()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf { Field = "regime", Operator = "eq", Value = "positive_gamma" },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"field\":\"regime\"", body);
        Assert.Contains("\"operator\":\"eq\"", body);
        Assert.Contains("positive_gamma", body);
    }

    [Fact]
    public async Task ScreenerAsync_OrGroup()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerGroup
            {
                Op = "or",
                Conditions = new System.Collections.Generic.List<object>
                {
                    new ScreenerLeaf { Field = "vrp_regime", Operator = "eq", Value = "toxic_short_vol" },
                    new ScreenerLeaf { Field = "vrp_regime", Operator = "eq", Value = "event_only" },
                },
            },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"op\":\"or\"", body);
        Assert.Contains("toxic_short_vol", body);
    }

    [Fact]
    public async Task ScreenerAsync_NestedAndInsideOr()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerGroup
            {
                Op = "or",
                Conditions = new System.Collections.Generic.List<object>
                {
                    new ScreenerGroup
                    {
                        Op = "and",
                        Conditions = new System.Collections.Generic.List<object>
                        {
                            new ScreenerLeaf { Field = "regime", Operator = "eq", Value = "positive_gamma" },
                            new ScreenerLeaf { Field = "harvest_score", Operator = "gte", Value = 70 },
                        },
                    },
                    new ScreenerGroup
                    {
                        Op = "and",
                        Conditions = new System.Collections.Generic.List<object>
                        {
                            new ScreenerLeaf { Field = "regime", Operator = "eq", Value = "negative_gamma" },
                            new ScreenerLeaf { Field = "atm_iv", Operator = "gte", Value = 50 },
                        },
                    },
                },
            },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"op\":\"or\"", body);
        Assert.Contains("\"op\":\"and\"", body);
        Assert.Contains("positive_gamma", body);
        Assert.Contains("negative_gamma", body);
    }

    [Fact]
    public async Task ScreenerAsync_BetweenOperator()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf { Field = "atm_iv", Operator = "between", Value = new[] { 15, 25 } },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"operator\":\"between\"", body);
        Assert.Contains("[15,25]", body);
    }

    [Fact]
    public async Task ScreenerAsync_InOperator()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf
            {
                Field = "term_state",
                Operator = "in",
                Value = new[] { "contango", "mixed" },
            },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"operator\":\"in\"", body);
        Assert.Contains("contango", body);
        Assert.Contains("mixed", body);
    }

    [Fact]
    public async Task ScreenerAsync_IsNotNullOperator_OmitsValue()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf { Field = "vrp_regime", Operator = "is_not_null" },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"operator\":\"is_not_null\"", body);
        Assert.DoesNotContain("\"value\"", body);
    }

    [Fact]
    public async Task ScreenerAsync_CascadingFilters()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerGroup
            {
                Op = "and",
                Conditions = new System.Collections.Generic.List<object>
                {
                    new ScreenerLeaf { Field = "regime", Operator = "eq", Value = "positive_gamma" },
                    new ScreenerLeaf { Field = "expiries.days_to_expiry", Operator = "lte", Value = 14 },
                    new ScreenerLeaf { Field = "strikes.call_oi", Operator = "gte", Value = 2000 },
                    new ScreenerLeaf { Field = "contracts.type", Operator = "eq", Value = "C" },
                },
            },
            Select = new System.Collections.Generic.List<string> { "*" },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("expiries.days_to_expiry", body);
        Assert.Contains("strikes.call_oi", body);
        Assert.Contains("contracts.type", body);
        Assert.Contains("\"select\":[\"*\"]", body);
    }

    [Fact]
    public async Task ScreenerAsync_Formulas()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Formulas = new System.Collections.Generic.List<ScreenerFormula>
            {
                new() { Alias = "vrp_ratio", Expression = "atm_iv / rv_20d" },
            },
            Filters = new ScreenerLeaf { Formula = "vrp_ratio", Operator = "gte", Value = 1.2 },
            Sort = new System.Collections.Generic.List<ScreenerSort>
            {
                new() { Formula = "vrp_ratio", Direction = "desc" },
            },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"alias\":\"vrp_ratio\"", body);
        Assert.Contains("atm_iv / rv_20d", body);
        Assert.Contains("\"formula\":\"vrp_ratio\"", body);
    }

    [Fact]
    public async Task ScreenerAsync_MultiSort()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Sort = new System.Collections.Generic.List<ScreenerSort>
            {
                new() { Field = "dealer_flow_risk", Direction = "asc" },
                new() { Field = "harvest_score", Direction = "desc" },
            },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("dealer_flow_risk", body);
        Assert.Contains("\"direction\":\"asc\"", body);
        Assert.Contains("\"direction\":\"desc\"", body);
    }

    [Fact]
    public async Task ScreenerAsync_Pagination()
    {
        var (client, handler) = TestClientFactory.Create();
        using (client) { await client.ScreenerAsync(new ScreenerRequest { Limit = 10, Offset = 10 }); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("\"limit\":10", body);
        Assert.Contains("\"offset\":10", body);
    }

    [Fact]
    public async Task ScreenerAsync_NegativeNumber()
    {
        var (client, handler) = TestClientFactory.Create();
        var request = new ScreenerRequest
        {
            Filters = new ScreenerLeaf { Field = "net_gex", Operator = "lt", Value = -500000 },
        };
        using (client) { await client.ScreenerAsync(request); }
        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("-500000", body);
    }

    [Fact]
    public async Task ScreenerAsync_ParsesResponseStructure()
    {
        var responseBody = "{\"meta\":{\"total_count\":7,\"tier\":\"alpha\",\"universe_size\":250},\"data\":[{\"symbol\":\"SPY\",\"price\":656.01}]}";
        var (client, _) = TestClientFactory.Create(body: responseBody);
        using (client)
        {
            var result = await client.ScreenerAsync(new ScreenerRequest());
            Assert.Equal("alpha", result.GetProperty("meta").GetProperty("tier").GetString());
            Assert.Equal(7, result.GetProperty("meta").GetProperty("total_count").GetInt32());
            Assert.Equal(656.01, result.GetProperty("data")[0].GetProperty("price").GetDouble());
        }
    }

    [Fact]
    public async Task ScreenerAsync_Throws_400_ValidationError()
    {
        var errBody = "{\"status\":\"ERROR\",\"error\":\"validation_error\",\"message\":\"Field 'harvest_score' requires the Alpha plan or higher.\"}";
        var (client, _) = TestClientFactory.Create(statusCode: System.Net.HttpStatusCode.BadRequest, body: errBody);
        using (client)
        {
            var request = new ScreenerRequest
            {
                Filters = new ScreenerLeaf { Field = "harvest_score", Operator = "gte", Value = 65 },
            };
            var ex = await Assert.ThrowsAsync<FlashAlphaException>(() => client.ScreenerAsync(request));
            Assert.Contains("Alpha", ex.Message);
        }
    }

    [Fact]
    public async Task ScreenerAsync_Throws_403_TierRestricted()
    {
        var errBody = "{\"status\":\"ERROR\",\"error\":\"tier_restricted\",\"message\":\"Screener requires Growth plan.\",\"current_plan\":\"Free\",\"required_plan\":\"Growth\"}";
        var (client, _) = TestClientFactory.Create(statusCode: System.Net.HttpStatusCode.Forbidden, body: errBody);
        using (client)
        {
            var ex = await Assert.ThrowsAsync<TierRestrictedException>(() => client.ScreenerAsync(new ScreenerRequest()));
            Assert.Equal("Free", ex.CurrentPlan);
            Assert.Equal("Growth", ex.RequiredPlan);
        }
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

    // ── sell-window ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SellWindowTypedAsync_BindsEveryField()
    {
        var (client, handler) = TestClientFactory.Create(body: """{"symbol":"SPY","session":"2026-10-16","spot":772.255,"quoted_at":"2026-10-16T13:35:00Z","legs_indicative":false,"windows":[{"expiry":"2026-10-16","days_to_expiry":0,"action":"sell","candidates":[{"structure":"short_put","exit":"expiry","legs":[{"side":"sell","quantity":1,"type":"put","strike":769,"delta":-0.19,"bid":0.39,"ask":0.4}],"mid_credit":0.395}]},{"expiry":"2026-10-23","days_to_expiry":7,"action":"skip"},{"expiry":"2026-10-30","days_to_expiry":14,"action":"skip"},{"action":"unavailable"}],"endpoint_version":"2026.09.30","data_as_of":{"node":"fa5"}}""");
        using (client)
        {
            var r = await client.SellWindowTypedAsync("SPY");

            Assert.Equal("/v1/strategies/sell-window/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
            Assert.Equal("", handler.LastRequest.RequestUri.Query);
            Assert.NotNull(r);
            Assert.Equal("2026-10-16", r!.Session);
            Assert.Equal(772.255, r.Spot);
            Assert.Equal("2026-10-16T13:35:00Z", r.QuotedAt);
            Assert.False(r.LegsIndicative);
            Assert.Equal(4, r.Windows!.Count);
            Assert.Equal("sell", r.Windows[0].Action);
            Assert.Equal(0, r.Windows[0].DaysToExpiry);
            var trade = r.Windows[0].Candidates![0];
            Assert.Equal("short_put", trade.Structure);
            Assert.Equal("expiry", trade.Exit);
            Assert.Equal(0.395, trade.MidCredit);
            Assert.Equal(769, trade.Legs![0].Strike);
            Assert.Equal(-0.19, trade.Legs[0].Delta);
            Assert.Equal("put", trade.Legs[0].Type);
            Assert.Null(r.Windows[3].Expiry);
            Assert.Equal("2026.09.30", r.EndpointVersion);
            Assert.Equal("fa5", r.DataAsOf!.Node);
        }
    }

    [Fact]
    public async Task SellWindowTypedAsync_SkipDayLeavesOptionalFieldsNull()
    {
        var (client, _) = TestClientFactory.Create(body: """{"symbol":"SPY","session":"2026-11-05","windows":[{"expiry":"2026-11-05","days_to_expiry":0,"action":"skip"}]}""");
        using (client)
        {
            var r = await client.SellWindowTypedAsync("SPY");
            Assert.Null(r!.Spot);
            Assert.Null(r.LegsIndicative);
            Assert.Null(r.Windows![0].Candidates);
        }
    }

    // ── expiry impact and strike dependency ──────────────────────────────────

    /// <summary>Real production responses captured 2026-11-19 (lists trimmed).</summary>
    private static string Fixture(string name)
        => System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task ExpiryImpactAsync_OmitsUnsetOptionalParameters()
    {
        var (client, handler) = TestClientFactory.Create(body: Fixture("expiry_impact_live.json"));
        using (client)
        {
            await client.ExpiryImpactAsync("SPY");
            Assert.Equal("/v1/exposure/expiry-impact/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
            Assert.Equal("", handler.LastRequest.RequestUri.Query);
        }
    }

    [Fact]
    public async Task ExpiryImpactAsync_SendsExpiryAndLimit()
    {
        var (client, handler) = TestClientFactory.Create(body: Fixture("expiry_impact_live.json"));
        using (client)
        {
            await client.ExpiryImpactAsync("QQQ", expiry: "2026-11-20", limit: 3);
            Assert.Equal("/v1/exposure/expiry-impact/QQQ", handler.LastRequest!.RequestUri!.AbsolutePath);
            Assert.Equal("?expiry=2026-11-20&limit=3", handler.LastRequest.RequestUri.Query);

            await client.ExpiryImpactAsync("SPY", limit: 20);
            Assert.Equal("?limit=20", handler.LastRequest!.RequestUri!.Query);
        }
    }

    [Fact]
    public async Task ExpiryImpactTypedAsync_BindsEveryLevel()
    {
        var (client, _) = TestClientFactory.Create(body: Fixture("expiry_impact_live.json"));
        using (client)
        {
            var r = await client.ExpiryImpactTypedAsync("SPY", limit: 2);

            Assert.NotNull(r);
            // shared metadata
            Assert.Equal("SPY", r!.Symbol);
            Assert.Equal(735.885, r.UnderlyingPrice);
            Assert.Equal("6be03e34b24a50979b50d4bd7fb9f700d4c20fe50aecbd0fc54132c60b8e0518", r.SnapshotId);
            Assert.Equal("2026-11-19T08:11:09.2307609Z", r.GeneratedAt);
            Assert.Equal("2026-11-19T01:00:00.26Z", r.QuoteAt);
            Assert.Equal("2026-11-18T14:30:38Z", r.GreeksOldestAt);
            Assert.Equal("2026-11-18T21:15:02Z", r.GreeksNewestAt);
            Assert.Equal("gamma-context-v2", r.CalculationVersion);
            Assert.Equal("captured_cached_gamma_current_spot", r.PricingBasis);
            Assert.Equal("stale", r.DataStatus);
            Assert.Equal(4, r.Reasons!.Count);
            Assert.Equal("expiry_clock_unverified", r.Reasons[0].Code);
            Assert.Equal(600, r.Reasons[0].ContractCount);
            Assert.Null(r.Reasons[2].ContractCount);
            Assert.Null(r.OiAsOf);
            Assert.Equal("unknown", r.OiVintageStatus);
            Assert.Equal(9891, r.EligibleContractCount);
            Assert.Equal(2099, r.RejectedContractCount);
            Assert.Equal(50, r.ObservedMinStrike);
            Assert.Equal(1480, r.ObservedMaxStrike);
            Assert.Equal("usd_delta_notional_per_1pct_move", r.GexUnit);
            Assert.Equal("calls_positive_puts_negative", r.SignConvention);
            Assert.Equal("expiry_removal_sensitivity", r.AnalysisType);
            // selection
            Assert.Equal("2026-11-19", r.SelectedExpiry);
            Assert.Equal("nearest_active", r.SelectionSource);
            Assert.Equal(new[] { "2026-11-19", "2026-11-20", "2026-11-23" }, r.AvailableExpiries);
            // totals
            Assert.Equal(33170572651.203655, r.Current!.GrossGexUsd);
            Assert.Equal(9891, r.Current.ContractCount);
            Assert.Null(r.Current.Status);
            Assert.Equal(153, r.ExcludedExpiry!.ContractCount);
            Assert.Equal(-573935208.9391723, r.ExcludedExpiry.PutGexUsd);
            Assert.Equal("available", r.ExcludingExpiry!.Status);
            Assert.Equal(9738, r.ExcludingExpiry.ContractCount);
            // impact
            Assert.Equal(4.374061987278384, r.Impact!.RemovedGrossSharePct);
            Assert.Equal(95.6259380127216, r.Impact.RetainedGrossSharePct);
            Assert.Equal(-303030991.4205142, r.Impact.NetGexChangeUsd);
            Assert.Equal("negative", r.Impact.NetSignBefore);
            Assert.Equal("negative", r.Impact.NetSignAfter);
            Assert.False(r.Impact.NetSignChanged);
            Assert.Equal(740, r.Impact.LargestGrossReductions![0].Strike);
            Assert.Equal(9.24725841038217, r.Impact.LargestGrossReductions[0].ExcludedShareAtStrikePct);
            Assert.Equal(750, r.Impact.StrongestRemainingConcentrations![0].Strike);
            Assert.Equal(1697218423.3419309, r.Impact.StrongestRemainingConcentrations[0].RemainingGrossGexUsd);
            // per strike (integer JSON numbers bind to doubles)
            Assert.Equal(3, r.Strikes!.Count);
            Assert.Equal(50, r.Strikes[0].Strike);
            Assert.Equal(-93.20546009226986, r.Strikes[0].DistanceFromSpotPct);
            Assert.Equal(0, r.Strikes[0].ExcludedGrossGexUsd);
            Assert.Equal(7688.633337283193, r.Strikes[0].CurrentGrossGexUsd);
            // facts: supporting_values is a free-form map
            Assert.Equal("expiry_gross_share", r.Facts![0].Code);
            Assert.StartsWith("The selected expiry contributes 4.4%", r.Facts[0].Text);
            Assert.Equal("2026-11-19", r.Facts[0].SupportingValues!["expiry"].GetString());
            Assert.Equal(4.374061987278384, r.Facts[0].SupportingValues!["gross_share_pct"].GetDouble());
            Assert.Equal(9738, r.Facts[1].SupportingValues!["remaining_contract_count"].GetInt32());
            // envelope
            Assert.Equal("2026.09.30", r.EndpointVersion);
            Assert.Equal("fa4", r.DataAsOf!.Node);
            Assert.Equal("2026-11-18T00:00:00Z", r.DataAsOf.OiFeed);
        }
    }

    [Fact]
    public async Task StrikeDependencyAsync_SendsFractionalStrikeExactly()
    {
        var (client, handler) = TestClientFactory.Create(body: Fixture("strike_dependency_live.json"));
        using (client)
        {
            await client.StrikeDependencyAsync("SPY", 672.5);
            Assert.Equal("/v1/exposure/strike-dependency/SPY", handler.LastRequest!.RequestUri!.AbsolutePath);
            Assert.Equal("?strike=672.5", handler.LastRequest.RequestUri.Query);

            await client.StrikeDependencyAsync("SPY", 670);
            Assert.Equal("?strike=670", handler.LastRequest!.RequestUri!.Query);
        }
    }

    [Fact]
    public async Task StrikeDependencyAsync_StrikeIsLocaleInvariant()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var (client, handler) = TestClientFactory.Create(body: Fixture("strike_dependency_live.json"));
            using (client)
            {
                await client.StrikeDependencyAsync("QQQ", 612.5);
                Assert.Equal("?strike=612.5", handler.LastRequest!.RequestUri!.Query);
            }
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = saved;
        }
    }

    [Fact]
    public async Task StrikeDependencyTypedAsync_BindsEveryLevel()
    {
        var (client, _) = TestClientFactory.Create(body: Fixture("strike_dependency_live.json"));
        using (client)
        {
            var r = await client.StrikeDependencyTypedAsync("SPY", 670);

            Assert.NotNull(r);
            Assert.Equal("SPY", r!.Symbol);
            Assert.Equal("strike_attribution", r.AnalysisType);
            Assert.Equal(670, r.Strike);
            Assert.Equal(-8.953165236416016, r.DistanceFromSpotPct);
            Assert.Equal(187959991.78647596, r.GrossGexUsd);
            Assert.Equal(-175533366.10899687, r.NetGexUsd);
            Assert.Equal(6213312.838739557, r.CallGexUsd);
            Assert.Equal(-181746678.9477364, r.PutGexUsd);
            Assert.Equal(48, r.ContractCount);
            Assert.Equal("2026-12-18", r.DominantExpiry);
            Assert.Equal(22.973602773735948, r.DominantExpirySharePct);
            Assert.Equal(3, r.ByExpiry!.Count);
            var row = r.ByExpiry[0];
            Assert.Equal("2026-11-19", row.Expiry);
            Assert.Equal("2026-11-19T21:00:00Z", row.ValuationCutoffAt);
            Assert.Equal("2026-11-19T21:15:00Z", row.LastTradingAt);
            Assert.Equal("scheduled_regular_session", row.LastTradingStatus);
            Assert.Null(row.SettlementAt);
            Assert.Equal("unknown", row.SettlementStatus);
            Assert.Equal(0, row.CalendarDaysToExpiry);
            Assert.Equal(0.10318767058863045, row.GrossSharePct);
            Assert.Equal(0.10318767058863045, row.ShareOfStrikeGrossPct);
            Assert.Equal(193951.53716304564, row.GrossGexUsd);
            Assert.Equal(0, row.CallGexUsd);
            Assert.Equal(1, row.ContractCount);
            Assert.Null(row.Status);
            Assert.Equal(1, r.ByExpiry[1].CalendarDaysToExpiry);
            Assert.Equal("stale", r.DataStatus);
            Assert.Equal("2026.09.30", r.EndpointVersion);
            Assert.Equal("fa4", r.DataAsOf!.Node);
        }
    }

    [Fact]
    public async Task ExpiryImpactAsync_Throws403TierRestricted()
    {
        var errBody = "{\"status\":\"ERROR\",\"error\":\"tier_restricted\",\"message\":\"Requires Growth.\",\"current_plan\":\"Basic\",\"required_plan\":\"Growth\"}";
        var (client, _) = TestClientFactory.Create(statusCode: HttpStatusCode.Forbidden, body: errBody);
        using (client)
        {
            var ex = await Assert.ThrowsAsync<TierRestrictedException>(() => client.ExpiryImpactTypedAsync("SPY"));
            Assert.Equal(403, ex.StatusCode);
            Assert.Equal("Basic", ex.CurrentPlan);
            Assert.Equal("Growth", ex.RequiredPlan);
        }
    }

    [Fact]
    public async Task StrikeDependencyAsync_Throws403TierRestricted()
    {
        var errBody = "{\"status\":\"ERROR\",\"error\":\"tier_restricted\",\"message\":\"Requires Growth.\",\"current_plan\":\"Free\",\"required_plan\":\"Growth\"}";
        var (client, _) = TestClientFactory.Create(statusCode: HttpStatusCode.Forbidden, body: errBody);
        using (client)
        {
            var ex = await Assert.ThrowsAsync<TierRestrictedException>(() => client.StrikeDependencyTypedAsync("SPY", 670));
            Assert.Equal("Growth", ex.RequiredPlan);
        }
    }

    [Fact]
    public async Task ExpiryImpactAsync_UnsupportedSymbolSurfaces422()
    {
        var (client, _) = TestClientFactory.Create(statusCode: (HttpStatusCode)422, body: Fixture("error_unsupported_symbol.json"));
        using (client)
        {
            var ex = await Assert.ThrowsAsync<FlashAlphaException>(() => client.ExpiryImpactTypedAsync("AAPL"));
            Assert.Equal(422, ex.StatusCode);
            Assert.Equal("This operation currently supports standard SPY and QQQ options.", ex.Message);
            Assert.Equal("unsupported_symbol", ex.Response!.Value.GetProperty("error").GetString());
        }
    }

    [Fact]
    public async Task StrikeDependencyAsync_UnsupportedSymbolSurfaces422()
    {
        var (client, _) = TestClientFactory.Create(statusCode: (HttpStatusCode)422, body: Fixture("error_unsupported_symbol.json"));
        using (client)
        {
            var ex = await Assert.ThrowsAsync<FlashAlphaException>(() => client.StrikeDependencyAsync("AAPL", 200));
            Assert.Equal(422, ex.StatusCode);
            Assert.Equal("unsupported_symbol", ex.Response!.Value.GetProperty("error").GetString());
        }
    }

    [Fact]
    public async Task StrikeDependencyAsync_StrikeNotFoundEchoesRequestedStrike()
    {
        var (client, _) = TestClientFactory.Create(statusCode: HttpStatusCode.NotFound, body: Fixture("error_strike_not_found.json"));
        using (client)
        {
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => client.StrikeDependencyAsync("SPY", 671.3));
            Assert.Equal("strike_not_found", ex.Response!.Value.GetProperty("error").GetString());
            Assert.Equal(671.3, ex.Response.Value.GetProperty("requested_strike").GetDouble());
        }
    }
}
