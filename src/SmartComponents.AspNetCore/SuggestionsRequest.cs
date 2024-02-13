using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

public readonly struct SuggestionsRequest
{
    public string InputValue { get; init; }
    public int MaxResults { get; init; }
    public float SimilarityThreshold { get; init; }
    public HttpContext HttpContext { get; init; }
}
