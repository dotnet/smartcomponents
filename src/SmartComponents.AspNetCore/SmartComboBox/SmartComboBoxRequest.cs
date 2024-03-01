// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using SmartComponents.Inference;

namespace Microsoft.AspNetCore.Builder;

public readonly struct SmartComboBoxRequest
{
    public SimilarityQuery Query { get; init; }
    public HttpContext HttpContext { get; init; }
}
