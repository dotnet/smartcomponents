using Microsoft.Extensions.DependencyInjection;
using SmartComponents.StaticAssets.Inference;

namespace Microsoft.AspNetCore.Builder;

internal class DefaultSmartComponentsBuilder(IServiceCollection services) : ISmartComponentsBuilder
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
}
