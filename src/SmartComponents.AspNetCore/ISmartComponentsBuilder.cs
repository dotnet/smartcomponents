// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SmartComponents.StaticAssets.Inference;

namespace Microsoft.AspNetCore.Builder;

public interface ISmartComponentsBuilder
{
    public ISmartComponentsBuilder WithInferenceBackend<T>(string? name = null) where T : class, IInferenceBackend;

    public ISmartComponentsBuilder WithAntiforgeryValidation();
}
