# FlashAlpha .NET SDK

Official .NET / C# client for the [FlashAlpha](https://flashalpha.com) options analytics API.

Access a **live options screener** (filter/rank symbols by GEX, VRP, IV, greeks,
harvest scores, and custom formulas), gamma exposure (GEX), delta exposure (DEX),
vanna exposure (VEX), charm exposure (CHEX), implied volatility, volatility surface,
0DTE analytics, Black-Scholes greeks, Kelly criterion position sizing, and more —
for SPX, SPY, QQQ, AAPL, and all major US equities.

- API documentation: https://lab.flashalpha.com/docs
- Sign up for an API key: https://flashalpha.com
- Python SDK: https://github.com/FlashAlpha-lab/flashalpha-python

## Installation

```
dotnet add package FlashAlpha
```

Target framework: .NET 8.0+. No external dependencies — uses `System.Net.Http` and `System.Text.Json`.

## Quick start

```csharp
using FlashAlpha;

var client = new FlashAlphaClient("YOUR_API_KEY");

// Gamma exposure for SPY
var gex = await client.GexAsync("SPY");
Console.WriteLine(gex);

// Black-Scholes greeks
var greeks = await client.GreeksAsync(spot: 500, strike: 505, dte: 30, sigma: 0.20, type: "call");
Console.WriteLine(greeks);

// Health check (no API key required)
var health = await client.HealthAsync();

// Live options screener — harvestable VRP setups
var screenResult = await client.ScreenerAsync(new ScreenerRequest
{
    Filters = new ScreenerGroup
    {
        Op = "and",
        Conditions = new List<object>
        {
            new ScreenerLeaf { Field = "regime", Operator = "eq", Value = "positive_gamma" },
            new ScreenerLeaf { Field = "harvest_score", Operator = "gte", Value = 65 },
        },
    },
    Sort = new List<ScreenerSort> { new() { Field = "harvest_score", Direction = "desc" } },
    Select = new List<string> { "symbol", "price", "harvest_score", "dealer_flow_risk" },
});
```

## Constructor

```csharp
var client = new FlashAlphaClient(
    apiKey: "YOUR_API_KEY",   // required
    baseUrl: null,            // optional: override API base URL
    timeout: 30               // optional: request timeout in seconds
);
```

The client implements `IDisposable`. Use `using` or call `Dispose()` when finished.

## All methods

All methods return `Task<JsonElement>` and accept an optional `CancellationToken`.

### Market Data

| Method | Description | Plan |
|--------|-------------|------|
| `StockQuoteAsync(ticker)` | Live stock quote (bid/ask/mid/last) | Free |
| `OptionQuoteAsync(ticker, expiry?, strike?, type?)` | Option quotes with greeks | Growth+ |
| `SurfaceAsync(symbol)` | Volatility surface grid | Free |
| `StockSummaryAsync(symbol)` | Comprehensive stock summary (price, vol, exposure, macro) | Free |

### Historical Data

| Method | Description | Plan |
|--------|-------------|------|
| `HistoricalStockQuoteAsync(ticker, date, time?)` | Historical stock quotes (minute resolution) | Growth+ |
| `HistoricalOptionQuoteAsync(ticker, date, time?, expiry?, strike?, type?)` | Historical option quotes (minute resolution) | Growth+ |

### Exposure Analytics

| Method | Description | Plan |
|--------|-------------|------|
| `GexAsync(symbol, expiration?, minOi?)` | Gamma exposure by strike | Free |
| `DexAsync(symbol, expiration?)` | Delta exposure by strike | Free |
| `VexAsync(symbol, expiration?)` | Vanna exposure by strike | Free |
| `ChexAsync(symbol, expiration?)` | Charm exposure by strike | Free |
| `ExposureLevelsAsync(symbol)` | Key support/resistance levels from options exposure | Free |
| `ExposureSummaryAsync(symbol)` | Full GEX/DEX/VEX/CHEX summary + hedging pressure | Growth+ |
| `NarrativeAsync(symbol)` | AI-generated verbal narrative of exposure | Growth+ |
| `ZeroDteAsync(symbol, strikeRange?)` | 0DTE regime, expected move, pin risk, hedging, decay | Growth+ |
| `ExposureHistoryAsync(symbol, days?)` | Daily exposure snapshots for trend analysis | Growth+ |

### Pricing and Sizing

| Method | Description | Plan |
|--------|-------------|------|
| `GreeksAsync(spot, strike, dte, sigma, type?, r?, q?)` | Full BSM greeks (first, second, third order) | Free |
| `IvAsync(spot, strike, dte, price, type?, r?, q?)` | Implied volatility from market price | Free |
| `KellyAsync(spot, strike, dte, sigma, premium, mu, type?, r?, q?)` | Kelly criterion optimal position size | Growth+ |

