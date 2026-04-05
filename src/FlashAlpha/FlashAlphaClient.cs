using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlashAlpha;

/// <summary>
/// Thin HTTP wrapper around the FlashAlpha options analytics REST API.
/// See https://flashalpha.com for API documentation and subscription plans.
/// </summary>
public sealed class FlashAlphaClient : IDisposable
{
    private const string DefaultBaseUrl = "https://lab.flashalpha.com";

    private readonly HttpClient _http;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="FlashAlphaClient"/>.
    /// </summary>
    /// <param name="apiKey">Your FlashAlpha API key from https://flashalpha.com.</param>
    /// <param name="baseUrl">Override the API base URL (useful for testing).</param>
    /// <param name="timeout">Request timeout in seconds. Default is 30.</param>
    public FlashAlphaClient(string apiKey, string? baseUrl = null, int timeout = 30)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key must not be null or empty.", nameof(apiKey));

        var effectiveBase = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');

        _http = new HttpClient
        {
            BaseAddress = new Uri(effectiveBase),
            Timeout = TimeSpan.FromSeconds(timeout),
        };
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Creates a new <see cref="FlashAlphaClient"/> using a pre-configured <see cref="HttpClient"/>.
    /// Useful for injecting a mock handler in unit tests.
    /// </summary>
    public FlashAlphaClient(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static string BuildQuery(Dictionary<string, string?> parameters)
    {
        var parts = new List<string>();
        foreach (var (key, value) in parameters)
        {
            if (value is not null)
                parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }
        return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
    }

    private async Task<JsonElement> GetAsync(string path, Dictionary<string, string?>? parameters = null, CancellationToken ct = default)
    {
        var query = parameters is not null ? BuildQuery(parameters) : string.Empty;
        var url = path + query;

        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        return await HandleResponseAsync(response).ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions PostSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private async Task<JsonElement> PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        HttpContent? content = null;
        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, PostSerializerOptions);
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // content ownership is transferred to HttpRequestMessage internally; it will be
        // disposed when the response is disposed. Do NOT dispose explicitly here.
        using var response = await _http.PostAsync(path, content, ct).ConfigureAwait(false);
        return await HandleResponseAsync(response).ConfigureAwait(false);
    }

