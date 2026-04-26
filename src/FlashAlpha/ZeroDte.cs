using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/exposure/zero-dte/{symbol}</c>.
///
/// <para>This is a strongly-typed mirror of the JSON response. Use it via
/// <see cref="FlashAlphaClient.ZeroDteTypedAsync(string, double?, System.Threading.CancellationToken)"/>.
/// The original <see cref="FlashAlphaClient.ZeroDteAsync(string, double?, System.Threading.CancellationToken)"/>
/// remains unchanged and continues to return <see cref="System.Text.Json.JsonElement"/>.</para>
///
/// <para>On weekends/holidays or symbols with no 0DTE today, <see cref="NoZeroDte"/> is
/// <c>true</c> and most fields are <c>null</c> — only <see cref="Symbol"/>, <see cref="AsOf"/>,
/// <see cref="Message"/>, and <see cref="NextZeroDteExpiry"/> are populated.</para>
/// </summary>
public sealed class ZeroDteResponse
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("underlying_price")]
    public double? UnderlyingPrice { get; set; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; set; }

    [JsonPropertyName("as_of")]
    public string? AsOf { get; set; }

    [JsonPropertyName("market_open")]
    public bool? MarketOpen { get; set; }

    [JsonPropertyName("time_to_close_hours")]
    public double? TimeToCloseHours { get; set; }

    [JsonPropertyName("time_to_close_pct")]
    public double? TimeToClosePct { get; set; }

    [JsonPropertyName("regime")]
    public ZeroDteRegime? Regime { get; set; }

    [JsonPropertyName("exposures")]
    public ZeroDteExposures? Exposures { get; set; }

    [JsonPropertyName("expected_move")]
    public ZeroDteExpectedMove? ExpectedMove { get; set; }

    [JsonPropertyName("pin_risk")]
    public ZeroDtePinRisk? PinRisk { get; set; }

    [JsonPropertyName("hedging")]
    public ZeroDteHedging? Hedging { get; set; }

    [JsonPropertyName("decay")]
    public ZeroDteDecay? Decay { get; set; }

    [JsonPropertyName("vol_context")]
    public ZeroDteVolContext? VolContext { get; set; }

    [JsonPropertyName("flow")]
    public ZeroDteFlow? Flow { get; set; }

    [JsonPropertyName("levels")]
    public ZeroDteLevels? Levels { get; set; }

    [JsonPropertyName("liquidity")]
    public ZeroDteLiquidity? Liquidity { get; set; }

    [JsonPropertyName("metadata")]
    public ZeroDteMetadata? Metadata { get; set; }

    [JsonPropertyName("strikes")]
    public List<ZeroDteStrike>? Strikes { get; set; }

    /// <summary>Optional — only present near close (&lt;5 min) when greeks may be unstable.</summary>
    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    // ── No-0DTE fallback ───────────────────────────────────────────────────

    [JsonPropertyName("no_zero_dte")]
    public bool? NoZeroDte { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("next_zero_dte_expiry")]
    public string? NextZeroDteExpiry { get; set; }
}

public sealed class ZeroDteRegime
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("gamma_flip")]
    public double? GammaFlip { get; set; }

    [JsonPropertyName("spot_vs_flip")]
    public string? SpotVsFlip { get; set; }

    [JsonPropertyName("spot_to_flip_pct")]
    public double? SpotToFlipPct { get; set; }

    [JsonPropertyName("distance_to_flip_dollars")]
    public double? DistanceToFlipDollars { get; set; }

    [JsonPropertyName("distance_to_flip_sigmas")]
    public double? DistanceToFlipSigmas { get; set; }
}

public sealed class ZeroDteExposures
{
    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    [JsonPropertyName("pct_of_total_gex")]
    public double? PctOfTotalGex { get; set; }

    [JsonPropertyName("total_chain_net_gex")]
    public double? TotalChainNetGex { get; set; }
}

public sealed class ZeroDteExpectedMove
{
    [JsonPropertyName("implied_1sd_dollars")]
    public double? Implied1SdDollars { get; set; }

    [JsonPropertyName("implied_1sd_pct")]
    public double? Implied1SdPct { get; set; }

    [JsonPropertyName("remaining_1sd_dollars")]
    public double? Remaining1SdDollars { get; set; }

    [JsonPropertyName("remaining_1sd_pct")]
    public double? Remaining1SdPct { get; set; }

    [JsonPropertyName("upper_bound")]
    public double? UpperBound { get; set; }

    [JsonPropertyName("lower_bound")]
    public double? LowerBound { get; set; }

    [JsonPropertyName("straddle_price")]
    public double? StraddlePrice { get; set; }

    [JsonPropertyName("atm_iv")]
    public double? AtmIv { get; set; }
}

public sealed class ZeroDtePinComponents
{
    [JsonPropertyName("oi_score")]
    public int? OiScore { get; set; }

    [JsonPropertyName("proximity_score")]
    public int? ProximityScore { get; set; }

