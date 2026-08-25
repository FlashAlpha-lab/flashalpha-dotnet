using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlashAlpha;
using Xunit;

namespace FlashAlpha.Tests;

/// <summary>
/// The envelope is carried on <see cref="FlashAlphaResponse"/> rather than repeated on
/// each of the 83 response models. That is only sound if the base actually binds through
/// the deserializer, so these tests exercise the binding rather than the declaration -
/// a model that quietly stopped inheriting would still compile.
/// </summary>
public class ResponseEnvelopeTests
{
    private const string Body = """
    {
      "symbol": "SPY",
      "net_gex": 1234.5,
      "endpoint_version": "2026.08.25",
      "data_as_of": {
        "node": "fa2",
        "equity_feed": "2026-08-25T18:48:58.204Z",
        "equity_options_feed": "2026-08-25T18:48:57.900Z",
        "index_feed": null,
        "index_options_feed": null,
        "futures_feed": null,
        "futures_options_feed": null,
        "flow_feed": "2026-08-25T18:48:55.100Z",
        "oi_feed": "2026-08-22T20:00:00.000Z",
        "macro_feed": "2026-08-25T18:45:00.000Z"
      }
    }
    """;

    [Fact]
    public void Envelope_BindsThroughTheBaseClass()
    {
        var gex = JsonSerializer.Deserialize<GexResponse>(Body);

        Assert.NotNull(gex);
        Assert.Equal("SPY", gex!.Symbol);
        Assert.Equal("2026.08.25", gex.EndpointVersion);
        Assert.NotNull(gex.DataAsOf);
        Assert.Equal("fa2", gex.DataAsOf!.Node);
        Assert.Equal("2026-08-25T18:48:58.204Z", gex.DataAsOf.EquityFeed);
        Assert.Equal("2026-08-25T18:48:57.900Z", gex.DataAsOf.EquityOptionsFeed);
        Assert.Equal("2026-08-25T18:48:55.100Z", gex.DataAsOf.FlowFeed);
        Assert.Equal("2026-08-25T18:45:00.000Z", gex.DataAsOf.MacroFeed);
    }

    /// <summary>
    /// A feed a node has never seen is reported as null, which is not the same as the
    /// feed being unhealthy. Distinguishing the two is the point, so null must survive
    /// as null rather than collapsing to empty string.
    /// </summary>
    [Fact]
    public void UnseenFeeds_StayNull()
    {
        var gex = JsonSerializer.Deserialize<GexResponse>(Body);

        Assert.Null(gex!.DataAsOf!.IndexFeed);
        Assert.Null(gex.DataAsOf.FuturesFeed);
        Assert.Null(gex.DataAsOf.FuturesOptionsFeed);
    }

    /// <summary>
    /// Settled open interest is published once per session, so it trails the response by
    /// design. This asserts the value is passed through untouched rather than normalised
    /// toward the response time, since that trailing gap is the signal a caller checks.
    /// </summary>
    [Fact]
    public void SettledOpenInterest_TrailsUnmodified()
    {
        var gex = JsonSerializer.Deserialize<GexResponse>(Body);

        Assert.Equal("2026-08-22T20:00:00.000Z", gex!.DataAsOf!.OiFeed);
    }

    /// <summary>Responses predating the envelope still deserialize; both members are optional.</summary>
    [Fact]
    public void PreEnvelopeResponses_StillDeserialize()
    {
        var gex = JsonSerializer.Deserialize<GexResponse>("""{"symbol":"SPY","net_gex":1.0}""");

        Assert.NotNull(gex);
        Assert.Equal("SPY", gex!.Symbol);
        Assert.Null(gex.EndpointVersion);
        Assert.Null(gex.DataAsOf);
    }

    /// <summary>The envelope survives the full client pipeline, not just a bare deserialize.</summary>
    [Fact]
    public async Task Envelope_SurvivesTheClientPipeline()
    {
        var (client, _) = TestClientFactory.Create(body: Body);

        var gex = await client.GexTypedAsync("SPY");

        Assert.Equal("2026.08.25", gex!.EndpointVersion);
        Assert.Equal("fa2", gex.DataAsOf!.Node);
        Assert.Equal("2026-08-22T20:00:00.000Z", gex.DataAsOf.OiFeed);
    }

    /// <summary>
    /// Every public response model must reach the envelope. Reflection is the guard here
    /// because the alternative - trusting that a regex touched all 83 files - is exactly
    /// the assumption worth testing.
    /// </summary>
    [Fact]
    public void EveryResponseModel_CarriesTheEnvelope()
    {
        var missing = typeof(FlashAlphaClient).Assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Response") && t != typeof(FlashAlphaResponse))
            .Where(t => !typeof(FlashAlphaResponse).IsAssignableFrom(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(missing.Count == 0,
            $"response models not inheriting FlashAlphaResponse: {string.Join(", ", missing)}");
    }
}
