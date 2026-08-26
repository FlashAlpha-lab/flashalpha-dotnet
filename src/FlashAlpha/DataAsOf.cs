using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// When each upstream feed last delivered to the node that served the response.
///
/// <para>Present on every successful response as <c>data_as_of</c>. The shape is fixed:
/// every property exists on every endpoint, and one is <c>null</c> when that node has not
/// received anything on that feed since it started.</para>
///
/// <para>Spot and options are reported separately because they arrive over different pipes
/// and fail independently - an index chain can be current while the index level behind it
/// is not, and one timestamp cannot express that.</para>
///
/// <para>Read each feed against its OWN cadence rather than against <c>as_of</c>.
/// <see cref="OiFeed"/> dated to the previous session's close is correct, because settled
/// open interest is published once per session: on a Monday the newest figure that exists
/// is Friday's. <see cref="EquityOptionsFeed"/> an hour behind during the regular session
/// is not correct.</para>
///
/// <para>A timestamp evidences that the feed delivered recently. It does not assert that
/// every contract in a chain is equally current: an illiquid strike may not have quoted
/// for hours while its feed is healthy.</para>
/// </summary>
public sealed class DataAsOf
{
    /// <summary>Which node answered. Nodes hydrate independently, so their feeds can differ.</summary>
    [JsonPropertyName("node")]
    public string? Node { get; set; }

    /// <summary>Equity and ETF spot quotes. Ticks in seconds during market hours.</summary>
    [JsonPropertyName("equity_feed")]
    public string? EquityFeed { get; set; }

    /// <summary>Equity and ETF option quotes. Ticks in seconds during market hours.</summary>
    [JsonPropertyName("equity_options_feed")]
    public string? EquityOptionsFeed { get; set; }

    /// <summary>Index spot - SPX, RUT, VIX and the other index roots. Ticks in seconds during market hours.</summary>
    [JsonPropertyName("index_feed")]
    public string? IndexFeed { get; set; }

    /// <summary>Index option quotes. Ticks in seconds during market hours.</summary>
    [JsonPropertyName("index_options_feed")]
    public string? IndexOptionsFeed { get; set; }

    /// <summary>Futures prices. Ticks in seconds during the futures session.</summary>
    [JsonPropertyName("futures_feed")]
    public string? FuturesFeed { get; set; }

    /// <summary>Futures option quotes. Ticks in seconds during the futures session.</summary>
    [JsonPropertyName("futures_options_feed")]
    public string? FuturesOptionsFeed { get; set; }

    /// <summary>Classified options and stock trade tape. Ticks in seconds during market hours.</summary>
    [JsonPropertyName("flow_feed")]
    public string? FlowFeed { get; set; }

    /// <summary>
    /// Settled open interest, dated to the prior 16:00 ET close. Published once per
    /// session, so trailing <c>as_of</c> by a day - or by three across a weekend - is
    /// correct rather than stale.
    /// </summary>
    [JsonPropertyName("oi_feed")]
    public string? OiFeed { get; set; }

    /// <summary>
    /// VIX, VVIX, SKEW, MOVE, SPX and Fear &amp; Greed. Unlike the other feeds this reports
    /// its OLDEST component, because these are independent series rather than one pipe.
    /// </summary>
    [JsonPropertyName("macro_feed")]
    public string? MacroFeed { get; set; }
}

/// <summary>
/// Base for every typed response model. Carries the response envelope the API returns
/// on all successful responses.
///
/// <para>Endpoints that return a bare JSON array cannot carry an envelope in the body and
/// send the same information in the <c>X-Data-As-Of</c> and <c>X-Endpoint-Version</c>
/// response headers instead.</para>
/// </summary>
public abstract class FlashAlphaResponse
{
    /// <summary>Identifies the deployment that produced this response.</summary>
    [JsonPropertyName("endpoint_version")]
    public string? EndpointVersion { get; set; }

    /// <summary>Per-feed freshness of the data behind this response. See <see cref="FlashAlpha.DataAsOf"/>.</summary>
    [JsonPropertyName("data_as_of")]
    public DataAsOf? DataAsOf { get; set; }
}
