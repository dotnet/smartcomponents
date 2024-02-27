using Microsoft.AspNetCore.Http;

namespace SmartComponents.AspNetCore;

public readonly struct SmartComboBoxSuggestionsRequest
{
    public string InputValue { get; init; }
    public int MaxResults { get; init; }
    public float SimilarityThreshold { get; init; }
    public HttpContext HttpContext { get; init; }
}