    private static async Task<JsonElement> HandleResponseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(body))
                return JsonDocument.Parse("{}").RootElement;
            return JsonDocument.Parse(body).RootElement;
        }

        JsonElement? json = null;
        string message = body;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                json = doc.RootElement;

                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    message = msgProp.GetString() ?? message;
                else if (doc.RootElement.TryGetProperty("detail", out var detailProp))
                    message = detailProp.GetString() ?? message;
            }
            catch (JsonException)
            {
                // body is not JSON; use raw text as message
            }
        }

        int statusCode = (int)response.StatusCode;

        switch (statusCode)
        {
            case 401:
                throw new AuthenticationException(message, json);
            case 403:
            {
                string? currentPlan = null;
                string? requiredPlan = null;
                if (json.HasValue)
                {
                    if (json.Value.TryGetProperty("current_plan", out var cp))
                        currentPlan = cp.GetString();
                    if (json.Value.TryGetProperty("required_plan", out var rp))
                        requiredPlan = rp.GetString();
                }
                throw new TierRestrictedException(message, currentPlan, requiredPlan, json);
            }
            case 404:
                throw new NotFoundException(message, json);
            case 429:
            {
                int? retryAfter = null;
                if (response.Headers.TryGetValues("Retry-After", out var values))
                {
                    foreach (var v in values)
                    {
                        if (int.TryParse(v, out var ra))
                        {
                            retryAfter = ra;
                            break;
                        }
                    }
                }
                throw new RateLimitException(message, retryAfter, json);
            }
            default:
                if (statusCode >= 500)
                    throw new ServerException(message, statusCode, json);
                throw new FlashAlphaException(message, statusCode, json);
        }
    }

    // ── Market Data ───────────────────────────────────────────────────────────

    /// <summary>Live stock quote (bid/ask/mid/last).</summary>
    public Task<JsonElement> StockQuoteAsync(string ticker, CancellationToken ct = default)
        => GetAsync($"/stockquote/{Uri.EscapeDataString(ticker)}", null, ct);

    /// <summary>Option quotes with greeks. Requires Growth+.</summary>
    public Task<JsonElement> OptionQuoteAsync(
        string ticker,
        string? expiry = null,
        double? strike = null,
        string? type = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        if (strike.HasValue) p["strike"] = strike.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (type is not null) p["type"] = type;
        return GetAsync($"/optionquote/{Uri.EscapeDataString(ticker)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Volatility surface grid (public, no auth required).</summary>
    public Task<JsonElement> SurfaceAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/surface/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Comprehensive stock summary (price, vol, exposure, macro).</summary>
    public Task<JsonElement> StockSummaryAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/stock/{Uri.EscapeDataString(symbol)}/summary", null, ct);

    // ── Historical ────────────────────────────────────────────────────────────

    /// <summary>Historical stock quotes (minute-by-minute).</summary>
    public Task<JsonElement> HistoricalStockQuoteAsync(
        string ticker,
        string date,
        string? time = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?> { ["date"] = date };
        if (time is not null) p["time"] = time;
        return GetAsync($"/historical/stockquote/{Uri.EscapeDataString(ticker)}", p, ct);
    }

    /// <summary>Historical option quotes (minute-by-minute).</summary>
    public Task<JsonElement> HistoricalOptionQuoteAsync(
        string ticker,
        string date,
        string? time = null,
        string? expiry = null,
        double? strike = null,
        string? type = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?> { ["date"] = date };
        if (time is not null) p["time"] = time;
        if (expiry is not null) p["expiry"] = expiry;
        if (strike.HasValue) p["strike"] = strike.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (type is not null) p["type"] = type;
        return GetAsync($"/historical/optionquote/{Uri.EscapeDataString(ticker)}", p, ct);
    }

    // ── Exposure Analytics ────────────────────────────────────────────────────

    /// <summary>Gamma exposure by strike.</summary>
    public Task<JsonElement> GexAsync(
        string symbol,
        string? expiration = null,
        int? minOi = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        if (minOi.HasValue) p["min_oi"] = minOi.Value.ToString();
        return GetAsync($"/v1/exposure/gex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Delta exposure by strike.</summary>
    public Task<JsonElement> DexAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/exposure/dex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Vanna exposure by strike.</summary>
    public Task<JsonElement> VexAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/exposure/vex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Charm exposure by strike.</summary>
    public Task<JsonElement> ChexAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/exposure/chex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Full exposure summary (GEX/DEX/VEX/CHEX + hedging). Requires Growth+.</summary>
    public Task<JsonElement> ExposureSummaryAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/summary/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Key support/resistance levels derived from options exposure.</summary>
    public Task<JsonElement> ExposureLevelsAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/levels/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Verbal narrative analysis of exposure. Requires Growth+.</summary>
    public Task<JsonElement> NarrativeAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/narrative/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>Real-time 0DTE analytics: regime, expected move, pin risk, hedging, decay. Requires Growth+.</summary>
    public Task<JsonElement> ZeroDteAsync(string symbol, double? strikeRange = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (strikeRange.HasValue) p["strike_range"] = strikeRange.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return GetAsync($"/v1/exposure/zero-dte/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Daily exposure snapshots for trend analysis. Requires Growth+.</summary>
    public Task<JsonElement> ExposureHistoryAsync(string symbol, int? days = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (days.HasValue) p["days"] = days.Value.ToString();
        return GetAsync($"/v1/exposure/history/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    // ── Pricing & Sizing ──────────────────────────────────────────────────────

    /// <summary>Full Black-Scholes-Merton greeks (first, second, and third order).</summary>
    public Task<JsonElement> GreeksAsync(
        double spot,
        double strike,
        double dte,
        double sigma,
        string type = "call",
        double? r = null,
        double? q = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>
        {
            ["spot"]   = spot.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["strike"] = strike.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["dte"]    = dte.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["sigma"]  = sigma.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["type"]   = type,
        };
        if (r.HasValue) p["r"] = r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (q.HasValue) p["q"] = q.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return GetAsync("/v1/pricing/greeks", p, ct);
    }

    /// <summary>Implied volatility from market price.</summary>
    public Task<JsonElement> IvAsync(
        double spot,
        double strike,
        double dte,
        double price,
        string type = "call",
        double? r = null,
        double? q = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>
        {
            ["spot"]   = spot.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["strike"] = strike.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["dte"]    = dte.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["price"]  = price.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["type"]   = type,
        };
        if (r.HasValue) p["r"] = r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (q.HasValue) p["q"] = q.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return GetAsync("/v1/pricing/iv", p, ct);
    }

    /// <summary>Kelly criterion optimal position sizing. Requires Growth+.</summary>
    public Task<JsonElement> KellyAsync(
        double spot,
        double strike,
        double dte,
        double sigma,
        double premium,
        double mu,
        string type = "call",
        double? r = null,
        double? q = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>
        {
            ["spot"]    = spot.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["strike"]  = strike.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["dte"]     = dte.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["sigma"]   = sigma.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["premium"] = premium.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["mu"]      = mu.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["type"]    = type,
        };
        if (r.HasValue) p["r"] = r.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (q.HasValue) p["q"] = q.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return GetAsync("/v1/pricing/kelly", p, ct);
    }

    // ── Volatility Analytics ──────────────────────────────────────────────────

    /// <summary>Comprehensive volatility analysis. Requires Growth+.</summary>
    public Task<JsonElement> VolatilityAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/volatility/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Advanced volatility analytics: SVI parameters, variance surface, arbitrage detection,
    /// greek surfaces, and variance swap pricing. Requires Alpha+.
    /// </summary>
    public Task<JsonElement> AdvVolatilityAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/adv_volatility/{Uri.EscapeDataString(symbol)}", null, ct);

    // ── Reference Data ────────────────────────────────────────────────────────

    /// <summary>All available stock tickers.</summary>
    public Task<JsonElement> TickersAsync(CancellationToken ct = default)
        => GetAsync("/v1/tickers", null, ct);

    /// <summary>Option chain metadata (expirations and strikes) for a ticker.</summary>
    public Task<JsonElement> OptionsAsync(string ticker, CancellationToken ct = default)
        => GetAsync($"/v1/options/{Uri.EscapeDataString(ticker)}", null, ct);

    /// <summary>Currently queried symbols with live data.</summary>
    public Task<JsonElement> SymbolsAsync(CancellationToken ct = default)
        => GetAsync("/v1/symbols", null, ct);

    // ── Account & System ──────────────────────────────────────────────────────

    // ── Screener ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Live options screener — filter and rank symbols by gamma exposure, VRP,
    /// volatility, greeks, and more. Powered by an in-memory store updated every
    /// 5-10s from live market data. Growth: 10-symbol universe, up to 10 rows.
    /// Alpha: ~250 symbols, up to 50 rows, formulas, and harvest/dealer-flow-risk scores.
    /// </summary>
    /// <param name="request">Screener request with filters, sort, select, formulas, limit, offset.</param>
    public Task<JsonElement> ScreenerAsync(ScreenerRequest request, CancellationToken ct = default)
        => PostAsync("/v1/screener", request, ct);

    /// <summary>
    /// Live options screener with raw object body (flexible alternative to ScreenerRequest).
    /// Use this when you want full control over the JSON payload shape.
    /// </summary>
    public Task<JsonElement> ScreenerAsync(object body, CancellationToken ct = default)
        => PostAsync("/v1/screener", body, ct);

    /// <summary>Account info and quota usage.</summary>
    public Task<JsonElement> AccountAsync(CancellationToken ct = default)
        => GetAsync("/v1/account", null, ct);

    /// <summary>API health check (public, no authentication required).</summary>
    public Task<JsonElement> HealthAsync(CancellationToken ct = default)
        => GetAsync("/health", null, ct);

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
    }
}
