using System;
using System.Text.Json;

namespace FlashAlpha;

/// <summary>Base exception for all FlashAlpha API errors.</summary>
public class FlashAlphaException : Exception
{
    /// <summary>HTTP status code returned by the API.</summary>
    public int StatusCode { get; }

    /// <summary>Raw JSON response body, if available.</summary>
    public JsonElement? Response { get; }

    public FlashAlphaException(string message, int statusCode, JsonElement? response = null)
        : base(message)
    {
        StatusCode = statusCode;
        Response = response;
    }
}

/// <summary>Raised when the API key is missing or invalid (HTTP 401).</summary>
public class AuthenticationException : FlashAlphaException
{
    public AuthenticationException(string message, JsonElement? response = null)
        : base(message, 401, response) { }
}

/// <summary>Raised when the endpoint requires a higher subscription tier (HTTP 403).</summary>
public class TierRestrictedException : FlashAlphaException
{
    /// <summary>The caller's current subscription plan.</summary>
    public string? CurrentPlan { get; }

    /// <summary>The minimum plan required to access the endpoint.</summary>
    public string? RequiredPlan { get; }

    public TierRestrictedException(string message, string? currentPlan = null, string? requiredPlan = null, JsonElement? response = null)
        : base(message, 403, response)
    {
        CurrentPlan = currentPlan;
        RequiredPlan = requiredPlan;
    }
}

/// <summary>Raised when the requested resource is not found (HTTP 404).</summary>
public class NotFoundException : FlashAlphaException
{
    public NotFoundException(string message, JsonElement? response = null)
        : base(message, 404, response) { }
}

/// <summary>Raised when the rate limit is exceeded (HTTP 429).</summary>
public class RateLimitException : FlashAlphaException
{
    /// <summary>Number of seconds to wait before retrying, if the server supplied a Retry-After header.</summary>
    public int? RetryAfter { get; }

    public RateLimitException(string message, int? retryAfter = null, JsonElement? response = null)
        : base(message, 429, response)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>Raised when the API returns a 5xx server error.</summary>
public class ServerException : FlashAlphaException
{
    public ServerException(string message, int statusCode, JsonElement? response = null)
        : base(message, statusCode, response) { }
}