### Volatility Analytics

| Method | Description | Plan |
|--------|-------------|------|
| `VolatilityAsync(symbol)` | Comprehensive volatility analysis | Growth+ |
| `AdvVolatilityAsync(symbol)` | SVI parameters, variance surface, arbitrage detection, variance swap | Alpha+ |

### Reference Data

| Method | Description | Plan |
|--------|-------------|------|
| `TickersAsync()` | All available stock tickers | Free |
| `OptionsAsync(ticker)` | Option chain metadata (expirations and strikes) | Free |
| `SymbolsAsync()` | Currently active symbols with live data | Free |

### Account and System

| Method | Description | Plan |
|--------|-------------|------|
| `AccountAsync()` | Account info and quota usage | Any |
| `HealthAsync()` | API health check (no auth required) | Public |

## Error handling

```csharp
using FlashAlpha;

try
{
    var result = await client.GexAsync("SPY");
}
catch (AuthenticationException ex)
{
    // HTTP 401: invalid or missing API key
    Console.WriteLine($"Auth error: {ex.Message}");
}
catch (TierRestrictedException ex)
{
    // HTTP 403: endpoint requires a higher plan
    Console.WriteLine($"Upgrade required. Current: {ex.CurrentPlan}, Required: {ex.RequiredPlan}");
}
catch (NotFoundException ex)
{
    // HTTP 404: symbol or resource not found
    Console.WriteLine($"Not found: {ex.Message}");
}
catch (RateLimitException ex)
{
    // HTTP 429: rate limit exceeded
    Console.WriteLine($"Rate limited. Retry after {ex.RetryAfter}s");
}
catch (ServerException ex)
{
    // HTTP 5xx: server-side error
    Console.WriteLine($"Server error {ex.StatusCode}: {ex.Message}");
}
catch (FlashAlphaException ex)
{
    // Any other API error
    Console.WriteLine($"API error {ex.StatusCode}: {ex.Message}");
}
```

All exceptions derive from `FlashAlphaException`, which exposes:
- `StatusCode` (int): the HTTP status code
- `Response` (JsonElement?): the raw JSON body, if the server returned one

## Dependency injection

`FlashAlphaClient` accepts a pre-configured `HttpClient`, making it compatible with
`IHttpClientFactory` and easy to mock in unit tests:

```csharp
// ASP.NET Core registration
builder.Services.AddHttpClient<FlashAlphaClient>(client =>
{
    client.BaseAddress = new Uri("https://lab.flashalpha.com");
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["FlashAlpha:ApiKey"]);
});
```

## Running the tests

Unit tests run without a key:

```
dotnet test
```

Integration tests require a live API key:

```
set FLASHALPHA_API_KEY=your_key_here
dotnet test
```

## License

MIT. See [LICENSE](LICENSE).

## Other SDKs

| Language | Package | Repository |
|----------|---------|------------|
| Python | `pip install flashalpha` | [flashalpha-python](https://github.com/FlashAlpha-lab/flashalpha-python) |
| JavaScript | `npm i flashalpha` | [flashalpha-js](https://github.com/FlashAlpha-lab/flashalpha-js) |
| Java | Maven Central | [flashalpha-java](https://github.com/FlashAlpha-lab/flashalpha-java) |
| Go | `go get github.com/FlashAlpha-lab/flashalpha-go` | [flashalpha-go](https://github.com/FlashAlpha-lab/flashalpha-go) |
| MCP | Claude / LLM tool server | [flashalpha-mcp](https://github.com/FlashAlpha-lab/flashalpha-mcp) |

## Links

- [FlashAlpha](https://flashalpha.com) — API keys, docs, pricing
- [API Documentation](https://flashalpha.com/docs)
- [NuGet Package](https://www.nuget.org/packages/FlashAlpha)
- [Examples](https://github.com/FlashAlpha-lab/flashalpha-examples) — runnable tutorials
- [GEX Explained](https://github.com/FlashAlpha-lab/gex-explained) — gamma exposure theory and code
- [0DTE Options Analytics](https://github.com/FlashAlpha-lab/0dte-options-analytics) — 0DTE pin risk, expected move, dealer hedging
- [Volatility Surface Python](https://github.com/FlashAlpha-lab/volatility-surface-python) — SVI calibration, variance swap, skew analysis
- [Awesome Options Analytics](https://github.com/FlashAlpha-lab/awesome-options-analytics) — curated resource list
