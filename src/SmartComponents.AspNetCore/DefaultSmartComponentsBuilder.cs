// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using SmartComponents.StaticAssets.Inference;

namespace Microsoft.AspNetCore.Builder;

internal sealed class DefaultSmartComponentsBuilder(IServiceCollection services) : ISmartComponentsBuilder
{
    public ISmartComponentsBuilder WithInferenceBackend<T>(string? name) where T : class, IInferenceBackend
    {
        if (string.IsNullOrEmpty(name))
        {
            services.AddSingleton<IInferenceBackend, T>();
        }
        else
        {
            services.AddKeyedSingleton<IInferenceBackend, T>(name);
        }

        return this;
    }

    public ISmartComponentsBuilder WithAntiforgeryValidation()
    {
        services.AddSingleton<SmartComponentsAntiforgeryValidation>();
        return this;
    }

    internal static bool HasEnabledAntiForgeryValidation(IServiceProvider services)
    {
        return services.GetService<SmartComponentsAntiforgeryValidation>() is not null;
    }

    internal sealed class SmartComponentsAntiforgeryValidation { }
}
