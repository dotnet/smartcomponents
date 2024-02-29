using Microsoft.AspNetCore.Http;
using SmartComponents.Inference;

namespace Microsoft.AspNetCore.Builder;

public readonly struct SmartComboBoxRequest
{
    public SimilarityQuery Query { get; init; }
    public HttpContext HttpContext { get; init; }
}
