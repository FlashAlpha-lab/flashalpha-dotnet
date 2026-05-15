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

    /// <summary>
    /// Strongly-typed variant of <see cref="StockQuoteAsync(string, CancellationToken)"/>.
    /// Returns a <see cref="StockQuoteResponse"/> POCO. The original
    /// <see cref="StockQuoteAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<StockQuoteResponse?> StockQuoteTypedAsync(string ticker, CancellationToken ct = default)
    {
        var element = await StockQuoteAsync(ticker, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<StockQuoteResponse>(element.GetRawText(), PostSerializerOptions);
    }

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

    /// <summary>
    /// Strongly-typed variant of <see cref="OptionQuoteAsync(string, string?, double?, string?, CancellationToken)"/>
    /// for the single-object response shape — i.e. when ALL three filters
    /// (<paramref name="expiry"/> + <paramref name="strike"/> + <paramref name="type"/>) are supplied.
    /// Returns a single <see cref="OptionQuote"/> POCO. The original untyped
    /// <see cref="OptionQuoteAsync(string, string?, double?, string?, CancellationToken)"/> remains unchanged.
    /// </summary>
    /// <remarks>
    /// When any filter is omitted the API returns an array — use
    /// <see cref="OptionQuotesTypedAsync(string, string?, double?, string?, CancellationToken)"/> instead.
    /// </remarks>
    public async Task<OptionQuote?> OptionQuoteTypedAsync(
        string ticker,
        string? expiry = null,
        double? strike = null,
        string? type = null,
        CancellationToken ct = default)
    {
        var element = await OptionQuoteAsync(ticker, expiry, strike, type, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<OptionQuote>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>
    /// Strongly-typed variant of <see cref="OptionQuoteAsync(string, string?, double?, string?, CancellationToken)"/>
    /// for the array response shape — i.e. when no filters or partial filters are supplied.
    /// Returns a <see cref="List{T}"/> of <see cref="OptionQuote"/> POCOs.
    /// </summary>
    public async Task<List<OptionQuote>?> OptionQuotesTypedAsync(
        string ticker,
        string? expiry = null,
        double? strike = null,
        string? type = null,
        CancellationToken ct = default)
    {
        var element = await OptionQuoteAsync(ticker, expiry, strike, type, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<List<OptionQuote>>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Volatility surface grid (public, no auth required).</summary>
    public Task<JsonElement> SurfaceAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/surface/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="SurfaceAsync(string, CancellationToken)"/>.
    /// Returns a <see cref="SurfaceResponse"/> POCO. Public — no auth required.
    /// The original <see cref="SurfaceAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<SurfaceResponse?> SurfaceTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await SurfaceAsync(symbol, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SurfaceResponse>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Comprehensive stock summary (price, vol, exposure, macro).</summary>
    public Task<JsonElement> StockSummaryAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/stock/{Uri.EscapeDataString(symbol)}/summary", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="StockSummaryAsync(string, CancellationToken)"/>.
    /// Returns a <see cref="StockSummaryResponse"/> POCO with snake_case → PascalCase
    /// field mappings. The original <see cref="StockSummaryAsync(string, CancellationToken)"/>
    /// remains unchanged.
    /// </summary>
    public async Task<StockSummaryResponse?> StockSummaryTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await StockSummaryAsync(symbol, ct).ConfigureAwait(false);
        return element.Deserialize<StockSummaryResponse>(PostSerializerOptions);
    }

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

    /// <summary>
    /// Strongly-typed variant of <see cref="GexAsync(string, string?, int?, CancellationToken)"/>.
    /// Returns a <see cref="GexResponse"/> POCO with per-strike GEX rows plus
    /// gamma flip and net GEX label. The original
    /// <see cref="GexAsync(string, string?, int?, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<GexResponse?> GexTypedAsync(
        string symbol,
        string? expiration = null,
        int? minOi = null,
        CancellationToken ct = default)
    {
        var element = await GexAsync(symbol, expiration, minOi, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<GexResponse>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Delta exposure by strike.</summary>
    public Task<JsonElement> DexAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/exposure/dex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>
    /// Strongly-typed variant of <see cref="DexAsync(string, string?, CancellationToken)"/>.
    /// Returns a <see cref="DexResponse"/> POCO with per-strike DEX rows plus net DEX.
    /// The original <see cref="DexAsync(string, string?, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<DexResponse?> DexTypedAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var element = await DexAsync(symbol, expiration, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<DexResponse>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Vanna exposure by strike.</summary>
    public Task<JsonElement> VexAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/exposure/vex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>
    /// Strongly-typed variant of <see cref="VexAsync(string, string?, CancellationToken)"/>.
    /// Returns a <see cref="VexResponse"/> POCO with per-strike VEX rows plus net VEX
    /// and a textual interpretation. The original
    /// <see cref="VexAsync(string, string?, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<VexResponse?> VexTypedAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var element = await VexAsync(symbol, expiration, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<VexResponse>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Charm exposure by strike.</summary>
    public Task<JsonElement> ChexAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/exposure/chex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>
    /// Strongly-typed variant of <see cref="ChexAsync(string, string?, CancellationToken)"/>.
    /// Returns a <see cref="ChexResponse"/> POCO with per-strike CHEX rows plus net CHEX
    /// and a textual interpretation. The original
    /// <see cref="ChexAsync(string, string?, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<ChexResponse?> ChexTypedAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var element = await ChexAsync(symbol, expiration, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<ChexResponse>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Full exposure summary (GEX/DEX/VEX/CHEX + hedging). Requires Growth+.</summary>
    public Task<JsonElement> ExposureSummaryAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/summary/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="ExposureSummaryAsync(string, CancellationToken)"/>.
    /// Returns an <see cref="ExposureSummaryResponse"/> POCO. The original
    /// <see cref="ExposureSummaryAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<ExposureSummaryResponse?> ExposureSummaryTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await ExposureSummaryAsync(symbol, ct).ConfigureAwait(false);
        return element.Deserialize<ExposureSummaryResponse>(PostSerializerOptions);
    }

    /// <summary>Key support/resistance levels derived from options exposure.</summary>
    public Task<JsonElement> ExposureLevelsAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/levels/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="ExposureLevelsAsync(string, CancellationToken)"/>.
    /// Returns an <see cref="ExposureLevelsResponse"/> POCO. The original
    /// <see cref="ExposureLevelsAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<ExposureLevelsResponse?> ExposureLevelsTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await ExposureLevelsAsync(symbol, ct).ConfigureAwait(false);
        return element.Deserialize<ExposureLevelsResponse>(PostSerializerOptions);
    }

    /// <summary>Verbal narrative analysis of exposure. Requires Growth+.</summary>
    public Task<JsonElement> NarrativeAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/exposure/narrative/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="NarrativeAsync(string, CancellationToken)"/>.
    /// Returns a <see cref="NarrativeResponse"/> POCO. The original
    /// <see cref="NarrativeAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<NarrativeResponse?> NarrativeTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await NarrativeAsync(symbol, ct).ConfigureAwait(false);
        return element.Deserialize<NarrativeResponse>(PostSerializerOptions);
    }

    /// <summary>Real-time 0DTE analytics: regime, expected move, pin risk, hedging, decay. Requires Growth+.</summary>
    public Task<JsonElement> ZeroDteAsync(string symbol, double? strikeRange = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (strikeRange.HasValue) p["strike_range"] = strikeRange.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return GetAsync($"/v1/exposure/zero-dte/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>
    /// Strongly-typed variant of <see cref="ZeroDteAsync(string, double?, CancellationToken)"/>.
    /// Returns a <see cref="ZeroDteResponse"/> POCO with snake_case → PascalCase field
    /// mappings for every documented field. The original <see cref="ZeroDteAsync(string, double?, CancellationToken)"/>
    /// remains unchanged.
    /// </summary>
    public async Task<ZeroDteResponse> ZeroDteTypedAsync(string symbol, double? strikeRange = null, CancellationToken ct = default)
    {
        var element = await ZeroDteAsync(symbol, strikeRange, ct).ConfigureAwait(false);
        var typed = element.Deserialize<ZeroDteResponse>(PostSerializerOptions);
        return typed ?? new ZeroDteResponse();
    }

    // ── Flow (live, simulation-aware) — requires the Alpha plan ───────────────
    //
    // Analytics endpoints (snake_case) fold today's intraday trade tape into
    // the settled book. All accept an optional expiry ("YYYY-MM-DD"). Each
    // untyped *Async returns the raw JsonElement; the matching *TypedAsync
    // deserializes into the POCO from Flow.cs.

    /// <summary>Live gamma flip / call &amp; put walls / max pain. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowLevelsAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/levels/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowLevelsAsync"/>.</summary>
    public async Task<FlowLevelsResponse?> FlowLevelsTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowLevelsAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowLevelsResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>0DTE pin-risk score + component breakdown. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowPinRiskAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/pin-risk/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowPinRiskAsync"/>.</summary>
    public async Task<FlowPinRiskResponse?> FlowPinRiskTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowPinRiskAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowPinRiskResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>At-a-glance flow direction + headline GEX shift. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowSummaryAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/summary/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowSummaryAsync"/>.</summary>
    public async Task<FlowSummaryResponse?> FlowSummaryTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowSummaryAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowSummaryResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Open-interest simulator state (official vs intraday). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOiAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/oi/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOiAsync"/>.</summary>
    public async Task<FlowOiResponse?> FlowOiTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowOiAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOiResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Live (flow-adjusted) GEX + per-strike profile. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowGexAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/gex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowGexAsync"/>.</summary>
    public async Task<FlowGexResponse?> FlowGexTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowGexAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowGexResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Live (flow-adjusted) DEX + per-strike profile. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowDexAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/dex/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowDexAsync"/>.</summary>
    public async Task<FlowDexResponse?> FlowDexTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowDexAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowDexResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Settled-vs-live dealer GEX/DEX + flow adjustment. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowDealerRiskAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/dealer-risk/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowDealerRiskAsync"/>.</summary>
    public async Task<FlowDealerRiskResponse?> FlowDealerRiskTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowDealerRiskAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowDealerRiskResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Everything-at-once live flow bundle (convenience). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowLiveAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/live/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowLiveAsync"/>.</summary>
    public async Task<FlowLiveResponse?> FlowLiveTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowLiveAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowLiveResponse>(e.GetRawText(), PostSerializerOptions);
    }

    // Raw flow data (camelCase) — proxied trade tape.

    /// <summary>Recent option trades, newest-first (limit 1–500). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionRecentAsync(string symbol, int? limit = null, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/options/{Uri.EscapeDataString(symbol)}/recent", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionRecentAsync"/>.</summary>
    public async Task<FlowOptionRecentResponse?> FlowOptionRecentTypedAsync(string symbol, int? limit = null, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowOptionRecentAsync(symbol, limit, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionRecentResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Per-underlying option-flow aggregates. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionSummaryAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/options/{Uri.EscapeDataString(symbol)}/summary", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionSummaryAsync"/>.</summary>
    public async Task<FlowOptionSummaryResponse?> FlowOptionSummaryTypedAsync(string symbol, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowOptionSummaryAsync(symbol, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionSummaryResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Large option prints (size &gt;= minSize). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionBlocksAsync(string symbol, int? minSize = null, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (minSize.HasValue) p["minSize"] = minSize.Value.ToString();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/options/{Uri.EscapeDataString(symbol)}/blocks", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionBlocksAsync"/>.</summary>
    public async Task<FlowOptionBlocksResponse?> FlowOptionBlocksTypedAsync(string symbol, int? minSize = null, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowOptionBlocksAsync(symbol, minSize, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionBlocksResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Per-minute option-flow buckets (minutes 1–10080). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionHistoryAsync(string symbol, int? minutes = null, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/options/{Uri.EscapeDataString(symbol)}/history", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionHistoryAsync"/>.</summary>
    public async Task<FlowOptionHistoryResponse?> FlowOptionHistoryTypedAsync(string symbol, int? minutes = null, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowOptionHistoryAsync(symbol, minutes, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionHistoryResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Cumulative option net-flow series. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionCumulativeAsync(string symbol, int? minutes = null, string? expiry = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        if (expiry is not null) p["expiry"] = expiry;
        return GetAsync($"/v1/flow/options/{Uri.EscapeDataString(symbol)}/cumulative", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionCumulativeAsync"/>.</summary>
    public async Task<FlowOptionCumulativeResponse?> FlowOptionCumulativeTypedAsync(string symbol, int? minutes = null, string? expiry = null, CancellationToken ct = default)
    {
        var e = await FlowOptionCumulativeAsync(symbol, minutes, expiry, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionCumulativeResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Recent stock trades, newest-first (limit 1–500). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStockRecentAsync(string symbol, int? limit = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        return GetAsync($"/v1/flow/stocks/{Uri.EscapeDataString(symbol)}/recent", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStockRecentAsync"/>.</summary>
    public async Task<FlowStockRecentResponse?> FlowStockRecentTypedAsync(string symbol, int? limit = null, CancellationToken ct = default)
    {
        var e = await FlowStockRecentAsync(symbol, limit, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockRecentResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Per-symbol stock-flow aggregates. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStockSummaryAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/flow/stocks/{Uri.EscapeDataString(symbol)}/summary", null, ct);

    /// <summary>Strongly-typed variant of <see cref="FlowStockSummaryAsync"/>.</summary>
    public async Task<FlowStockSummaryResponse?> FlowStockSummaryTypedAsync(string symbol, CancellationToken ct = default)
    {
        var e = await FlowStockSummaryAsync(symbol, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockSummaryResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Large stock prints (size &gt;= minSize). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStockBlocksAsync(string symbol, int? minSize = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (minSize.HasValue) p["minSize"] = minSize.Value.ToString();
        return GetAsync($"/v1/flow/stocks/{Uri.EscapeDataString(symbol)}/blocks", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStockBlocksAsync"/>.</summary>
    public async Task<FlowStockBlocksResponse?> FlowStockBlocksTypedAsync(string symbol, int? minSize = null, CancellationToken ct = default)
    {
        var e = await FlowStockBlocksAsync(symbol, minSize, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockBlocksResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Per-minute stock-flow buckets w/ OHLC (minutes 1–10080). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStockHistoryAsync(string symbol, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/stocks/{Uri.EscapeDataString(symbol)}/history", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStockHistoryAsync"/>.</summary>
    public async Task<FlowStockHistoryResponse?> FlowStockHistoryTypedAsync(string symbol, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowStockHistoryAsync(symbol, minutes, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockHistoryResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Cumulative stock net-flow series. Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStockCumulativeAsync(string symbol, int? minutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (minutes.HasValue) p["minutes"] = minutes.Value.ToString();
        return GetAsync($"/v1/flow/stocks/{Uri.EscapeDataString(symbol)}/cumulative", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStockCumulativeAsync"/>.</summary>
    public async Task<FlowStockCumulativeResponse?> FlowStockCumulativeTypedAsync(string symbol, int? minutes = null, CancellationToken ct = default)
    {
        var e = await FlowStockCumulativeAsync(symbol, minutes, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockCumulativeResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Cross-symbol option-flow leaderboard (top n by net $). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionsLeaderboardAsync(int? n = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (n.HasValue) p["n"] = n.Value.ToString();
        if (windowMinutes.HasValue) p["windowMinutes"] = windowMinutes.Value.ToString();
        return GetAsync("/v1/flow/options/leaderboard", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionsLeaderboardAsync"/>.</summary>
    public async Task<FlowOptionLeaderboardResponse?> FlowOptionsLeaderboardTypedAsync(int? n = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var e = await FlowOptionsLeaderboardAsync(n, windowMinutes, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionLeaderboardResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Cross-symbol option-flow outliers (imbalance-ranked). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowOptionsOutliersAsync(int? limit = null, int? minTrades = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        if (minTrades.HasValue) p["minTrades"] = minTrades.Value.ToString();
        if (windowMinutes.HasValue) p["windowMinutes"] = windowMinutes.Value.ToString();
        return GetAsync("/v1/flow/options/outliers", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowOptionsOutliersAsync"/>.</summary>
    public async Task<FlowOptionOutliersResponse?> FlowOptionsOutliersTypedAsync(int? limit = null, int? minTrades = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var e = await FlowOptionsOutliersAsync(limit, minTrades, windowMinutes, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowOptionOutliersResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Cross-symbol stock-flow leaderboard (top n by net $). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStocksLeaderboardAsync(int? n = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (n.HasValue) p["n"] = n.Value.ToString();
        if (windowMinutes.HasValue) p["windowMinutes"] = windowMinutes.Value.ToString();
        return GetAsync("/v1/flow/stocks/leaderboard", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStocksLeaderboardAsync"/>.</summary>
    public async Task<FlowStockLeaderboardResponse?> FlowStocksLeaderboardTypedAsync(int? n = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var e = await FlowStocksLeaderboardAsync(n, windowMinutes, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockLeaderboardResponse>(e.GetRawText(), PostSerializerOptions);
    }

    /// <summary>Cross-symbol stock-flow outliers (imbalance-ranked). Requires the Alpha plan.</summary>
    public Task<JsonElement> FlowStocksOutliersAsync(int? limit = null, int? minTrades = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (limit.HasValue) p["limit"] = limit.Value.ToString();
        if (minTrades.HasValue) p["minTrades"] = minTrades.Value.ToString();
        if (windowMinutes.HasValue) p["windowMinutes"] = windowMinutes.Value.ToString();
        return GetAsync("/v1/flow/stocks/outliers", p.Count > 0 ? p : null, ct);
    }

    /// <summary>Strongly-typed variant of <see cref="FlowStocksOutliersAsync"/>.</summary>
    public async Task<FlowStockOutliersResponse?> FlowStocksOutliersTypedAsync(int? limit = null, int? minTrades = null, int? windowMinutes = null, CancellationToken ct = default)
    {
        var e = await FlowStocksOutliersAsync(limit, minTrades, windowMinutes, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<FlowStockOutliersResponse>(e.GetRawText(), PostSerializerOptions);
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

    /// <summary>
    /// Strongly-typed variant of <see cref="GreeksAsync(double, double, double, double, string, double?, double?, CancellationToken)"/>.
    /// Returns a <see cref="PricingGreeksResponse"/> POCO with first-, second-, and third-order
    /// greeks plus theoretical price. The original
    /// <see cref="GreeksAsync(double, double, double, double, string, double?, double?, CancellationToken)"/>
    /// remains unchanged.
    /// </summary>
    public async Task<PricingGreeksResponse?> GreeksTypedAsync(
        double spot,
        double strike,
        double dte,
        double sigma,
        string type = "call",
        double r = 0.045,
        double q = 0.013,
        CancellationToken ct = default)
    {
        var element = await GreeksAsync(spot, strike, dte, sigma, type, r, q, ct).ConfigureAwait(false);
        return element.Deserialize<PricingGreeksResponse>(PostSerializerOptions);
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

    /// <summary>
    /// Strongly-typed variant of <see cref="IvAsync(double, double, double, double, string, double?, double?, CancellationToken)"/>.
    /// Returns a <see cref="PricingIvResponse"/> POCO with the inverted IV (decimal +
    /// percent) and an echo of the request inputs. The original
    /// <see cref="IvAsync(double, double, double, double, string, double?, double?, CancellationToken)"/>
    /// remains unchanged.
    /// </summary>
    public async Task<PricingIvResponse?> IvTypedAsync(
        double spot,
        double strike,
        double dte,
        double price,
        string type = "call",
        double? r = null,
        double? q = null,
        CancellationToken ct = default)
    {
        var element = await IvAsync(spot, strike, dte, price, type, r, q, ct).ConfigureAwait(false);
        return element.Deserialize<PricingIvResponse>(PostSerializerOptions);
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

    /// <summary>
    /// Strongly-typed variant of <see cref="KellyAsync(double, double, double, double, double, double, string, double?, double?, CancellationToken)"/>.
    /// Returns a <see cref="PricingKellyResponse"/> POCO with sizing fractions, expected
    /// payoff/probability analytics, and a plain-English recommendation. The original
    /// <see cref="KellyAsync(double, double, double, double, double, double, string, double?, double?, CancellationToken)"/>
    /// remains unchanged.
    /// </summary>
    public async Task<PricingKellyResponse?> KellyTypedAsync(
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
        var element = await KellyAsync(spot, strike, dte, sigma, premium, mu, type, r, q, ct).ConfigureAwait(false);
        return element.Deserialize<PricingKellyResponse>(PostSerializerOptions);
    }

    // ── Volatility Analytics ──────────────────────────────────────────────────

    /// <summary>Comprehensive volatility analysis. Requires Growth+.</summary>
    public Task<JsonElement> VolatilityAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/volatility/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="VolatilityAsync(string, CancellationToken)"/>.
    /// Returns a <see cref="VolatilityResponse"/> POCO with realized-vol ladder, IV-RV spreads,
    /// skew profiles, term structure, GEX/theta-by-DTE, put/call profile, OI concentration,
    /// hedging scenarios, and liquidity. The original
    /// <see cref="VolatilityAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<VolatilityResponse?> VolatilityTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await VolatilityAsync(symbol, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<VolatilityResponse>(element.GetRawText(), PostSerializerOptions);
    }

    /// <summary>
    /// Advanced volatility analytics: SVI parameters, variance surface, arbitrage detection,
    /// greek surfaces, and variance swap pricing. Requires Alpha+.
    /// </summary>
    public Task<JsonElement> AdvVolatilityAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/adv_volatility/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="AdvVolatilityAsync(string, CancellationToken)"/>.
    /// Returns an <see cref="AdvVolatilityResponse"/> POCO covering SVI parameter sets,
    /// forward prices, total variance surface, arbitrage flags, variance swap fair values,
    /// and second-/third-order greek surfaces. The original
    /// <see cref="AdvVolatilityAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<AdvVolatilityResponse?> AdvVolatilityTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await AdvVolatilityAsync(symbol, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AdvVolatilityResponse>(element.GetRawText(), PostSerializerOptions);
    }

    // ── VRP (Variance Risk Premium) ───────────────────────────────────────────

    /// <summary>
    /// Variance risk premium analytics — the implied-vs-realized vol spread,
    /// conditioned on dealer gamma and vanna regime, with strategy scores
    /// for harvesting. Requires Alpha+.
    /// </summary>
    /// <remarks>
    /// The response is a nested object. Key access paths on the returned
    /// <see cref="JsonElement"/>:
    /// <list type="bullet">
    ///   <item><c>result.GetProperty("symbol")</c>, <c>result.GetProperty("underlying_price")</c> — top-level</item>
    ///   <item><c>result.GetProperty("vrp").GetProperty("z_score")</c>, <c>"percentile"</c>,
    ///     <c>"atm_iv"</c>, <c>"rv_20d"</c>, <c>"vrp_20d"</c> — core VRP metrics
    ///     (NOT at top level — common silent-null trap)</item>
    ///   <item><c>result.GetProperty("directional").GetProperty("downside_vrp")</c>,
    ///     <c>"upside_vrp"</c> — directional skew (NOT <c>put_vrp</c>/<c>call_vrp</c>)</item>
    ///   <item><c>result.GetProperty("gex_conditioned").GetProperty("harvest_score")</c>,
    ///     <c>"regime"</c>, <c>"interpretation"</c> — gamma-regime conditioning (nullable)</item>
    ///   <item><c>result.GetProperty("regime").GetProperty("net_gex")</c>,
    ///     <c>"gamma"</c>, <c>"vrp_regime"</c>, <c>"gamma_flip"</c> — regime snapshot
    ///     (net_gex NOT at top level on this endpoint)</item>
    ///   <item><c>result.GetProperty("strategy_scores")</c> — short_put_spread,
    ///     short_strangle, iron_condor, calendar_spread (0–100, nullable)</item>
    ///   <item><c>result.GetProperty("net_harvest_score")</c>,
    ///     <c>result.GetProperty("dealer_flow_risk")</c> — top-level composite scores</item>
    ///   <item><c>result.GetProperty("term_vrp")</c> — array of <c>{dte, iv, rv, vrp}</c></item>
    /// </list>
    /// </remarks>
    public Task<JsonElement> VrpAsync(string symbol, CancellationToken ct = default)
        => GetAsync($"/v1/vrp/{Uri.EscapeDataString(symbol)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="VrpAsync(string, CancellationToken)"/>.
    /// Returns a <see cref="VrpResponse"/> POCO covering core VRP metrics, directional skew,
    /// gamma-regime conditioning, regime snapshot, strategy scores, and term-VRP series.
    /// The original <see cref="VrpAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<VrpResponse?> VrpTypedAsync(string symbol, CancellationToken ct = default)
    {
        var element = await VrpAsync(symbol, ct).ConfigureAwait(false);
        return element.Deserialize<VrpResponse>(PostSerializerOptions);
    }

    // ── Reference Data ────────────────────────────────────────────────────────

    /// <summary>All available stock tickers.</summary>
    public Task<JsonElement> TickersAsync(CancellationToken ct = default)
        => GetAsync("/v1/tickers", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="TickersAsync(CancellationToken)"/>.
    /// Returns a <see cref="TickersResponse"/> POCO. The original
    /// <see cref="TickersAsync(CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<TickersResponse?> TickersTypedAsync(CancellationToken ct = default)
    {
        var element = await TickersAsync(ct).ConfigureAwait(false);
        return element.Deserialize<TickersResponse>(PostSerializerOptions);
    }

    /// <summary>Option chain metadata (expirations and strikes) for a ticker.</summary>
    public Task<JsonElement> OptionsAsync(string ticker, CancellationToken ct = default)
        => GetAsync($"/v1/options/{Uri.EscapeDataString(ticker)}", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="OptionsAsync(string, CancellationToken)"/>.
    /// Returns an <see cref="OptionsMetaResponse"/> POCO with the listed expirations
    /// and per-expiry strike lists. The original
    /// <see cref="OptionsAsync(string, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<OptionsMetaResponse?> OptionsTypedAsync(string ticker, CancellationToken ct = default)
    {
        var element = await OptionsAsync(ticker, ct).ConfigureAwait(false);
        return element.Deserialize<OptionsMetaResponse>(PostSerializerOptions);
    }

    /// <summary>Currently queried symbols with live data.</summary>
    public Task<JsonElement> SymbolsAsync(CancellationToken ct = default)
        => GetAsync("/v1/symbols", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="SymbolsAsync(CancellationToken)"/>.
    /// Returns a <see cref="SymbolsResponse"/> POCO. The original
    /// <see cref="SymbolsAsync(CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<SymbolsResponse?> SymbolsTypedAsync(CancellationToken ct = default)
    {
        var element = await SymbolsAsync(ct).ConfigureAwait(false);
        return element.Deserialize<SymbolsResponse>(PostSerializerOptions);
    }

    // ── Account & System ──────────────────────────────────────────────────────

    // ── Max Pain ──────────────────────────────────────────────────────────────

    /// <summary>Max pain analysis with dealer alignment, pain curve, OI breakdown,
    /// expected move, pin probability, multi-expiry calendar. Requires Growth+.</summary>
    public Task<JsonElement> MaxPainAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var p = new Dictionary<string, string?>();
        if (expiration is not null) p["expiration"] = expiration;
        return GetAsync($"/v1/maxpain/{Uri.EscapeDataString(symbol)}", p.Count > 0 ? p : null, ct);
    }

    /// <summary>
    /// Strongly-typed variant of <see cref="MaxPainAsync(string, string?, CancellationToken)"/>.
    /// Returns a <see cref="MaxPainResponse"/> POCO with the pain curve, dealer alignment,
    /// expected move, pin probability, and multi-expiry calendar.
    /// The original <see cref="MaxPainAsync(string, string?, CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<MaxPainResponse?> MaxPainTypedAsync(string symbol, string? expiration = null, CancellationToken ct = default)
    {
        var element = await MaxPainAsync(symbol, expiration, ct).ConfigureAwait(false);
        return element.Deserialize<MaxPainResponse>(PostSerializerOptions);
    }

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

    /// <summary>
    /// Strongly-typed variant of <see cref="ScreenerAsync(ScreenerRequest, CancellationToken)"/>.
    /// Returns a <see cref="ScreenerResponse"/> POCO with strongly-typed
    /// <see cref="ScreenerMeta"/> and a <see cref="System.Text.Json.JsonElement"/>[]
    /// for <see cref="ScreenerResponse.Data"/> (row schema depends on <c>select</c>).
    /// The original <see cref="ScreenerAsync(ScreenerRequest, CancellationToken)"/>
    /// remains unchanged.
    /// </summary>
    public async Task<ScreenerResponse?> ScreenerTypedAsync(ScreenerRequest request, CancellationToken ct = default)
    {
        var element = await ScreenerAsync(request, ct).ConfigureAwait(false);
        return element.Deserialize<ScreenerResponse>(PostSerializerOptions);
    }

    /// <inheritdoc cref="ScreenerTypedAsync(ScreenerRequest, CancellationToken)"/>
    public async Task<ScreenerResponse?> ScreenerTypedAsync(object body, CancellationToken ct = default)
    {
        var element = await ScreenerAsync(body, ct).ConfigureAwait(false);
        return element.Deserialize<ScreenerResponse>(PostSerializerOptions);
    }

    /// <summary>Account info and quota usage.</summary>
    public Task<JsonElement> AccountAsync(CancellationToken ct = default)
        => GetAsync("/v1/account", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="AccountAsync(CancellationToken)"/>.
    /// Returns an <see cref="AccountResponse"/> POCO. The original
    /// <see cref="AccountAsync(CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<AccountResponse?> AccountTypedAsync(CancellationToken ct = default)
    {
        var element = await AccountAsync(ct).ConfigureAwait(false);
        return element.Deserialize<AccountResponse>(PostSerializerOptions);
    }

    /// <summary>API health check (public, no authentication required).</summary>
    public Task<JsonElement> HealthAsync(CancellationToken ct = default)
        => GetAsync("/health", null, ct);

    /// <summary>
    /// Strongly-typed variant of <see cref="HealthAsync(CancellationToken)"/>.
    /// Returns a <see cref="HealthResponse"/> POCO. The original
    /// <see cref="HealthAsync(CancellationToken)"/> remains unchanged.
    /// </summary>
    public async Task<HealthResponse?> HealthTypedAsync(CancellationToken ct = default)
    {
        var element = await HealthAsync(ct).ConfigureAwait(false);
        return element.Deserialize<HealthResponse>(PostSerializerOptions);
    }

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
