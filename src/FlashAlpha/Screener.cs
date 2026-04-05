using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlashAlpha;

/// <summary>
/// Request body for the live options screener (<c>POST /v1/screener</c>).
/// All fields are optional — an empty request returns the default universe with
/// default fields for your tier.
/// </summary>
public sealed class ScreenerRequest
{
    /// <summary>Recursive filter tree (leaf or group node).</summary>
    [JsonPropertyName("filters")]
    public object? Filters { get; set; }

    /// <summary>Sort specs applied in order (primary first).</summary>
    [JsonPropertyName("sort")]
    public List<ScreenerSort>? Sort { get; set; }

    /// <summary>Field names to return, or <c>["*"]</c> for the full flat object.</summary>
    [JsonPropertyName("select")]
    public List<string>? Select { get; set; }

    /// <summary>Computed fields (Alpha tier only).</summary>
    [JsonPropertyName("formulas")]
    public List<ScreenerFormula>? Formulas { get; set; }

    /// <summary>Row cap. 1-10 on Growth, 1-50 on Alpha.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    /// <summary>Pagination offset (Alpha only).</summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }
}

/// <summary>Leaf filter condition: <c>{field, operator, value}</c> or <c>{formula, operator, value}</c>.</summary>
public sealed class ScreenerLeaf
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("formula")]
    public string? Formula { get; set; }

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "eq";

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

/// <summary>Filter group node: <c>{op: "and"|"or", conditions: [...]}</c>.</summary>
public sealed class ScreenerGroup
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = "and";

    [JsonPropertyName("conditions")]
    public List<object> Conditions { get; set; } = new();
}

/// <summary>Sort spec: <c>{field|formula, direction: "asc"|"desc"}</c>.</summary>
public sealed class ScreenerSort
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("formula")]
    public string? Formula { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "desc";
}

/// <summary>Computed field definition (Alpha tier only).</summary>
public sealed class ScreenerFormula
{
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = string.Empty;

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;
}
