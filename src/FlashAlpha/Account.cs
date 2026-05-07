using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Typed response model for <c>GET /v1/account</c> (Free+).
///
/// <para>Account identity, plan, and daily quota usage for the authenticated key.</para>
///
/// <para><b>String-vs-int caveat:</b> the API returns <see cref="DailyLimit"/> and
/// <see cref="Remaining"/> as JSON strings (not numbers). Unlimited plans report
/// the literal string <c>"unlimited"</c>; numeric plans report a numeric string
/// like <c>"1000"</c> — caller-side parsing required if you need an integer.
/// <see cref="UsageToday"/> is a real integer.</para>
/// </summary>
public sealed class AccountResponse
{
    /// <summary>Account user identifier (GUID string).</summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>Account email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Plan name: <c>free</c>, <c>basic</c>, <c>growth</c>, <c>alpha</c>, <c>enterprise</c>.</summary>
    [JsonPropertyName("plan")]
    public string? Plan { get; set; }

    /// <summary>Daily request cap as a string. Either a numeric string (e.g. <c>"1000"</c>) or the literal <c>"unlimited"</c>.</summary>
    [JsonPropertyName("daily_limit")]
    public string? DailyLimit { get; set; }

    /// <summary>Number of API requests made today. Always <c>0</c> on unlimited plans.</summary>
    [JsonPropertyName("usage_today")]
    public int? UsageToday { get; set; }

    /// <summary>Requests remaining today as a string. Either a numeric string or <c>"unlimited"</c>.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>ISO-8601 timestamp when the daily quota resets (midnight UTC).</summary>
    [JsonPropertyName("resets_at")]
    public string? ResetsAt { get; set; }
}
