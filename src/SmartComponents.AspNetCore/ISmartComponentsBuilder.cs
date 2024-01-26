using SmartComponents.StaticAssets.Inference;

namespace Microsoft.AspNetCore.Builder;

public interface ISmartComponentsBuilder
{
    public ISmartComponentsBuilder WithInferenceBackend<T>(string? name = null) where T : class, IInferenceBackend;
}
