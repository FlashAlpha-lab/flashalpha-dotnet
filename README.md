# FlashAlpha .NET SDK

Official .NET / C# client for the [FlashAlpha](https://flashalpha.com) options analytics API.

Access a **live options screener** (filter/rank symbols by GEX, VRP, IV, greeks,
harvest scores, and custom formulas), gamma exposure (GEX), delta exposure (DEX),
vanna exposure (VEX), charm exposure (CHEX), implied volatility, volatility surface,
0DTE analytics, Black-Scholes greeks, Kelly criterion position sizing, and more —
for SPX, SPY, QQQ, AAPL, and all major US equities.

> 🔑 **[Get a free API key at flashalpha.com →](https://flashalpha.com)** · 📚 [API documentation](https://lab.flashalpha.com/docs) · 💹 [FlashAlpha options analytics API](https://flashalpha.com)

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
| `SurfaceSviAsync(symbol)` | Calibrated SVI surface parameters per expiry slice | Alpha+ |
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
| `DexAsync(symbol, expiration?)` | Delta exposure by strike | Basic |
| `VexAsync(symbol, expiration?)` | Vanna exposure by strike | Basic |
| `ChexAsync(symbol, expiration?)` | Charm exposure by strike | Basic |
| `ExposureLevelsAsync(symbol)` | Key support/resistance levels from options exposure | Free |
| `ExposureSummaryAsync(symbol)` | Full GEX/DEX/VEX/CHEX summary + hedging pressure | Growth+ |
| `NarrativeAsync(symbol)` | AI-generated verbal narrative of exposure | Growth+ |
| `ZeroDteAsync(symbol, strikeRange?, expiry?)` | 0DTE regime, expected move, pin risk, hedging, decay | Growth+ |
| `MaxPainAsync(symbol, expiration?)` | Max pain analysis with dealer alignment, pain curve, pin probability | Basic+ |
| `ExposureSheetAsync(symbol, expiration?, minOi?)` | Per-strike GEX/DEX/VEX/CHEX dealer exposure sheet with totals, levels, peaks | Growth+ |
| `ExposureTermStructureAsync(symbol)` | Net GEX/DEX/VEX/CHEX broken out by expiry bucket (term structure of exposure) | Growth+ |
| `ExposureBasketAsync(symbols, weights?)` | Weighted cross-symbol exposure aggregate (up to 50 symbols) | Growth+ |
| `ExposureOiDiffAsync(symbol, topN?)` | Largest open-interest changes since the prior snapshot | Growth+ |

### Flow (live, simulation-aware) — Growth+ (raw tape, unusual-flow signals, OI simulator state & the full live bundle are Alpha)

Each method has a strongly-typed `*TypedAsync` variant (e.g. `FlowLevelsTypedAsync`).

| Method | Description |
|--------|-------------|
| `FlowLevelsAsync(symbol, expiry?)` | Live gamma flip / call & put walls / max pain |
| `FlowPinRiskAsync(symbol, expiry?)` | 0DTE pin-risk score + component breakdown |
| `FlowSummaryAsync(symbol, expiry?)` | At-a-glance flow direction + headline GEX shift |
| `FlowOiAsync(symbol, expiry?)` | Open-interest simulator state (official vs intraday) |
| `FlowGexAsync(symbol, expiry?)` | Live (flow-adjusted) GEX + per-strike profile |
| `FlowDexAsync(symbol, expiry?)` | Live (flow-adjusted) DEX + per-strike profile |
| `FlowDealerRiskAsync(symbol, expiry?)` | Settled-vs-live dealer GEX/DEX + flow adjustment |
| `FlowLiveAsync(symbol, expiry?)` | Everything-at-once live flow bundle |
| `FlowSignalsAsync(symbol, minScore?, intent?, structure?, windowMinutes?, limit?, expiry?)` | Scored, classified unusual-flow feed (block/sweep, intent, 0-100 score) |
| `FlowSignalsSummaryAsync(symbol, windowMinutes?, expiry?)` | Net bullish/bearish + opening/closing premium roll-up + top 10 signals |
| `FlowOptionRecentAsync(symbol, limit?, expiry?)` | Recent option trades, newest-first |
| `FlowOptionSummaryAsync(symbol, expiry?)` | Per-underlying option-flow aggregates |
| `FlowOptionBlocksAsync(symbol, minSize?, expiry?)` | Large option prints (`size >= minSize`) |
| `FlowOptionHistoryAsync(symbol, minutes?, expiry?)` | Per-minute option-flow buckets |
| `FlowOptionCumulativeAsync(symbol, minutes?, expiry?)` | Cumulative option net-flow series |
| `FlowStockRecentAsync(symbol, limit?)` | Recent stock trades, newest-first |
| `FlowStockSummaryAsync(symbol)` | Per-symbol stock-flow aggregates |
| `FlowStockBlocksAsync(symbol, minSize?)` | Large stock prints (`size >= minSize`) |
| `FlowStockHistoryAsync(symbol, minutes?)` | Per-minute stock-flow buckets w/ OHLC |
| `FlowStockCumulativeAsync(symbol, minutes?)` | Cumulative stock net-flow series |
| `FlowOptionsLeaderboardAsync(n?, windowMinutes?)` | Cross-symbol option-flow leaderboard |
| `FlowOptionsOutliersAsync(limit?, minTrades?, windowMinutes?)` | Cross-symbol option-flow outliers |
| `FlowStocksLeaderboardAsync(n?, windowMinutes?)` | Cross-symbol stock-flow leaderboard |
| `FlowStocksOutliersAsync(limit?, minTrades?, windowMinutes?)` | Cross-symbol stock-flow outliers |
| `FlowStockBarsAsync(symbol, resolution, minutes?)` | Multi-resolution OHLCV + flow bars from the live trade tape |
| `FlowDealerPremiumAsync(symbol, windowMinutes?, expiry?)` | Net dealer premium roll-up over the full tape (VWAP-weighted) |

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
| `LiquidityAsync(symbol)` | Per-expiry execution / liquidity score (ATM spread %, OI-weighted spread %, depth) | Growth+ |
| `SkewTermAsync(symbol)` | Skew + term-structure of IV (25-delta risk reversals, ATM term curve) | Growth+ |
| `SpotVolCorrelationAsync(symbol)` | Spot–vol correlation / leverage-effect regime | Growth+ |
| `RealizedVolatilityAsync(symbol)` | Range-based realized vol estimators (close-to-close, Parkinson, Garman-Klass, Rogers-Satchell, Yang-Zhang) over 10/20/30-day windows | Alpha+ |
| `VolatilityForecastAsync(symbol, dist?)` | Conditional vol forecasts (EWMA λ=0.94, HAR-RV, GARCH(1,1) MLE with multi-horizon term structure) | Alpha+ |
| `DispersionAsync(index, symbols, weights?, horizonDays?)` | Implied vs realized correlation (dispersion / vol-arb) for an index vs a basket | Alpha+ |
| `ExpectedMoveAsync(symbol, expiry?)` | Options-implied expected move (straddle-derived) per expiry | Basic+ |
| `VrpHistoryAsync(symbol, days?)` | Historical variance-risk-premium (VRP) time series | Alpha+ |

### Macro / Universe

| Method | Description | Plan |
|--------|-------------|------|
| `VixStateAsync()` | VIX term-structure regime (contango/backwardation, VIX/VIX3M, percentiles) | Growth+ |
| `UniverseAsync(sort?, limit?)` | Curated tier-1 / tier-2 pre-warmed symbol universe | Public |

### Strategy Signals

All ten endpoints return the shared `StrategyDecisionResponse` envelope (decision,
conviction, scored structure proposal, context). Each has a `*TypedAsync` variant.

| Method | Description | Plan |
|--------|-------------|------|
| `StrategyFlowAnomalyAsync(symbol, expiry?)` | Directional options-flow anomaly score + matching short vertical | Growth+ |
| `StrategyExpiryPositioningAsync(symbol, expiry?, minOpenInterest?, wingWidth?)` | OPEX pin-risk score + iron-fly proposal | Basic+ |
| `StrategyZeroDteAsync(symbol, expiry?, minOpenInterest?, wingWidth?)` | 0DTE intraday structure proposal (iron fly / condor) | Growth+ (+0DTE) |
| `StrategyDealerRegimeAsync(symbol, expiry?)` | Dealer-regime read (positive vs negative gamma) | Growth+ |
| `StrategyVolCarryAsync(symbol, expiry?, …)` | Vol-carry harvesting candidate (short premium under positive carry) | Alpha+ |
| `StrategyYieldEnhancementAsync(symbol, expiry?, …)` | Covered-call / cash-secured-put overlay proposal | Growth+ |
| `StrategySurfaceAnomalyAsync(symbol, expiry?)` | Surface-anomaly / vol-arbitrage signal | Alpha+ |
| `StrategySkewAsync(symbol, expiry?)` | Skew-trade signal (risk-reversal / put-skew richness) | Growth+ |
| `StrategyTermStructureAsync(symbol)` | Term-structure signal (calendar / diagonal opportunity) | Growth+ |
| `StrategyTailPricingAsync(symbol, expiry?)` | Tail-pricing signal (wing richness / cheap convexity) | Growth+ |

### Earnings

| Method | Description | Plan |
|--------|-------------|------|
| `EarningsCalendarAsync(days?, symbols?, importance?)` | Upcoming earnings calendar over a forward window | Growth+ |
| `EarningsExpectedMoveAsync(symbol)` | Earnings-implied (straddle-derived) move for the next event | Growth+ |
| `EarningsHistoryAsync(symbol, limit?)` | Historical earnings reactions (implied vs realized move, surprises) | Growth+ |
| `EarningsIvCrushAsync(symbol)` | Estimated post-earnings IV crush (front-month deflation) | Growth+ |
| `EarningsVrpAsync(symbol)` | Earnings variance risk premium with surprise reactions | Alpha+ |
| `EarningsDealerPositioningAsync(symbol)` | Dealer positioning into the event (GEX buckets, top strikes, levels) | Alpha+ |
| `EarningsStrategiesAsync(symbol)` | Earnings-aware strategy scores (straddle/strangle/iron-condor) | Alpha+ |
| `EarningsScreenerAsync(sort?, limit?, days?, minImportance?)` | Cross-sectional earnings screener (VRP richest / cheapest move / highest crush / importance) | Alpha+ |

### Structures (multi-leg, pure-math POST)

| Method | Description | Plan |
|--------|-------------|------|
| `StructurePnlAsync(request)` | At-expiry P&L curve, breakevens, max profit/loss for a multi-leg structure | Basic+ |
| `StructureGreeksAsync(request)` | Aggregated, quantity-scaled, direction-signed position Greeks | Basic+ |

### Zero-DTE Flow (intraday)

| Method | Description | Plan |
|--------|-------------|------|
| `FlowZeroDteSnapshotAsync(symbol)` | Live 0DTE flow snapshot (0DTE analytics + intraday flow direction) | Growth+ |
| `FlowZeroDteSeriesAsync(symbol, bar?, minutes?)` | Intraday 0DTE flow time series (one bar per interval) | Growth+ |
| `FlowZeroDteHedgeFlowAsync(symbol, side?, bar?, minutes?)` | Estimated dealer hedge-flow time series for today's 0DTE chain | Growth+ |
| `FlowZeroDteHeatmapAsync(symbol, metric?, mode?, bar?, minutes?)` | Per-strike value matrix for a strike × time 0DTE heatmap | Alpha+ |
| `FlowZeroDteStrikeFlowAsync(symbol, bar?, minutes?)` | Per-strike signed aggressor flow over today's 0DTE session | Alpha+ |

### Reference Data

| Method | Description | Plan |
|--------|-------------|------|
| `TickersAsync()` | All available stock tickers | Free |
| `OptionsAsync(ticker)` | Option chain metadata (expirations and strikes) | Free |
| `SymbolsAsync()` | Currently active symbols with live data | Free |
| `ScreenerFieldsAsync()` | Catalogue of screener fields available for filters/sort/select/formulas | Free |

### Account and System

| Method | Description | Plan |
|--------|-------------|------|
| `AccountAsync()` | Account info and quota usage | Any |
| `HealthAsync()` | API health check (no auth required) | Public |

## Futures (CME)

FlashAlpha serves the full options-analytics stack for **CME futures** across six complexes - equity index (`ES=F`, `NQ=F`, `RTY=F`, `YM=F`, `MES=F`, `MNQ=F`), metals (`GC=F` gold, `SI=F` silver), energy (`CL=F` crude oil, `NG=F` natural gas), the Treasury curve (`ZT=F`, `ZF=F`, `ZN=F`, `TN=F`, `ZB=F`, `UB=F`), grains (`ZC=F` corn, `ZS=F` soybeans, `ZW=F` wheat) and crypto (`BTC=F` bitcoin). Options-on-futures are priced with **Black-76** (forward-priced) and each root carries its own CME contract multiplier, so notionals and dollar gamma are in real dollars. Note the quote conventions: Treasuries are quoted in points of par and grains in cents, so their multipliers are the contract size divided by 100. Everything that works for an equity works for futures: gamma exposure (GEX), DEX, VEX, CHEX, key levels, max pain, the IV surface, exposure summary, narrative, and live flow.

```csharp
// Gamma exposure for the E-mini S&P 500 future
var gex = await client.GexAsync("ES=F");
Console.WriteLine(gex);
```

Use the `=F` suffix - bare `ES`/`NQ` are equities, not futures. In raw REST paths URL-encode the `=` as `%3D` (e.g. `GET /v1/exposure/gex/GC%3DF`); SDK methods take the plain string `"GC=F"`. Futures symbols require the Growth plan or higher. Historical replay for futures is coming; live analytics are available now.

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

## What the paid tiers unlock

The free tier covers single-expiry GEX on equities, key levels, the BSM Greeks/IV
calculator and stock quotes. Paid tiers add:

- **DEX, VEX (vanna) and CHEX (charm) exposure, plus max pain** — from the **Basic tier**
  ($79/mo), with ETF and index symbols.
- **Full-chain GEX, 0DTE and flow analytics** — from the **Growth tier** ($299/mo).
- **Point-in-time replay since 2018, SVI vol surfaces, VRP analytics, higher-order Greeks**,
  uncached and unlimited — the **Alpha tier** ($1,499/mo). FlashAlpha is one of the only
  public APIs publishing aggregate vanna and charm exposure across the full universe, with
  no look-ahead and no training-serving skew.

Built for quants, prop desks, and vol funds. See the full picture and get a key:
**[flashalpha.com/for-quant-teams](https://flashalpha.com/for-quant-teams?utm_source=github&utm_medium=readme&utm_campaign=repo-flashalpha-dotnet)**