    [JsonPropertyName("time_score")]
    public int? TimeScore { get; set; }

    [JsonPropertyName("gamma_score")]
    public int? GammaScore { get; set; }
}

public sealed class ZeroDtePinRisk
{
    [JsonPropertyName("magnet_strike")]
    public double? MagnetStrike { get; set; }

    [JsonPropertyName("magnet_gex")]
    public double? MagnetGex { get; set; }

    [JsonPropertyName("distance_to_magnet_pct")]
    public double? DistanceToMagnetPct { get; set; }

    [JsonPropertyName("pin_score")]
    public int? PinScore { get; set; }

    [JsonPropertyName("components")]
    public ZeroDtePinComponents? Components { get; set; }

    [JsonPropertyName("max_pain")]
    public double? MaxPain { get; set; }

    [JsonPropertyName("oi_concentration_top3_pct")]
    public double? OiConcentrationTop3Pct { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ZeroDteHedgingBucket
{
    [JsonPropertyName("dealer_shares_to_trade")]
    public double? DealerSharesToTrade { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("notional_usd")]
    public double? NotionalUsd { get; set; }
}

public sealed class ZeroDteHedging
{
    [JsonPropertyName("spot_up_10bp")]
    public ZeroDteHedgingBucket? SpotUp10Bp { get; set; }

    [JsonPropertyName("spot_down_10bp")]
    public ZeroDteHedgingBucket? SpotDown10Bp { get; set; }

    [JsonPropertyName("spot_up_25bp")]
    public ZeroDteHedgingBucket? SpotUp25Bp { get; set; }

    [JsonPropertyName("spot_down_25bp")]
    public ZeroDteHedgingBucket? SpotDown25Bp { get; set; }

    [JsonPropertyName("spot_up_half_pct")]
    public ZeroDteHedgingBucket? SpotUpHalfPct { get; set; }

    [JsonPropertyName("spot_down_half_pct")]
    public ZeroDteHedgingBucket? SpotDownHalfPct { get; set; }

    [JsonPropertyName("spot_up_1pct")]
    public ZeroDteHedgingBucket? SpotUp1Pct { get; set; }

    [JsonPropertyName("spot_down_1pct")]
    public ZeroDteHedgingBucket? SpotDown1Pct { get; set; }

    [JsonPropertyName("convexity_at_spot")]
    public double? ConvexityAtSpot { get; set; }
}

public sealed class ZeroDteDecay
{
    [JsonPropertyName("net_theta_dollars")]
    public double? NetThetaDollars { get; set; }

    [JsonPropertyName("theta_per_hour_remaining")]
    public double? ThetaPerHourRemaining { get; set; }

    [JsonPropertyName("charm_regime")]
    public string? CharmRegime { get; set; }

    [JsonPropertyName("charm_description")]
    public string? CharmDescription { get; set; }

    [JsonPropertyName("gamma_acceleration")]
    public double? GammaAcceleration { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ZeroDteVolContext
{
    [JsonPropertyName("zero_dte_atm_iv")]
    public double? ZeroDteAtmIv { get; set; }

    [JsonPropertyName("seven_dte_atm_iv")]
    public double? SevenDteAtmIv { get; set; }

    [JsonPropertyName("iv_ratio_0dte_7dte")]
    public double? IvRatio0Dte7Dte { get; set; }

    [JsonPropertyName("vix")]
    public double? Vix { get; set; }

    [JsonPropertyName("vanna_exposure")]
    public double? VannaExposure { get; set; }

    [JsonPropertyName("vanna_interpretation")]
    public string? VannaInterpretation { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ZeroDteFlow
{
    [JsonPropertyName("total_volume")]
    public long? TotalVolume { get; set; }

    [JsonPropertyName("call_volume")]
    public long? CallVolume { get; set; }

    [JsonPropertyName("put_volume")]
    public long? PutVolume { get; set; }

    [JsonPropertyName("net_call_minus_put_volume")]
    public long? NetCallMinusPutVolume { get; set; }

    [JsonPropertyName("total_oi")]
    public long? TotalOi { get; set; }

    [JsonPropertyName("call_oi")]
    public long? CallOi { get; set; }

    [JsonPropertyName("put_oi")]
    public long? PutOi { get; set; }

    [JsonPropertyName("pc_ratio_volume")]
    public double? PcRatioVolume { get; set; }

    [JsonPropertyName("pc_ratio_oi")]
    public double? PcRatioOi { get; set; }

    [JsonPropertyName("volume_to_oi_ratio")]
    public double? VolumeToOiRatio { get; set; }

    [JsonPropertyName("atm_volume_share_pct")]
    public double? AtmVolumeSharePct { get; set; }

    [JsonPropertyName("top3_strike_volume_pct")]
    public double? Top3StrikeVolumePct { get; set; }
}

public sealed class ZeroDteLevels
{
    [JsonPropertyName("call_wall")]
    public double? CallWall { get; set; }

    [JsonPropertyName("call_wall_gex")]
    public double? CallWallGex { get; set; }

    [JsonPropertyName("call_wall_strength")]
    public double? CallWallStrength { get; set; }

    [JsonPropertyName("distance_to_call_wall_pct")]
    public double? DistanceToCallWallPct { get; set; }

    [JsonPropertyName("put_wall")]
    public double? PutWall { get; set; }

    [JsonPropertyName("put_wall_gex")]
    public double? PutWallGex { get; set; }

    [JsonPropertyName("put_wall_strength")]
    public double? PutWallStrength { get; set; }

    [JsonPropertyName("distance_to_put_wall_pct")]
    public double? DistanceToPutWallPct { get; set; }

    [JsonPropertyName("distance_to_magnet_dollars")]
    public double? DistanceToMagnetDollars { get; set; }

    [JsonPropertyName("highest_oi_strike")]
    public double? HighestOiStrike { get; set; }

    [JsonPropertyName("highest_oi_total")]
    public long? HighestOiTotal { get; set; }

    [JsonPropertyName("max_positive_gamma")]
    public double? MaxPositiveGamma { get; set; }

    [JsonPropertyName("max_negative_gamma")]
    public double? MaxNegativeGamma { get; set; }

    [JsonPropertyName("level_cluster_score")]
    public int? LevelClusterScore { get; set; }
}

public sealed class ZeroDteLiquidity
{
    [JsonPropertyName("atm_spread_pct")]
    public double? AtmSpreadPct { get; set; }

    [JsonPropertyName("weighted_spread_pct")]
    public double? WeightedSpreadPct { get; set; }

    [JsonPropertyName("execution_score")]
    public int? ExecutionScore { get; set; }
}

public sealed class ZeroDteMetadata
{
    [JsonPropertyName("snapshot_age_seconds")]
    public double? SnapshotAgeSeconds { get; set; }

    [JsonPropertyName("chain_contract_count")]
    public int? ChainContractCount { get; set; }

    [JsonPropertyName("data_quality_score")]
    public int? DataQualityScore { get; set; }

    [JsonPropertyName("greek_smoothness_score")]
    public int? GreekSmoothnessScore { get; set; }
}

public sealed class ZeroDteStrike
{
    [JsonPropertyName("strike")]
    public double Strike { get; set; }

    [JsonPropertyName("distance_from_spot_pct")]
    public double? DistanceFromSpotPct { get; set; }

    [JsonPropertyName("call_symbol")]
    public string? CallSymbol { get; set; }

    [JsonPropertyName("put_symbol")]
    public string? PutSymbol { get; set; }

    [JsonPropertyName("call_gex")]
    public double? CallGex { get; set; }

    [JsonPropertyName("put_gex")]
    public double? PutGex { get; set; }

    [JsonPropertyName("net_gex")]
    public double? NetGex { get; set; }

    [JsonPropertyName("call_dex")]
    public double? CallDex { get; set; }

    [JsonPropertyName("put_dex")]
    public double? PutDex { get; set; }

    [JsonPropertyName("net_dex")]
    public double? NetDex { get; set; }

    [JsonPropertyName("net_vex")]
    public double? NetVex { get; set; }

    [JsonPropertyName("net_chex")]
    public double? NetChex { get; set; }

    [JsonPropertyName("call_oi")]
    public long? CallOi { get; set; }

    [JsonPropertyName("put_oi")]
    public long? PutOi { get; set; }

    [JsonPropertyName("call_volume")]
    public long? CallVolume { get; set; }

    [JsonPropertyName("put_volume")]
    public long? PutVolume { get; set; }

    [JsonPropertyName("gex_share_pct")]
    public double? GexSharePct { get; set; }

    [JsonPropertyName("oi_share_pct")]
    public double? OiSharePct { get; set; }

    [JsonPropertyName("volume_share_pct")]
    public double? VolumeSharePct { get; set; }

    [JsonPropertyName("call_iv")]
    public double? CallIv { get; set; }

    [JsonPropertyName("put_iv")]
    public double? PutIv { get; set; }

    [JsonPropertyName("call_delta")]
    public double? CallDelta { get; set; }

    [JsonPropertyName("put_delta")]
    public double? PutDelta { get; set; }

    [JsonPropertyName("call_gamma")]
    public double? CallGamma { get; set; }

    [JsonPropertyName("put_gamma")]
    public double? PutGamma { get; set; }

    [JsonPropertyName("call_theta")]
    public double? CallTheta { get; set; }

    [JsonPropertyName("put_theta")]
    public double? PutTheta { get; set; }

    [JsonPropertyName("call_mid")]
    public double? CallMid { get; set; }

    [JsonPropertyName("put_mid")]
    public double? PutMid { get; set; }

    [JsonPropertyName("call_spread_pct")]
    public double? CallSpreadPct { get; set; }

    [JsonPropertyName("put_spread_pct")]
    public double? PutSpreadPct { get; set; }
}
